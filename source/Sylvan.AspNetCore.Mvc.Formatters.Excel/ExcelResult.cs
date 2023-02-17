using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Sylvan.AspNetCore.Mvc.Formatters;
using Sylvan.Data;
using Sylvan.Data.Excel;
using Sylvan.IO;
using System.Data.Common;

namespace Sylvan.AspNetCore.Mvc;

class ExcelResult : IResult, IActionResult, IStatusCodeActionResult
{
	readonly DbDataReader data;

	public ExcelResult(DbDataReader data)
	{
		this.data = data;
	}

	public int? StatusCode => StatusCodes.Status200OK;

	public async Task ExecuteAsync(HttpContext httpContext)
	{
		var response = httpContext.Response;
		response.ContentType = ExcelConstants.XlsxContentType;
		var stream = new PooledMemoryStream();
		{
			using var csv = ExcelDataWriter.Create(stream, ExcelWorkbookType.ExcelXml);
			await csv.WriteAsync(data);
		}
		stream.Seek(0, SeekOrigin.Begin);
		await stream.CopyToAsync(response.Body);
	}

	public Task ExecuteResultAsync(ActionContext context)
	{
		return ExecuteAsync(context.HttpContext);
	}
}

class ExcelResult<T> : ExcelResult
	where T : class
{
	public ExcelResult(IEnumerable<T> data) : base(data.AsDataReader()) { }
	public ExcelResult(IAsyncEnumerable<T> data) : base(data.AsDataReader()) { }
}
