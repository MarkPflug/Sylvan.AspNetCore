using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace Sylvan.AspNetCore.TagHelpers;

[HtmlTargetElement("datetime")]
public class DateTimeTagHelper : TagHelper
{
	const string DisplayFormat = "yyyy-MM-ddTHH:mm:ss";
	const string Format = "yyyy-MM-ddTHH:mm:ssZ";

	public DateTime DateTime { get; set; }

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		var dateStr = DateTime.ToString(Format);
		output.TagMode = TagMode.StartTagAndEndTag;
		output.TagName = "time";
		output.Attributes.SetAttribute("datetime", dateStr);
		output.Content.SetContent(DateTime.ToString(DisplayFormat));
	}
}
