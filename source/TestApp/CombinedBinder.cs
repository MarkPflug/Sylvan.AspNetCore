using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestApp;

public class CombinedModelBinderProvider : IModelBinderProvider
{
	IModelBinderProvider[] providers;

	public CombinedModelBinderProvider(params IModelBinderProvider[] providers)
	{
		this.providers = providers;
	}

	public IModelBinder GetBinder(ModelBinderProviderContext context)
	{
		List<IModelBinder> binders = new();

		foreach (var provider in providers)
		{
			var b = provider.GetBinder(context);
			if (b != null)
				binders.Add(b);
		}
		return
			new CombinedModelBinder(binders.ToArray());
	}
}

public class CombinedModelBinder : IModelBinder
{
	IModelBinder[] binders;

	public CombinedModelBinder(IModelBinder[] binders)
	{
		this.binders = binders;
	}

	public async Task BindModelAsync(ModelBindingContext bindingContext)
	{
		foreach (var binder in binders)
		{
			await binder.BindModelAsync(bindingContext);
			if (bindingContext.Result.IsModelSet)
				return;
		}
		bindingContext.Result = ModelBindingResult.Failed();
	}
}