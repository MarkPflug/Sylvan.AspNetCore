using Microsoft.AspNetCore.Http;
using Sylvan.Data;
using Sylvan.Data.Excel;
using System.Data.Common;

#if MVC
namespace Sylvan.AspNetCore.Mvc;
#else
namespace Sylvan.AspNetCore.Http;
#endif

public class ExcelResult :
#if MVC
    Microsoft.AspNetCore.Mvc.IActionResult
#else
	IResult
#endif
{
	readonly DbDataReader data;
	readonly string? filename;
	readonly ExcelWorkbookType type;

	public ExcelResult(DbDataReader data, ExcelWorkbookType type, string? filename)
	{
		this.data = data;
		this.type = type;
		this.filename = filename;
	}

	public async Task ExecuteAsync(HttpContext httpContext)
	{
		var response = httpContext.Response;
		response.ContentType = ExcelFileType.GetForWorkbookType(this.type).ContentType;
		if (filename != null)
		{
			response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
		}
		await using (var dw = await ExcelDataWriter.CreateAsync(response.Body, this.type))
		{
			await dw.WriteAsync(data);
		}		
	}

#if MVC
	public Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context)
	{
		return ExecuteAsync(context.HttpContext);
	}
#endif

}

public class ExcelResult<T> : ExcelResult
	where T : class
{
	public ExcelResult(IEnumerable<T> data, ExcelWorkbookType type, string? filename = null)
		: base(data.AsDataReader(), type, filename) { }

	public ExcelResult(IAsyncEnumerable<T> data, ExcelWorkbookType type, string? filename = null)
		: base(data.AsDataReader(), type, filename) { }
}
