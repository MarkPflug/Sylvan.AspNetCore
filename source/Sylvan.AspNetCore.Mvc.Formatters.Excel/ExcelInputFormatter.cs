using Microsoft.AspNetCore.Mvc.Formatters;
using Sylvan.Data;
using Sylvan.Data.Excel;
using Sylvan.IO;
using System.Buffers;
using System.Data;
using System.Data.Common;

namespace Sylvan.AspNetCore.Mvc.Formatters;

/// <summary>
/// Input formatter for converting text/csv HTTP request body.
/// </summary>
public class ExcelInputFormatter : InputFormatter
{
	static Dictionary<string, ExcelWorkbookType> MimeMap;

	static ExcelInputFormatter()
	{
		MimeMap = new Dictionary<string, ExcelWorkbookType>(StringComparer.OrdinalIgnoreCase);
		MimeMap.Add(ExcelConstants.XlsxContentType, ExcelWorkbookType.ExcelXml);
		MimeMap.Add(ExcelConstants.XlsbContentType, ExcelWorkbookType.ExcelBinary);
		MimeMap.Add(ExcelConstants.XlsContentType, ExcelWorkbookType.Excel);
	}

	readonly Action<ExcelDataReaderOptions> options;

	/// <summary>
	/// Creates a new CsvInputFormatter.
	/// </summary>
	public ExcelInputFormatter() : this(o => { })
	{
	}

	/// <summary>
	/// Creates a new CsvInputFormatter.
	/// </summary>
	/// <param name="options">Allows customizing the CsvDataReaderOptions.</param>
	public ExcelInputFormatter(Action<ExcelDataReaderOptions> options)
	{
		this.options = options;
		SupportedMediaTypes.Add(ExcelConstants.XlsxContentType);
		SupportedMediaTypes.Add(ExcelConstants.XlsbContentType);
		SupportedMediaTypes.Add(ExcelConstants.XlsContentType);
	}

	static ExcelWorkbookType GetWorkbookType(string contentType)
	{
		return
			MimeMap.TryGetValue(contentType, out var value)
			? value
			: ExcelWorkbookType.Unknown;
	}

	/// <inheritdoc/>
	public async override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
	{
		var opts = new ExcelDataReaderOptions();
		this.options?.Invoke(opts);
		opts.OwnsStream = true;

		var ms = new PooledMemoryStream();

		await context.HttpContext.Request.Body.CopyToAsync(ms);
		ms.Seek(0, SeekOrigin.Begin);

		var contentType = context.HttpContext.Request.ContentType;
		if (contentType == null)
		{
			return InputFormatterResult.Failure();
		}
		var workbookType = GetWorkbookType(contentType);

		var edr = ExcelDataReader.Create(ms, workbookType, opts);

		context.HttpContext.Response.RegisterForDispose(ms);
		context.HttpContext.Response.RegisterForDispose(edr);

		var modelType = context.ModelType;

		if (modelType == typeof(DbDataReader) || modelType == typeof(IDataReader) || modelType == typeof(ExcelDataReader))
		{
			return InputFormatterResult.Success(edr);
		}

		if (modelType.IsGenericType)
		{
			var genType = modelType.GetGenericTypeDefinition();

			var schema = edr.GetColumnSchema();

			if (genType == typeof(IAsyncEnumerable<>) || genType == typeof(IEnumerable<>))
			{
				var targetType = modelType.GetGenericArguments()[0];
				var binder = FormatterUtils.GetObjectBinder(targetType, schema, new DataBinderOptions { BindingMode = DataBindingMode.Any, InferColumnTypeFromMember = true });

				var fac = FormatterUtils.GetReaderFactory(modelType);
				var seqReader = fac(binder, edr);
				return InputFormatterResult.Success(seqReader);
			}
		}
		return InputFormatterResult.Failure();
	}
}

///// <summary>
///// 
///// </summary>
//public sealed class DebugPool<T> : ArrayPool<T>
//{
//	/// <summary>
//	/// 
//	/// </summary>
//	public static DebugPool<T> Instance = new DebugPool<T>();

//	int rentCount = 0;
//	int reuseCount = 0;

//	int returnCount = 0;
//	HashSet<object> seen = new HashSet<object>();

//	/// <summary>
//	/// 
//	/// </summary>
//	public void DumpStats()
//	{
//		Console.WriteLine($"Rent {rentCount}");
//		Console.WriteLine($"Reuse {reuseCount}");
//		Console.WriteLine($"return {returnCount}");
//	}

//	/// <inheritdoc/>
//	public override T[] Rent(int minimumLength)
//	{
//		rentCount++;
//		T[] arr = ArrayPool<T>.Shared.Rent(minimumLength);
//		if (!seen.Add(arr))
//			reuseCount++;
//		return arr;
//	}
//	/// <inheritdoc/>
//	public override void Return(T[] array, bool clearArray = false)
//	{
//		returnCount++;
//		ArrayPool<T>.Shared.Return(array, clearArray);
//	}
//}

//sealed class StreamWrapper : Stream
//{
//	readonly Stream inner;
	
//	public StreamWrapper(Stream inner)
//	{
//		this.inner = inner;
//	}

//	public override bool CanRead => this.inner.CanRead;

//	public override bool CanSeek => this.inner.CanSeek;

//	public override bool CanWrite => this.inner.CanWrite;

//	public override long Length => this.inner.Length;

//	public override long Position { get => this.inner.Position; set => this.inner.Position = value; }

//	public override void Flush()
//	{
//		this.inner.Flush();
//	}

//	public override int Read(byte[] buffer, int offset, int count)
//	{
//		return this.inner.Read(buffer, offset, count);
//	}

//	public override long Seek(long offset, SeekOrigin origin)
//	{
//		return this.inner.Seek(offset, origin);
//	}

//	public override void SetLength(long value)
//	{
//		this.inner.SetLength(value);
//	}

//	public override void Write(byte[] buffer, int offset, int count)
//	{
//		this.inner.Write(buffer, offset, count);
//	}

//	protected override void Dispose(bool disposing)
//	{
//		base.Dispose(disposing);
//	}

//	public override void Close()
//	{
//		base.Close();
//	}
//}