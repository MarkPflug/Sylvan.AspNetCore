using Sylvan.AspNetCore.Mvc;
using Sylvan.AspNetCore.Mvc.Formatters;
using System.Data.Common;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Provides convenience extension methods for returning CSV responses
/// from controller actions.
/// </summary>
public static class SylvanCsvExtensions
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

	/// <summary>
	/// Returns a CSV result.
	/// </summary>
	public static IActionResult Csv<T>(this ControllerBase context, IEnumerable<T> seq)
		where T : class
	{
		return new CsvResult<T>(seq);
	}

	/// <summary>
	/// Returns a CSV result.
	/// </summary>
	public static IActionResult Csv<T>(this ControllerBase context, IAsyncEnumerable<T> seq)
		where T : class
	{
		return new CsvResult<T>(seq);
	}

	/// <summary>
	/// Returns a CSV result.
	/// </summary>
	public static IActionResult Csv(this ControllerBase context, DbDataReader data)
	{
		return new CsvResult(data);
	}
}
