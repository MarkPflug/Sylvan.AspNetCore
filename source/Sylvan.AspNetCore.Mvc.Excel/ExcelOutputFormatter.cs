﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Sylvan.Data.Excel;
using Sylvan.IO;

namespace Sylvan.AspNetCore.Mvc.Formatters;

/// <summary>
/// Output formatter for converting API results to text/csv HTTP response body.
/// </summary>
public sealed class ExcelOutputFormatter : OutputFormatter
{
	Action<ExcelDataWriterOptions>? options;

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
		foreach (var type in ExcelFileType.WriterSupported)
		{
			SupportedMediaTypes.Add(type.ContentType);
		}
	}

	/// <inheritdoc/>
	protected override bool CanWriteType(Type? type)
	{
		return type != null && FormatterUtils.CanWriteType(type);
	}

	/// <inheritdoc/>
	public override bool CanWriteResult(OutputFormatterCanWriteContext context)
	{
		if (!base.CanWriteResult(context))
		{
			return false;
		}

		foreach (var type in ExcelFileType.WriterSupported)
		{
			if (StringComparer.OrdinalIgnoreCase.Equals(context.ContentType.Value, type.ContentType))
			{
				return true;
			}
		}
		return false;
	}

	/// <inheritdoc/>
	public override IReadOnlyList<string>? GetSupportedContentTypes(string contentType, Type objectType)
	{
		var items = base.GetSupportedContentTypes(contentType, objectType);
		return items;
	}

	/// <inheritdoc/>
	public async override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
	{
		var cancel = context.HttpContext.RequestAborted;
		var opts = new ExcelDataWriterOptions();
		this.options?.Invoke(opts);

		var wbType =
			ExcelFileType.FindForContentType(context.ContentType.Value)?.WorkbookType
			?? ExcelWorkbookType.Unknown;

		if (wbType == ExcelWorkbookType.Unknown)
		{
			var response = context.HttpContext.Response;
			response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
			await response.CompleteAsync();
			return;
		}

		using var ms = new PooledMemoryStream();
		//using var ms = new MemoryStream();
		using (var edw = ExcelDataWriter.Create(ms, wbType, opts))
		{
			var data = context.Object;
			await using var dr = FormatterUtils.GetReader(data);
			await edw.WriteAsync(dr);
		}

		var s = context.HttpContext.Response.Body;
		ms.Seek(0, SeekOrigin.Begin);
		await ms.CopyToAsync(s);
		await s.FlushAsync();
	}
}
