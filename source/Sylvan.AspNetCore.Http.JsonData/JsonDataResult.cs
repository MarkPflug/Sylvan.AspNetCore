namespace Sylvan.AspNetCore.Http;

using Microsoft.AspNetCore.Http;
using Sylvan.Data;
using System.Data.Common;

public class JsonDataResult : IResult
{
	readonly DbDataReader data;
	readonly string? filename;

	public JsonDataResult(DbDataReader data, string? filename)
	{
		this.data = data;
		this.filename = filename;
	}

	public async Task ExecuteAsync(HttpContext httpContext)
	{
		var response = httpContext.Response;
		
		response.ContentType = "application/json; charset=utf-8";
		if (filename != null)
		{
			response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
		}

		await data.WriteJsonAsync(response.Body);
		await data.DisposeAsync();
	}
}

public class JsonDataResult<T> : JsonDataResult
	where T : class
{
	public JsonDataResult(IEnumerable<T> data, string? filename = null)
		: base(data.AsDataReader(),  filename) { }

	public JsonDataResult(IAsyncEnumerable<T> data, string? filename = null)
		: base(data.AsDataReader(), filename) { }
}
