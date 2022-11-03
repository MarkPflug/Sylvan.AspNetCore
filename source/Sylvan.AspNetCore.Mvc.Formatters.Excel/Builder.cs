﻿using Sylvan.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Extension methods for configuring the Sylvan Excel MVC formatters.
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

	///// <summary>
	///// Registers the Sylvan Excel model binder.
	///// </summary>
	//public static MvcOptions AddSylvanExcelModelBinder(this MvcOptions opts)
	//{
	//	opts.ModelBinderProviders.Insert(0, new ExcelModelBinderProvider());
	//	return opts;
	//}
}
