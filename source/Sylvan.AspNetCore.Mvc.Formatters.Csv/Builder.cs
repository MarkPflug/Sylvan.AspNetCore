using Sylvan.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Extension methods for configuring the Sylvan CSV MVC formatters.
/// </summary>
public static class SylvanExtensions
{
	/// <summary>
	/// Registers both the CSV input and output formatters using the default options.
	/// </summary>
	public static MvcOptions AddSylvanCsvFormatters(this MvcOptions opts)
	{
		opts.InputFormatters.Add(new CsvInputFormatter());
		opts.OutputFormatters.Add(new CsvOutputFormatter());

		return opts;
	}
}
