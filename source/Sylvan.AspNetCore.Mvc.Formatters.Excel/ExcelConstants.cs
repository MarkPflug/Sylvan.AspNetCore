using System;

namespace Sylvan.Data.Excel;

static class ExcelConstants
{
	internal const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
	internal const string XlsbContentType = "application/vnd.ms-excel.sheet.binary.macroEnabled.12";
	internal const string XlsContentType = "application/vnd.ms-excel";

	public static bool IsExcelContentType(string contentType)
	{
		var compare = StringComparer.OrdinalIgnoreCase;
		return
			compare.Equals(contentType, XlsxContentType) |
			compare.Equals(contentType, XlsbContentType) |
			compare.Equals(contentType, XlsContentType);
	}

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
