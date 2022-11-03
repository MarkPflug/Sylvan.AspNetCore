//using Microsoft.AspNetCore.Mvc.ModelBinding;
//using Sylvan.Data;
//using Sylvan.Data.Csv;
//using System.Data.Common;

//namespace Sylvan.AspNetCore.Mvc.Formatters;

///// <summary>
///// A model binder that binds CSV files to controller action arguments.
///// </summary>
//public class CsvModelBinderProvider : IModelBinderProvider
//{
//	IModelBinder? IModelBinderProvider.GetBinder(ModelBinderProviderContext context)
//	{
//		var elemType = context.Metadata.ElementType;
//		if (context.Metadata.ModelType == typeof(DbDataReader))
//		{
//			return CsvDataBinder.Instance;
//		}
//		if (elemType != null)
//		{
//			return GetCsvObjectBinder(elemType);
//		}
//		return null;
//	}

//	IModelBinder? GetCsvObjectBinder(Type type)
//	{
//		var bindertype = typeof(CsvObjectBinder<>).MakeGenericType(type);
//		return Activator.CreateInstance(bindertype) as IModelBinder;
//	}
//}

//class CsvObjectBinder<T> : IModelBinder where T : class, new()
//{
//	public Task BindModelAsync(ModelBindingContext bindingContext)
//	{
//		var files = bindingContext.HttpContext.Request.Form.Files;
		
//		var file = files.GetFile(bindingContext.FieldName);
//		if (file == null)
//		{
//			bindingContext.Result = ModelBindingResult.Failed();
//		}
//		else
//		{
//			if (file.ContentType == CsvConstants.CsvContentType)
//			{
//				var stream = file.OpenReadStream();
//				var tr = new StreamReader(stream);
//				var edr = CsvDataReader.Create(tr);
//				bindingContext.HttpContext.Response.RegisterForDispose(tr);
//				bindingContext.HttpContext.Response.RegisterForDispose(edr);
//				// TODO: don't need to materialize everything with ToList
//				var records = edr.GetRecords<T>().ToList();
//				bindingContext.Result = ModelBindingResult.Success(records);
//			}
//			else
//			{
//				bindingContext.Result = ModelBindingResult.Failed();
//			}
//		}
//		return Task.CompletedTask;
//	}
//}

//class CsvDataBinder : IModelBinder
//{
//	public static CsvDataBinder Instance = new CsvDataBinder();

//	public Task BindModelAsync(ModelBindingContext bindingContext)
//	{
//		var files = bindingContext.HttpContext.Request.Form.Files;
//		var file = files.GetFile(bindingContext.FieldName);
//		var type = file.ContentType;
//		if (type == CsvConstants.CsvContentType)
//		{
//			var stream = file.OpenReadStream();
//			var tr = new StreamReader(stream);
//			var cdr = CsvDataReader.Create(tr);
//			bindingContext.HttpContext.Response.RegisterForDispose(stream);
//			bindingContext.HttpContext.Response.RegisterForDispose(cdr);
//			bindingContext.Result = ModelBindingResult.Success(cdr);
//		}
//		else
//		{
//			bindingContext.Result = ModelBindingResult.Failed();
//		}
//		return Task.CompletedTask;
//	}
//}