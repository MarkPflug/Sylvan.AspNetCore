using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Sylvan.AspNetCore.Mvc.Formatters;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System.Data.Common;
using System.Text;

namespace Sylvan.AspNetCore.Mvc;

class CsvResult : IResult, IActionResult, IStatusCodeActionResult
{
	readonly DbDataReader data;

	public CsvResult(DbDataReader data)
	{
		this.data = data;
	}

	public int? StatusCode => StatusCodes.Status200OK;

	public async Task ExecuteAsync(HttpContext httpContext)
	{
		var response = httpContext.Response;
		response.ContentType = CsvConstants.CsvContentType;
		await using var writer = new StreamWriter(response.Body, Encoding.UTF8);
		var csv = CsvDataWriter.Create(writer);
		await csv.WriteAsync(data);
		await response.CompleteAsync();
	}

	public Task ExecuteResultAsync(ActionContext context)
	{
		return ExecuteAsync(context.HttpContext);
	}
}

class CsvResult<T> : CsvResult
	where T : class
{
	public CsvResult(IEnumerable<T> data) : base(data.AsDataReader()) { }
	public CsvResult(IAsyncEnumerable<T> data) : base(data.AsDataReader()) { }
}
