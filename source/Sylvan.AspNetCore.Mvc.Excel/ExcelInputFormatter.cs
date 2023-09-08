using Microsoft.AspNetCore.Mvc.Formatters;
using Sylvan.Data;
using Sylvan.Data.Excel;
using Sylvan.IO;

namespace Sylvan.AspNetCore.Mvc.Formatters;

/// <summary>
/// Input formatter for converting text/csv HTTP request body.
/// </summary>
public sealed class ExcelInputFormatter : InputFormatter
{
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
		foreach(var type in ExcelFileType.ReaderSupported)
		{
			SupportedMediaTypes.Add(type.ContentType);

		}
	}

	static ExcelWorkbookType GetWorkbookType(string contentType)
	{
		return
			ExcelFileType.FindForContentType(contentType)?.WorkbookType ?? ExcelWorkbookType.Unknown;
	}

	/// <inheritdoc/>
	public async override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
	{
		var opts = new ExcelDataReaderOptions();
		this.options?.Invoke(opts);
		opts.OwnsStream = true;

		var ms = new PooledMemoryStream();

		await context.HttpContext.Request.Body.CopyToAsync(ms);
		ms.Seek(0, SeekOrigin.Begin);

		var contentType = context.HttpContext.Request.ContentType;
		if (contentType == null)
		{
			return InputFormatterResult.Failure();
		}
		var workbookType = GetWorkbookType(contentType);

		var edr = ExcelDataReader.Create(ms, workbookType, opts);

		context.HttpContext.Response.RegisterForDispose(ms);
		context.HttpContext.Response.RegisterForDispose(edr);

		var modelType = context.ModelType;

		if (modelType.IsAssignableFrom(typeof(ExcelDataReader)))
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
