using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KcalTracker;

/// <summary>
/// Acepta yyyy-MM-dd (inputs type=date y hidden) además de dd/MM/yyyy de es-UY.
/// Sin esto, un POST con Fecha=2026-09-17 no bindea y el entrenamiento se guarda en otra fecha.
/// </summary>
public sealed class DateOnlyIsoBinder : IModelBinder
{
    private static readonly string[] Exactos =
        ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy"];

    public static bool TryParse(string? crudo, out DateOnly fecha)
    {
        fecha = default;
        if (string.IsNullOrWhiteSpace(crudo))
            return false;

        foreach (var formato in Exactos)
        {
            if (DateOnly.TryParseExact(crudo, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
                return true;
        }

        return DateOnly.TryParse(crudo, CultureInfo.GetCultureInfo("es-UY"), DateTimeStyles.None, out fecha)
            || DateOnly.TryParse(crudo, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
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

        if (TryParse(crudo, out var fecha))
            ctx.Result = ModelBindingResult.Success(fecha);

        return Task.CompletedTask;
    }
}

public sealed class DateOnlyIsoBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext ctx)
    {
        var t = ctx.Metadata.UnderlyingOrModelType;
        return t == typeof(DateOnly) ? new DateOnlyIsoBinder() : null;
    }
}
