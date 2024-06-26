using Microsoft.AspNetCore.Builder;
using Sylvan.AspNetCore.Http;
using System.Data.Common;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides convenience extension methods for returning CSV responses
/// from controller actions.
/// </summary>
public static class SylvanCsvExtensions
{
	public static void F(this WebApplicationBuilder b) {
		
	}

	/// <summary>
	/// Returns a CSV result.
	/// </summary>
	public static IResult Csv<T>(this IResultExtensions context, IEnumerable<T> seq)
		where T : class
	{
		return new CsvResult<T>(seq);
	}

	/// <summary>
	/// Returns a CSV result.
	/// </summary>
	public static IResult Csv<T>(this IResultExtensions context, IAsyncEnumerable<T> seq)
		where T : class
	{
		return new CsvResult<T>(seq);
	}

	/// <summary>
	/// Returns a CSV result.
	/// </summary>
	public static IResult Csv(this IResultExtensions context, DbDataReader data)
	{
		return new CsvResult(data);
	}
}
