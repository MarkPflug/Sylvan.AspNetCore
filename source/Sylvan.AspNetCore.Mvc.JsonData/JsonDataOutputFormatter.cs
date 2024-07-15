using Microsoft.AspNetCore.Mvc.Formatters;
using Sylvan.Data;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Sylvan.AspNetCore.Mvc.JsonData;

/// <summary>
/// Output formatter for converting DbDataReader API results to application/json HTTP response body.
/// </summary>
public class JsonDataOutputFormatter : TextOutputFormatter
{
	const string JsonContentType = "application/json";

	/// <summary>
	/// Creates a new JsonDataOutputFormatter.
	/// </summary>
	/// <param name="options">Allows customizing the CsvDataWriterOptions.</param>
	public JsonDataOutputFormatter()
	{
		SupportedEncodings.Add(Encoding.UTF8);
		SupportedMediaTypes.Add(JsonContentType);
	}

	/// <inheritdoc/>
	protected override bool CanWriteType(Type? type)
	{
		return type != null &&
			(type.IsAssignableTo(typeof(DbDataReader)) || type.IsAssignableTo(typeof(IDataReader)));
	}

	/// <inheritdoc/>
	public override bool CanWriteResult(OutputFormatterCanWriteContext context)
	{
		if (!base.CanWriteResult(context))
		{
			return false;
		}
		var acceptsJson = context.HttpContext.Request.Headers.Accept.Any(c => StringComparer.OrdinalIgnoreCase.Equals(c, JsonContentType) || c.Contains("*/*"));
		if (acceptsJson)
		{
			context.ContentTypeIsServerDefined = true;
			context.ContentType = JsonContentType;
			return true;
		}
		return false;
	}

	/// <inheritdoc/>
	public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
	{
		context.ContentType = JsonContentType + "; charset=" + selectedEncoding.WebName;
		var data = context.Object;
		await using var reader = FormatterUtils.GetReader(data);
		await reader.WriteJsonAsync(context.HttpContext.Response.Body);
	}
}
