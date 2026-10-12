using KcalTracker.Dominio;

namespace KcalTracker.Servicios;

public sealed record GrupoAlimentoHoy(string Etiqueta, IReadOnlyList<Alimento> Items);

/// <summary>
/// Agrupa el desplegable de Hoy por tipo de alimento (categoría), no por tramo.
/// </summary>
public static class GruposAlimentoHoy
{
    public static IReadOnlyList<GrupoAlimentoHoy> De(IEnumerable<Alimento> alimentos)
        => alimentos
            .GroupBy(a => a.CategoriaAlimento?.Nombre ?? "Sin tipo")
            .Select(g =>
            {
                var cat = g.Select(a => a.CategoriaAlimento).FirstOrDefault(c => c is not null);
                return new
                {
                    Etiqueta = cat?.Nombre ?? "Sin tipo",
                    Orden = cat?.Orden ?? int.MaxValue,
                    Nombre = cat?.Nombre ?? "zzz",
                    Items = (IReadOnlyList<Alimento>)g.OrderBy(a => a.Nombre).ToList()
                };
            })
            .OrderBy(x => x.Orden)
            .ThenBy(x => x.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => new GrupoAlimentoHoy(x.Etiqueta, x.Items))
            .ToList();
}
