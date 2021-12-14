using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sylvan.AspNetCore.Mvc.Formatters;

/// <summary>
/// Output formatter for converting API results to text/csv HTTP response body.
/// </summary>
public class CsvOutputFormatter : TextOutputFormatter
{
	Action<CsvDataWriterOptions>? options;

	ConcurrentDictionary<Type, Func<object, DbDataReader>> objectReaderFactories;
	ConcurrentDictionary<Type, Func<object, DbDataReader>> typeReaderFactories;

	MethodInfo objectDataReaderCreateMethod;

	/// <summary>
	/// Creates a new CsvOutputFormatter.
	/// </summary>
	public CsvOutputFormatter() : this(null) { }

	/// <summary>
	/// Creates a new CsvOutputFormatter.
	/// </summary>
	/// <param name="options">Allows customizing the CsvDataWriterOptions.</param>
	public CsvOutputFormatter(Action<CsvDataWriterOptions>? options)
	{
		this.options = options;
		SupportedEncodings.Add(Encoding.UTF8);
		SupportedEncodings.Add(Encoding.Unicode);
		SupportedMediaTypes.Add("text/csv");

		this.objectReaderFactories = new ConcurrentDictionary<Type, Func<object, DbDataReader>>();
		this.typeReaderFactories = new ConcurrentDictionary<Type, Func<object, DbDataReader>>();
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
		return context.ContentType.Value == "text/csv";
	}

	/// <inheritdoc/>
	public override Encoding SelectCharacterEncoding(OutputFormatterWriteContext context)
	{
		var enc = base.SelectCharacterEncoding(context);
		return enc;
	}

	/// <inheritdoc/>
	public override IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
	{
		var items = base.GetSupportedContentTypes(contentType, objectType);
		return items;
	}

	/// <inheritdoc/>
	public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
	{
		await using var tw = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding);

		var opts = new CsvDataWriterOptions();
		// prefer the shorter line ending, can be overrridden by user.
		opts.NewLine = "\n";
		this.options?.Invoke(opts);

		var delimiter = opts.Delimiter;

		context.ContentType = "text/csv; charset=" + selectedEncoding.WebName;
		if (delimiter != ',')
		{
			context.HttpContext.Response.Headers.Add("Csv-Delimiter", WebUtility.UrlEncode("" + opts.Delimiter));
		}

		var rentedBuffer = ArrayPool<char>.Shared.Rent(opts.BufferSize);
		await using var csv = CsvDataWriter.Create(tw, rentedBuffer, opts);

		var data = context.Object;
		var reader = GetReader(data);

		// TODO: consider adding the ability to include schema info
		// by applying an attribute to the API method.
		// It isn't obvious to me how to access controller/action info from here, however.
		//var schema = reader.GetColumnSchema();
		//var spec = new Schema.Builder(schema).Build().ToString();
		//context.HttpContext.Response.Headers.Add("Csv-Schema", WebUtility.UrlEncode(spec));

		await csv.WriteAsync(reader);

		await reader.DisposeAsync();
		if (rentedBuffer != null)
		{
			ArrayPool<char>.Shared.Return(rentedBuffer);
		}
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
