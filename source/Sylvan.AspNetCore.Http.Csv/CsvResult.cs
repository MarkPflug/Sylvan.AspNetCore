using Microsoft.AspNetCore.Http;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System.Data.Common;
using System.Text;

#if MVC
namespace Sylvan.AspNetCore.Mvc;
#else
namespace Sylvan.AspNetCore.Http;
#endif

public class CsvResult :
#if MVC
	Microsoft.AspNetCore.Mvc.IActionResult
#else
	IResult
#endif

{
	const string CsvContentType = "text/csv";

	readonly DbDataReader data;
	readonly string? filename;

	public CsvResult(DbDataReader data, string? filename = null)
	{
		this.data = data;
		this.filename = filename;
	}

	public async Task ExecuteAsync(HttpContext httpContext)
	{
		var response = httpContext.Response;
		response.ContentType = CsvContentType;
		response.StatusCode = StatusCodes.Status200OK;

		if (filename != null)
		{
			response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
		}
		await using var writer = new StreamWriter(response.Body, Encoding.UTF8);
		await using var csv = CsvDataWriter.Create(writer);
		await csv.WriteAsync(data);
	}

#if MVC
	public Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context)
	{
		return ExecuteAsync(context.HttpContext);
	}
#endif
}

public class CsvResult<T> : CsvResult
	where T : class
{
	public CsvResult(IEnumerable<T> data) : base(data.AsDataReader()) { }
	public CsvResult(IAsyncEnumerable<T> data) : base(data.AsDataReader()) { }
}
