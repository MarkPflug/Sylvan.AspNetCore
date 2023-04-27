using Sylvan.AspNetCore.Mvc;
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
