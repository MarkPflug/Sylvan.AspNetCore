using Sylvan.AspNetCore.Http;
using System.Data.Common;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides convenience extension methods for returning JSON data responses
/// from minimal APIs.
/// </summary>
public static class SylvanJsonDataExtensions
{
	public static IResult AsJsonData<T>(this IEnumerable<T> data, string? filename = null) where T : class
	{
		return new JsonDataResult<T>(data,  filename);
	}

	public static IResult ToJsonData(this DbDataReader data, string? filename = null) 
	{
		return new JsonDataResult(data, filename);
	}

	/// <summary>
	/// Returns a JSON data result.
	/// </summary>
	public static IResult JsonData<T>(
		this IResultExtensions context, 
		IEnumerable<T> seq, 
		string? filename = null
	)
		where T : class
	{
		return new JsonDataResult<T>(seq, filename);
	}

	/// <summary>
	/// Returns an JSON data result.
	/// </summary>
	public static IResult JsonData<T>(
		this IResultExtensions context, 
		IAsyncEnumerable<T> seq, 
		string? filename = null
	)
		where T : class
	{
		return new JsonDataResult<T>(seq, filename);
	}

	/// <summary>
	/// Returns an JSON data result.
	/// </summary>
	public static IResult JsonData(
		this IResultExtensions context, 
		DbDataReader data,
		string? filename = null
	)
	{
		return new JsonDataResult(data, filename);
	}
}