using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sylvan.AspNetCore.Mvc.Formatters;

/// <summary>
/// Input formatter for converting text/csv HTTP request body.
/// </summary>
public class CsvInputFormatter : TextInputFormatter
{
	readonly Action<CsvDataReaderOptions> options;

	/// <summary>
	/// Creates a new CsvInputFormatter.
	/// </summary>
	public CsvInputFormatter() : this(o => { })
	{
	}

	/// <summary>
	/// Creates a new CsvInputFormatter.
	/// </summary>
	/// <param name="options">Allows customizing the CsvDataReaderOptions.</param>
	public CsvInputFormatter(Action<CsvDataReaderOptions> options)
	{
		this.options = options;
		SupportedEncodings.Add(Encoding.UTF8);
		SupportedEncodings.Add(Encoding.Unicode);
		SupportedMediaTypes.Add("text/csv");
	}

	/// <inheritdoc/>
	public override async Task<InputFormatterResult> ReadRequestBodyAsync(
		InputFormatterContext context,
		Encoding encoding
	)
	{
		var reader = context.ReaderFactory(context.HttpContext.Request.Body, encoding);

		var opts = new CsvDataReaderOptions();

		this.options?.Invoke(opts);

		var delim = context.HttpContext.Request.Headers["Csv-Delimiter"];

		if (delim.Count == 1)
		{
			var delimStr = delim[0];
			if (delimStr.Length != 1 || delimStr[0] > 127)
			{
				return InputFormatterResult.Failure();
			}
			var delimChar = delimStr[0];
			opts.Delimiter = delimChar;
		}

		var hasHeader = context.HttpContext.Request.Headers["Csv-HasHeaders"];

		if (hasHeader.Count == 1)
		{
			var hasHeaderStr = hasHeader[0];
			bool hasHeaders;
			if (!bool.TryParse(hasHeaderStr, out hasHeaders))
			{
				if (hasHeaderStr == "1")
				{
					hasHeaders = true;
				}
				else if (hasHeaderStr == "0")
				{
					hasHeaders = false;
				}
				else
				{
					// TODO: bad request?
					// Or, allow skipping rows in the input data perhaps?
					hasHeaders = true;
				}
			}
			opts.HasHeaders = hasHeaders;
		}

		var schemaSpecHeader = context.HttpContext.Request.Headers["Csv-Schema"];
		if (schemaSpecHeader.Count == 1)
		{
			var schemaSpec = schemaSpecHeader[0];

			// the schema object is immutable, and thus can be cached/shared between requests
			var mc = context.HttpContext.RequestServices.GetService<IMemoryCache>();
			CsvSchema schema;

			if (mc != null && mc.TryGetValue(schemaSpec, out object val))
			{
				schema = (CsvSchema)val;
			}
			else
			{
				schema = new CsvSchema(Schema.Parse(schemaSpec));
				mc.Set(schemaSpec, schema);
			}
			opts.Schema = schema;
		}

		var rentedBuffer = ArrayPool<char>.Shared.Rent(opts.BufferSize);
		// HACK: create a TextReader that will return our rented buffer when disposed.
		reader = new HackReader(reader, () => ArrayPool<char>.Shared.Return(rentedBuffer));

		var csv = await CsvDataReader.CreateAsync(reader, opts);

		var modelType = context.ModelType;

		if (modelType == typeof(DbDataReader) || modelType == typeof(IDataReader) || modelType == typeof(CsvDataReader))
		{
			return InputFormatterResult.Success(csv);
		}

		if (modelType.IsGenericType)
		{
			var genType = modelType.GetGenericTypeDefinition();

			var schema = csv.GetColumnSchema();

			if (genType == typeof(IAsyncEnumerable<>) || genType == typeof(IEnumerable<>))
			{
				var targetType = modelType.GetGenericArguments()[0];
				var binder = GetObjectBinder(targetType, schema, new DataBinderOptions { BindingMode = DataBindingMode.Any, InferColumnTypeFromMember = true });

				var fac = GetReaderFactory(modelType);
				var seqReader = fac(binder, csv);
				return InputFormatterResult.Success(seqReader);
			}
		}
		return InputFormatterResult.Failure();
	}

