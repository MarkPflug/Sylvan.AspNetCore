using Sylvan.AspNetCore.Http;
using Sylvan.Data.Excel;
using System.Data.Common;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides convenience extension methods for returning Excel responses
/// from minimal APIs.
/// </summary>
public static class SylvanExcelExtensions
{
	/// <summary>
	/// Returns an Excel result.
	/// </summary>
	public static IResult Excel<T>(
		this IResultExtensions context, 
		IEnumerable<T> seq, 
		ExcelWorkbookType type = ExcelWorkbookType.ExcelXml, 
		string? filename = null
	)
		where T : class
	{
		return new ExcelResult<T>(seq, type,filename);
	}

	/// <summary>
	/// Returns an Excel result.
	/// </summary>
	public static IResult Excel<T>(
		this IResultExtensions context, 
		IAsyncEnumerable<T> seq, 
		ExcelWorkbookType type = ExcelWorkbookType.ExcelXml, 
		string? filename = null
	)
		where T : class
	{
		return new ExcelResult<T>(seq, type, filename);
	}

	/// <summary>
	/// Returns an Excel result.
	/// </summary>
	public static IResult Excel(
		this IResultExtensions context, 
		DbDataReader data, 
		ExcelWorkbookType type = ExcelWorkbookType.ExcelXml, 
		string? filename = null
	)
	{
		return new ExcelResult(data, type, filename);
	}
}