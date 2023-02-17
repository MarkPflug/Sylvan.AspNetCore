using Microsoft.AspNetCore.Mvc.Formatters;
using Sylvan.Data.Csv;
using System.Buffers;
using System.Net;
using System.Text;

namespace Sylvan.AspNetCore.Mvc.Formatters;

/// <summary>
/// Output formatter for converting API results to text/csv HTTP response body.
/// </summary>
public class CsvOutputFormatter : TextOutputFormatter
{
	Action<CsvDataWriterOptions>? options;

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
		SupportedMediaTypes.Add(CsvConstants.CsvContentType);
	}

	/// <inheritdoc/>
	protected override bool CanWriteType(Type? type)
	{
		return type != null && FormatterUtils.CanWriteType(type);
	}

	/// <inheritdoc/>
	public override bool CanWriteResult(OutputFormatterCanWriteContext context)
	{
		return context.ContentType.Value == CsvConstants.CsvContentType;
	}

	/// <inheritdoc/>
	public override Encoding SelectCharacterEncoding(OutputFormatterWriteContext context)
	{
		var enc = base.SelectCharacterEncoding(context);
		return enc;
	}

	/// <inheritdoc/>
	public override IReadOnlyList<string>? GetSupportedContentTypes(string contentType, Type objectType)
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

		context.ContentType = CsvConstants.CsvContentType + "; charset=" + selectedEncoding.WebName;
		if (delimiter != ',')
		{
			context.HttpContext.Response.Headers.Add("Csv-Delimiter", WebUtility.UrlEncode("" + opts.Delimiter));
		}

		var rentedBuffer = ArrayPool<char>.Shared.Rent(opts.BufferSize);
		await using var csv = CsvDataWriter.Create(tw, rentedBuffer, opts);

		var data = context.Object;
		var reader = FormatterUtils.GetReader(data);

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
}
