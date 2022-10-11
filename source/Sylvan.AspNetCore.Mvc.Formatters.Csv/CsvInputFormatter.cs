﻿using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
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
		context.HttpContext.Response.RegisterForDispose(new GenericDisposable(() => ArrayPool<char>.Shared.Return(rentedBuffer)));
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
				var binder = FormatterUtils.GetObjectBinder(targetType, schema, new DataBinderOptions { BindingMode = DataBindingMode.Any, InferColumnTypeFromMember = true });

				var fac = FormatterUtils.GetReaderFactory(modelType);
				var seqReader = fac(binder, csv);
				return InputFormatterResult.Success(seqReader);
			}
		}
		return InputFormatterResult.Failure();
	}

	interface IDataAccessor
	{
		Task<object> GetBufferedData();
	}
}
