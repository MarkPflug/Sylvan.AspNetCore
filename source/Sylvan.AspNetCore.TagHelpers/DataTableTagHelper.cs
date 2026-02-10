using Microsoft.AspNetCore.Razor.TagHelpers;
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

		var c = output.Content;

		c.AppendHtml("<thead>");
		c.AppendHtml("<tr>");
		for (int i = 0; i < reader.FieldCount; i++)
		{
			c.AppendHtml("<td>");
			c.Append(reader.GetName(i));
			c.AppendHtml("</td>");
		}

		c.AppendHtml("</tr>");
		c.AppendHtml("</thead>");
		c.AppendHtml("<tbody>");
		while (reader.Read())
		{
			c.AppendHtml("<tr>");

			for (int i = 0; i < reader.FieldCount; i++)
			{
				c.AppendHtml("<td>");
				c.Append(reader.GetValue(i)?.ToString());
				c.AppendHtml("</td>");
			}

			c.AppendHtml("</tr>");
		}
		c.AppendHtml("</tbody>");

		c.AppendHtml("<tfoot>");
		c.AppendHtml("<tr>");
		for (int i = 0; i < reader.FieldCount; i++)
		{
			c.AppendHtml("<td>");
			c.Append(reader.GetName(i));
			c.AppendHtml("</td>");
		}

		c.AppendHtml("</tr>");
		c.AppendHtml("</tfoot>");
	}
}
