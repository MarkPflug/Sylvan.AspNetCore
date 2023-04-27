using Microsoft.AspNetCore.Mvc.Formatters;
using Sylvan.Data.Excel;
using Sylvan.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sylvan.AspNetCore.Mvc.Formatters;

/// <summary>
/// Output formatter for converting API results to text/csv HTTP response body.
/// </summary>
public class ExcelOutputFormatter : OutputFormatter
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
		SupportedMediaTypes.Add(ExcelConstants.XlsxContentType);
	}

	/// <inheritdoc/>
	protected override bool CanWriteType(Type? type)
	{
		return type != null && FormatterUtils.CanWriteType(type);
	}

	/// <inheritdoc/>
	public override bool CanWriteResult(OutputFormatterCanWriteContext context)
	{
		return
			context.ContentType.Value == ExcelConstants.XlsxContentType ||
			context.ContentType.Value == ExcelConstants.XlsbContentType;
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
		var opts = new ExcelDataWriterOptions();
		this.options?.Invoke(opts);
		
		var wbType =
			context.ContentType.Value switch
			{
				ExcelConstants.XlsbContentType => ExcelWorkbookType.ExcelBinary,
				ExcelConstants.XlsxContentType => ExcelWorkbookType.ExcelXml,
				_ => ExcelWorkbookType.Unknown,
			};
			
		using var ms = new PooledMemoryStream();
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
