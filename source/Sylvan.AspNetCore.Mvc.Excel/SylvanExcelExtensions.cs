using Sylvan.AspNetCore.Mvc;
using Sylvan.AspNetCore.Mvc.Formatters;
using Sylvan.Data.Excel;
using System.Data.Common;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Provides convenience extension methods for returning Excel responses
/// from controller actions.
/// </summary>
public static class SylvanExcelExtensions
{
	/// <summary>
	/// Registers both the Excel input and output formatters using the default options.
	/// </summary>
	public static MvcOptions AddSylvanExcelFormatters(this MvcOptions opts)
	{
		opts.InputFormatters.Add(new ExcelInputFormatter());
		opts.OutputFormatters.Add(new ExcelOutputFormatter());

		return opts;
	}

	/// <summary>
	/// Returns an Excel result.
	/// </summary>
	public static IActionResult Excel<T>(this ControllerBase context, IEnumerable<T> seq, ExcelWorkbookType type = ExcelWorkbookType.ExcelXml)
		where T : class
	{
		return new ExcelResult<T>(seq, type);
	}

	/// <summary>
	/// Returns an Excel result.
	/// </summary>
	public static IActionResult Excel<T>(this ControllerBase context, IAsyncEnumerable<T> seq, ExcelWorkbookType type = ExcelWorkbookType.ExcelXml)
		where T : class
	{
		return new ExcelResult<T>(seq, type);
	}

	/// <summary>
	/// Returns an Excel result.
	/// </summary>
	public static IActionResult Excel(this ControllerBase context, DbDataReader data, ExcelWorkbookType type = ExcelWorkbookType.ExcelXml, string? filename = null)
	{
		return new ExcelResult(data, type, filename);
	}
}
