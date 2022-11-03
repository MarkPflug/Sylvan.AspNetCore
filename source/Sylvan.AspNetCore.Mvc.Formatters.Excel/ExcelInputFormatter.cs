using Microsoft.AspNetCore.Mvc.Formatters;
using Sylvan.Data;
using Sylvan.Data.Excel;
using Sylvan.IO;
using System.Buffers;
using System.Data;
using System.Data.Common;

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

		context.HttpContext.Response.RegisterForDispose(ms);
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
				var binder = FormatterUtils.GetObjectBinder(targetType, schema, new DataBinderOptions { BindingMode = DataBindingMode.Any, InferColumnTypeFromMember = true });

				var fac = FormatterUtils.GetReaderFactory(modelType);
				var seqReader = fac(binder, edr);
				return InputFormatterResult.Success(seqReader);
			}
		}
		return InputFormatterResult.Failure();
	}
}
