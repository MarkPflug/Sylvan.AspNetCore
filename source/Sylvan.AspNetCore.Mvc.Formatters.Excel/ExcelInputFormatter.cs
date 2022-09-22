using Microsoft.AspNetCore.Mvc.Formatters;
using Sylvan.Data;
using Sylvan.Data.Excel;
using Sylvan.IO;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Sylvan.AspNetCore.Mvc.Formatters;

/// <summary>
/// Input formatter for converting text/csv HTTP request body.
/// </summary>
public class ExcelInputFormatter : InputFormatter
{
	static Dictionary<string, ExcelWorkbookType> MimeMap;

	static ExcelInputFormatter()
	{
		MimeMap = new Dictionary<string, ExcelWorkbookType>(StringComparer.OrdinalIgnoreCase);
		MimeMap.Add(ExcelConstants.XlsxContentType, ExcelWorkbookType.ExcelXml);
		MimeMap.Add(ExcelConstants.XlsbContentType, ExcelWorkbookType.ExcelBinary);
		MimeMap.Add(ExcelConstants.XlsContentType, ExcelWorkbookType.Excel);
	}

	readonly Action<ExcelDataReaderOptions> options;

	/// <summary>
	/// Creates a new CsvInputFormatter.
	/// </summary>
	public ExcelInputFormatter() : this(o => { })
	{
	}

	/// <summary>
	/// Creates a new CsvInputFormatter.
	/// </summary>
	/// <param name="options">Allows customizing the CsvDataReaderOptions.</param>
	public ExcelInputFormatter(Action<ExcelDataReaderOptions> options)
	{
		this.options = options;
		SupportedMediaTypes.Add(ExcelConstants.XlsxContentType);
		SupportedMediaTypes.Add(ExcelConstants.XlsbContentType);
		SupportedMediaTypes.Add(ExcelConstants.XlsContentType);
		SupportedMediaTypes.Add("multipart/form-data");
	}

	static ExcelWorkbookType GetWorkbookType(string contentType)
	{
		return
			MimeMap.TryGetValue(contentType, out var value)
			? value
			: ExcelWorkbookType.Unknown;
	}

	/// <inheritdoc/>
	public async override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
	{
		var opts = new ExcelDataReaderOptions();
		this.options?.Invoke(opts);
		opts.OwnsStream = true;

		var ms = new PooledMemoryStream(ArrayPool<byte>.Shared, 12);

		await context.HttpContext.Request.Body.CopyToAsync(ms);
		ms.Seek(0, SeekOrigin.Begin);

		var workbookType = GetWorkbookType(context.HttpContext.Request.ContentType);

		var edr = ExcelDataReader.Create(ms, workbookType, opts);

		context.HttpContext.Response.RegisterForDispose(edr);

		var modelType = context.ModelType;

		if (modelType == typeof(DbDataReader) || modelType == typeof(IDataReader) || modelType == typeof(ExcelDataReader))
		{
			return InputFormatterResult.Success(edr);
		}

		if (modelType.IsGenericType)
		{
			var genType = modelType.GetGenericTypeDefinition();

			var schema = edr.GetColumnSchema();

			if (genType == typeof(IAsyncEnumerable<>) || genType == typeof(IEnumerable<>))
			{
				var targetType = modelType.GetGenericArguments()[0];
				var binder = GetObjectBinder(targetType, schema, new DataBinderOptions { BindingMode = DataBindingMode.Any, InferColumnTypeFromMember = true });

				var fac = GetReaderFactory(modelType);
				var seqReader = fac(binder, edr);
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

	class DataReader<T> :
		IEnumerable<T>,
		IAsyncEnumerable<T>,
		IAsyncDisposable
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
