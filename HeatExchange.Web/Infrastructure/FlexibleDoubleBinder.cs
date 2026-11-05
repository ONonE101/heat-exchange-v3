using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HeatExchange.Web.Infrastructure;

public sealed class FlexibleDoubleBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        string normalized = (value.FirstValue ?? "").Trim().Replace(',', '.');
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number))
            context.Result = ModelBindingResult.Success(number);
        else context.ModelState.TryAddModelError(context.ModelName, "Введите число, например 0,78 или 0.78.");
        return Task.CompletedTask;
    }
}

public sealed class FlexibleDoubleBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) => context.Metadata.ModelType == typeof(double)
        ? new FlexibleDoubleBinder() : null;
}
