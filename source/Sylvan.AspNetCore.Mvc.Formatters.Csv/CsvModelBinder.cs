using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sylvan.AspNetCore.Mvc.Formatters;

/// <summary>
/// 
/// </summary>
public class CsvModelBinderProvider : IModelBinderProvider
{
	/// <summary>
	/// 
	/// </summary>
	public IModelBinder? GetBinder(ModelBinderProviderContext context)
	{
		var elemType = context.Metadata.ElementType;
		if (context.Metadata.ModelType == typeof(DbDataReader))
		{
			return CsvDataBinder.Instance;
		}
		if (elemType != null)
		{
			return GetCsvObjectBinder(elemType);
		}
		return null;
	}

	/// <summary>
	/// 
	/// </summary>
	public IModelBinder? GetCsvObjectBinder(Type type)
	{
		// TODO: cache for T?
		var bindertype = typeof(CsvObjectBinder<>).MakeGenericType(type);
		return Activator.CreateInstance(bindertype) as IModelBinder;
	}
}

class CsvObjectBinder<T> : IModelBinder where T : class, new()
{
	public Task BindModelAsync(ModelBindingContext bindingContext)
	{
		var files = bindingContext.HttpContext.Request.Form.Files;
		var file = files.GetFile(bindingContext.FieldName);
		if (file == null)
		{
			bindingContext.Result = ModelBindingResult.Failed();
		}
		else
		{
			if (file.ContentType == CsvConstants.CsvContentType)
			{
				var stream = file.OpenReadStream();
				var tr = new StreamReader(stream);
				var edr = CsvDataReader.Create(tr);
				// TODO: don't need to materialize everything with ToList
				var records = edr.GetRecords<T>().ToList();
				bindingContext.Result = ModelBindingResult.Success(records);
			}
			else
			{
				bindingContext.Result = ModelBindingResult.Failed();
			}
		}
		return Task.CompletedTask;
	}
}

class CsvDataBinder : IModelBinder
{
	public static CsvDataBinder Instance = new CsvDataBinder();

	public Task BindModelAsync(ModelBindingContext bindingContext)
	{
		var files = bindingContext.HttpContext.Request.Form.Files;
		var file = files.GetFile(bindingContext.FieldName);
		var type = file.ContentType;
		if (type == CsvConstants.CsvContentType)
		{
			var stream = file.OpenReadStream();
			var tr = new StreamReader(stream);
			var cdr = CsvDataReader.Create(tr);
			bindingContext.HttpContext.Response.RegisterForDispose(stream);
			bindingContext.HttpContext.Response.RegisterForDispose(cdr);
			bindingContext.Result = ModelBindingResult.Success(cdr);
		}
		else
		{
			bindingContext.Result = ModelBindingResult.Failed();
		}
		return Task.CompletedTask;
	}
}