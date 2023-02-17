using Microsoft.AspNetCore.Mvc;
using Sylvan.AspNetCore.Mvc.Formatters.Csv;
using System.Data.Common;

namespace Microsoft.AspNetCore.Mvc
{

	/// <summary>
	/// Provides convenience extension methods for returning CSV responses
	/// from controller actions.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Returns a CSV result.
		/// </summary>
		public static IActionResult Csv<T>(this ControllerBase context, IEnumerable<T> seq)
			where T : class
		{
			return new CsvResult<T>(seq);
		}

		/// <summary>
		/// Returns a CSV result.
		/// </summary>
		public static IActionResult Csv<T>(this ControllerBase context, IAsyncEnumerable<T> seq)
			where T : class
		{
			return new CsvResult<T>(seq);
		}

		/// <summary>
		/// Returns a CSV result.
		/// </summary>
		public static IActionResult Csv(this ControllerBase context, DbDataReader data)
		{
			return new CsvResult(data);
		}
	}
}

namespace Microsoft.AspNetCore.Http
{

	/// <summary>
	/// Provides convenience extension methods for returning CSV responses
	/// from controller actions.
	/// </summary>
	public static class Extensions
	{
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
}