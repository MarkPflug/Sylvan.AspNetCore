using Sylvan.AspNetCore.Mvc;
using System.Data.Common;

namespace Microsoft.AspNetCore.Mvc
{
	/// <summary>
	/// Provides convenience extension methods for returning Excel responses
	/// from controller actions.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Returns an Excel result.
		/// </summary>
		public static IActionResult Excel<T>(this ControllerBase context, IEnumerable<T> seq)
			where T : class
		{
			return new ExcelResult<T>(seq);
		}

		/// <summary>
		/// Returns an Excel result.
		/// </summary>
		public static IActionResult Excel<T>(this ControllerBase context, IAsyncEnumerable<T> seq)
			where T : class
		{
			return new ExcelResult<T>(seq);
		}

		/// <summary>
		/// Returns an Excel result.
		/// </summary>
		public static IActionResult Excel(this ControllerBase context, DbDataReader data)
		{
			return new ExcelResult(data);
		}
	}
}

namespace Microsoft.AspNetCore.Http
{
	/// <summary>
	/// Provides convenience extension methods for returning Excel responses
	/// from minimal APIs.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Returns an Excel result.
		/// </summary>
		public static IResult Excel<T>(this IResultExtensions context, IEnumerable<T> seq)
			where T : class
		{
			return new ExcelResult<T>(seq);
		}

		/// <summary>
		/// Returns an Excel result.
		/// </summary>
		public static IResult Excel<T>(this IResultExtensions context, IAsyncEnumerable<T> seq)
			where T : class
		{
			return new ExcelResult<T>(seq);
		}

		/// <summary>
		/// Returns an Excel result.
		/// </summary>
		public static IResult Excel(this IResultExtensions context, DbDataReader data)
		{
			return new ExcelResult(data);
		}
	}
}