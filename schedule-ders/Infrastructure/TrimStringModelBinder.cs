using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace schedule_ders.Infrastructure;

/// <summary>
/// Trims leading/trailing whitespace from every string bound through MVC model binding
/// (form fields, query params, route values, JSON body properties).
/// Registered globally via TrimStringModelBinderProvider in Program.cs.
/// </summary>
public sealed class TrimStringModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var raw = valueProviderResult.FirstValue;
        if (raw is null)
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(raw.Trim());
        return Task.CompletedTask;
    }
}

public sealed class TrimStringModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Metadata.ModelType == typeof(string) ? new TrimStringModelBinder() : null;
    }
}
