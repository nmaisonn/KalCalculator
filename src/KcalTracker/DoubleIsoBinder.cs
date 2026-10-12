using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KcalTracker;

/// <summary>
/// Los input type=number mandan 180.5 (punto). Con cultura es-UY el punto es miles
/// y 180.5 termina en 1805, o 14.5 queda fuera de rango y no se guarda.
/// </summary>
public sealed class DoubleIsoBinder : IModelBinder
{
    public static bool TryParse(string? crudo, out double valor)
    {
        valor = default;
        if (string.IsNullOrWhiteSpace(crudo))
            return false;
        var normalizado = crudo.Trim().Replace(',', '.');
        return double.TryParse(normalizado, NumberStyles.Float, CultureInfo.InvariantCulture, out valor);
    }

    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var crudo = ctx.ValueProvider.GetValue(ctx.ModelName).FirstValue;
        if (string.IsNullOrWhiteSpace(crudo))
        {
            if (ctx.ModelMetadata.IsReferenceOrNullableType)
                ctx.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (TryParse(crudo, out var valor))
            ctx.Result = ModelBindingResult.Success(valor);

        return Task.CompletedTask;
    }
}

public sealed class DoubleIsoBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext ctx)
        => ctx.Metadata.UnderlyingOrModelType == typeof(double) ? new DoubleIsoBinder() : null;
}
