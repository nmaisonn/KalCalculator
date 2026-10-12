using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

/// <summary>
/// Par de pesadas comparables para contrastar el modelo con la báscula:
/// la última en condiciones (ayunas, misma hora) contra la anterior comparable.
/// </summary>
public static class ContrasteComposicion
{
    public static (Medicion Inicial, Medicion Ultima)? Par(
        IEnumerable<Medicion> mediciones,
        DateOnly desde,
        DateOnly hasta)
    {
        var lista = mediciones
            .Where(m => m.EsComparable && m.GrasaPct is not null)
            .OrderBy(m => m.Fecha)
            .ThenBy(m => m.Id)
            .ToList();

        var ultima = lista.LastOrDefault(m => m.Fecha <= hasta);
        if (ultima is null) return null;

        var inicial = lista.FirstOrDefault(m => m.Fecha >= desde && m.Fecha < ultima.Fecha);
        if (inicial is null)
            inicial = lista.LastOrDefault(m => m.Fecha < ultima.Fecha);

        return inicial is null ? null : (inicial, ultima);
    }

    public static double? GrasaPerdidaKg(Medicion inicial, Medicion ultima)
    {
        var a = Composicion.GrasaKg(inicial.PesoKg, inicial.GrasaPct);
        var b = Composicion.GrasaKg(ultima.PesoKg, ultima.GrasaPct);
        if (a is null || b is null) return null;
        return a.Value - b.Value;
    }
}