	static ConcurrentDictionary<Type, IDataBinder> binders =
		new ConcurrentDictionary<Type, IDataBinder>();

	static ConcurrentDictionary<Type, Func<IDataBinder, DbDataReader, object>> readerFactories =
		new ConcurrentDictionary<Type, Func<IDataBinder, DbDataReader, object>>();

	static MethodInfo BinderCreateMethod =
				typeof(DataBinder)
				.GetMethod("Create", 1, BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(ReadOnlyCollection<DbColumn>), typeof(DataBinderOptions) }, null)!;

	static IDataBinder GetObjectBinder(Type modelType, ReadOnlyCollection<DbColumn> schema, DataBinderOptions opts)
	{
		IDataBinder? binder;
		if (!binders.TryGetValue(modelType, out binder))
		{
			var method = BinderCreateMethod.MakeGenericMethod(modelType);
			binder = (IDataBinder)method.Invoke(null, new object[] { schema, opts })!;
			binders.TryAdd(modelType, binder);
		}
		return binder;
	}

	static Func<IDataBinder, DbDataReader, object> GetReaderFactory(Type modelType)
	{
		Func<IDataBinder, DbDataReader, object>? readerFactory;
		if (!readerFactories.TryGetValue(modelType, out readerFactory))
		{
			var targetType = modelType.GetGenericArguments()[0];

			var ctor = typeof(DataReader<>).MakeGenericType(targetType).GetConstructors()[0];

			var a = Expression.Parameter(typeof(DbDataReader));
			var b = Expression.Parameter(typeof(IDataBinder));

			var lambda =
				Expression.Lambda<Func<IDataBinder, DbDataReader, object>>(
					Expression.New(ctor, a, b),
					b,
					a
				);
			readerFactory = lambda.Compile();
			readerFactories.TryAdd(modelType, readerFactory);

		}
		return readerFactory;
	}

	// allows attaching logic to disposal of a textreader.
	// this is to allow returning a rented buffer when a CsvDataReader
	// holding onto the TextReader is disposed.
	// CsvDataReader only calls Read(char[], int,int) or ReadAsync(char[], int, int)
	// so only those methods need be overridden.
	sealed class HackReader : TextReader
	{
		readonly TextReader inner;
		readonly Action disposeAction;

		public HackReader(TextReader inner, Action disposeAction)
		{
			this.inner = inner;
			this.disposeAction = disposeAction;
		}

		public override Task<int> ReadAsync(char[] buffer, int index, int count)
		{
			return inner.ReadAsync(buffer, index, count);
		}

		// this shouldn't be needed, since everything should be async.
		public override int Read(char[] buffer, int index, int count)
		{
			return inner.Read(buffer, index, count);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				inner.Dispose();
			}
			disposeAction();
		}
	}

	interface IDataAccessor
	{
		Task<object> GetBufferedData();
	}

	class DataReader<T> :
		IAsyncEnumerable<T>,
		IAsyncDisposable,
		IEnumerable<T>,
		IDataAccessor
		where T : new()
	{

		DbDataReader data;
		IDataBinder<T> binder;

		public DataReader(DbDataReader data, object binder)
		{
			this.data = data;
			this.binder = (IDataBinder<T>)binder;
		}

		public ValueTask DisposeAsync()
		{
			return data.DisposeAsync();
		}

		async Task<object> IDataAccessor.GetBufferedData()
		{
			var buffer = new List<T>();
			while (await data.ReadAsync())
			{
				var record = new T();
				binder.Bind(data, record);
				buffer.Add(record);
			}
			return buffer;
		}

		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			while (await data.ReadAsync())
			{
				var record = new T();
				binder.Bind(data, record);
				yield return record;
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			while (data.Read())
			{
				var record = new T();
				binder.Bind(data, record);
				yield return record;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}
