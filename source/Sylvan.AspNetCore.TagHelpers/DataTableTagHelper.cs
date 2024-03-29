﻿using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Data.Common;

namespace Sylvan.AspNetCore.TagHelpers;

[HtmlTargetElement("datatable")]
public class DataTableTagHelper : TagHelper
{
	// TODO: make this object and allow DbDataReader, or DataTable, or IEnumerable<object>?
	public DbDataReader Data { get; set; }

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		output.TagMode = TagMode.StartTagAndEndTag;
		output.TagName = "table";
		var reader = Data;

		output.Content.AppendHtml("<thead>");
		output.Content.AppendHtml("<tr>");
		for (int i = 0; i < reader.FieldCount; i++)
		{
			output.Content.AppendHtml("<td>");
			output.Content.Append(reader.GetName(i));
			output.Content.AppendHtml("</td>");
		}

		output.Content.AppendHtml("</tr>");
		output.Content.AppendHtml("</thead>");
		output.Content.AppendHtml("<tbody>");
		while (reader.Read())
		{
			output.Content.AppendHtml("<tr>");

			for (int i = 0; i < reader.FieldCount; i++)
			{
				output.Content.AppendHtml("<td>");
				output.Content.Append(reader.GetValue(i)?.ToString());
				output.Content.AppendHtml("</td>");
			}

			output.Content.AppendHtml("</tr>");
		}
		output.Content.AppendHtml("</tbody>");

		output.Content.AppendHtml("<tfoot>");
		output.Content.AppendHtml("<tr>");
		for (int i = 0; i < reader.FieldCount; i++)
		{
			output.Content.AppendHtml("<td>");
			output.Content.Append(reader.GetName(i));
			output.Content.AppendHtml("</td>");
		}

		output.Content.AppendHtml("</tr>");
		output.Content.AppendHtml("</tfoot>");
	}
}
