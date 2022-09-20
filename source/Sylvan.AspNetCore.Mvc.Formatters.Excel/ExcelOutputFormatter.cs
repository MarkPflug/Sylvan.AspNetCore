using Microsoft.AspNetCore.Mvc.Formatters;
using Sylvan.Data;
using Sylvan.Data.Excel;
using Sylvan.IO;
using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Sylvan.AspNetCore.Mvc.Formatters;



/// <summary>
/// Output formatter for converting API results to text/csv HTTP response body.
/// </summary>
public class ExcelOutputFormatter : OutputFormatter
{
	Action<ExcelDataWriterOptions>? options;

	ConcurrentDictionary<Type, Func<object, DbDataReader>> objectReaderFactories;

	MethodInfo objectDataReaderCreateMethod;

	/// <summary>
	/// Creates a new CsvOutputFormatter.
	/// </summary>
	public ExcelOutputFormatter() : this(null) { }

	/// <summary>
	/// Creates a new CsvOutputFormatter.
	/// </summary>
	/// <param name="options">Allows customizing the CsvDataWriterOptions.</param>
	public ExcelOutputFormatter(Action<ExcelDataWriterOptions>? options)
	{
		this.options = options;
		SupportedMediaTypes.Add(ExcelConstants.XlsxMimeType);

		this.objectReaderFactories = new ConcurrentDictionary<Type, Func<object, DbDataReader>>();
		this.objectDataReaderCreateMethod = GetObjectDataReaderCreateMethod();
	}

	MethodInfo GetObjectDataReaderCreateMethod()
	{
		return
			typeof(ObjectDataReader)
			.GetMethods()
			.Single(m =>
			{
				if (m.Name != "Create")
					return false;
				var prms = m.GetParameters();
				if (prms.Length != 1)
					return false;
				var pt = prms[0].ParameterType;
				return pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(IEnumerable<>);
			});
	}

	/// <inheritdoc/>
	protected override bool CanWriteType(Type type)
	{
		if (type.IsPrimitive || type == typeof(string))
		{
			return false;
		}
		// can support I/DbDataReader and IEnumerable<T> where T is a complex type
		if (type == typeof(DbDataReader) || type == typeof(IDataReader))
		{
			return true;
		}

		return IsComplexIEnumerableT(type);
	}

	static bool IsComplexIEnumerableT(Type t)
	{
		if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
		{
			var elementArg = t.GetGenericArguments()[0];
			if (Type.GetTypeCode(elementArg) == TypeCode.Object && elementArg != typeof(Guid))
			{
				return true;
			}
		}
		return t.GetInterfaces().Any(IsComplexIEnumerableT);
	}

	/// <inheritdoc/>
	public override bool CanWriteResult(OutputFormatterCanWriteContext context)
	{
		return context.ContentType.Value == ExcelConstants.XlsxMimeType;
	}



	/// <inheritdoc/>
	public override IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
	{
		var items = base.GetSupportedContentTypes(contentType, objectType);
		return items;
	}

	/// <inheritdoc/>
	public async override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
	{
		var opts = new ExcelDataWriterOptions();
		this.options?.Invoke(opts);

		context.ContentType = ExcelConstants.XlsxMimeType;

		using var ms = new PooledMemoryStream();

		using (var edw = ExcelDataWriter.Create(ms, ExcelWorkbookType.ExcelXml, opts))
		{
			var data = context.Object;
			var dr = GetReader(data);

			edw.Write("Sheet1", dr);
			await dr.DisposeAsync();
		}

		var s = context.HttpContext.Response.Body;
		ms.Seek(0, SeekOrigin.Begin);
		await ms.CopyToAsync(s);
		await s.FlushAsync();
	}

	DbDataReader GetReader(object? data)
	{
		if (data is DbDataReader r)
		{
			return r;
		}
		else if (data is IDataReader rr)
		{
			return rr.AsDbDataReader();
		}
		else if (data is IEnumerable e)
		{
			var type = data.GetType();
			var factory = GetObjectReaderFactory(type);
			return factory(data);
		}
		throw new NotSupportedException();
	}

	Func<object, DbDataReader> GetObjectReaderFactory(Type type)
	{
		return this.objectReaderFactories.GetOrAdd(type, BuildObjectReaderFactory);
	}

	Func<object, DbDataReader> BuildObjectReaderFactory(Type type)
	{
		Type? elementType = null;
		foreach (var iface in type.GetInterfaces())
		{
			if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			{
				elementType = iface.GetGenericArguments()[0];
				break;
			}
		}
		if (elementType == null)
		{
			// TODO: might be nice to explain why it isn't supported.
			throw new NotSupportedException();
		}

		var param = Expression.Parameter(typeof(object));
		var createMethod = objectDataReaderCreateMethod.MakeGenericMethod(new Type[] { elementType });
		var lambda = Expression.Lambda<Func<object, DbDataReader>>(
			Expression.Call(
				createMethod,
				Expression.Convert(
					param,
					typeof(IEnumerable<>).MakeGenericType(elementType)
				)
			),
			param
		);

		return lambda.Compile();
	}
}
