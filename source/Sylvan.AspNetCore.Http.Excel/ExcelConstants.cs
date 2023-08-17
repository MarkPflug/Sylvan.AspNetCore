namespace Sylvan.Data.Excel;

/// <summary>
/// Defines constants of the Excel file formats.
/// </summary>
public static class ExcelConstants
{
	/// <summary>
	/// The ContentType for .xlsx Excel files.
	/// </summary>
	public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

	/// <summary>
	/// The ContentType for .xlsb Excel files.
	/// </summary>
	public const string XlsbContentType = "application/vnd.ms-excel.sheet.binary.macroEnabled.12";

	/// <summary>
	/// The ContentType for .xls Excel files.
	/// </summary>
	public const string XlsContentType = "application/vnd.ms-excel";

	/// <summary>
	/// Determines if the string defines a known Excel ContentType.
	/// </summary>
	public static bool IsExcelContentType(string contentType)
	{
		return GetWorkbookType(contentType) != ExcelWorkbookType.Unknown;	
	}

	/// <summary>
	/// Gets the ContentType for the ExcelWorkbookType.
	/// </summary>
	public static string GetExcelContentType(ExcelWorkbookType type)
	{
		switch (type)
		{
			case ExcelWorkbookType.Excel: 
				return XlsContentType;
			case ExcelWorkbookType.ExcelXml: 
				return XlsxContentType;
			case ExcelWorkbookType.ExcelBinary:
				return XlsbContentType;
		}
		throw new NotSupportedException();
	}

	/// <summary>
	/// Gets the ExcelWorkbookType for the given ContentType string.
	/// </summary>
	/// <remarks>
	/// This may return ExcelWorkbookType.Unknown if the contentType is an unknown or incorrect ContentType.
	/// </remarks>
	public static ExcelWorkbookType GetWorkbookType(string contentType)
	{
		var compare = StringComparer.OrdinalIgnoreCase;
		if (compare.Equals(contentType, XlsxContentType))
			return ExcelWorkbookType.ExcelXml;
		if (compare.Equals(contentType, XlsbContentType))
			return ExcelWorkbookType.ExcelBinary;
		if (compare.Equals(contentType, XlsContentType))
			return ExcelWorkbookType.Excel;
		return ExcelWorkbookType.Unknown;
	}
}
