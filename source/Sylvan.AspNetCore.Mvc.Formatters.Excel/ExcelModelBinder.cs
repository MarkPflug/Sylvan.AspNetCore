using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sylvan.Data;
using Sylvan.Data.Excel;
using System.Data.Common;

namespace Sylvan.AspNetCore.Mvc.Formatters;

public class ExcelModelBinderProvider : IModelBinderProvider
{
	public IModelBinder? GetBinder(ModelBinderProviderContext context)
	{
		var elemType = context.Metadata.ElementType;
		if (context.Metadata.ModelType == typeof(DbDataReader))
		{
			return ExcelDataBinder.Instance;
		}
		if (elemType != null)
		{
			return GetExcelObjectBinder(elemType);
		}
		return null;
	}

	public IModelBinder? GetExcelObjectBinder(Type type)
	{
		// TODO: cache for T?
		var bindertype = typeof(ExcelObjectBinder<>).MakeGenericType(type);
		return Activator.CreateInstance(bindertype) as IModelBinder;
	}
}

class ExcelObjectBinder<T> : IModelBinder where T : class, new()
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
			var workbookType = ExcelConstants.GetWorkbookType(file.ContentType);
			if (workbookType != ExcelWorkbookType.Unknown)
			{
				var stream = file.OpenReadStream();
				var edr = ExcelDataReader.Create(stream, workbookType);
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

class ExcelDataBinder : IModelBinder
{
	public static ExcelDataBinder Instance = new ExcelDataBinder();

	public Task BindModelAsync(ModelBindingContext bindingContext)
	{
		var files = bindingContext.HttpContext.Request.Form.Files;
		var file = files.GetFile(bindingContext.FieldName);
		var type = file.ContentType;
		var workbookType = ExcelConstants.GetWorkbookType(file.ContentType);
		if (workbookType != ExcelWorkbookType.Unknown)
		{
			var stream = file.OpenReadStream();
			var edr = ExcelDataReader.Create(stream, workbookType);
			bindingContext.HttpContext.Response.RegisterForDispose(stream);
			bindingContext.HttpContext.Response.RegisterForDispose(edr);
			bindingContext.Result = ModelBindingResult.Success(edr);
		}
		else
		{
			bindingContext.Result = ModelBindingResult.Failed();
		}
		return Task.CompletedTask;
	}
}