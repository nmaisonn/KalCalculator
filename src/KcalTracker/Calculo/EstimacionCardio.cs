using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

/// <summary>
/// Gasto de cinta según ACSM (caminar &lt; 8 km/h, correr a partir de 8).
/// El neto resta lo que ya cubre el gasto cotidiano de esos minutos (RN-02).
/// </summary>
public static class EstimacionCardio
{
    public static double BrutoKcal(double minutos, double? velocidadKmh, double? inclinacionPct, double pesoKg)
    {
        if (minutos <= 0 || pesoKg <= 0) return 0;
        var v = velocidadKmh ?? 5;
        var g = (inclinacionPct ?? 0) / 100.0;
        var mMin = v * 1000.0 / 60.0;
        var vo2 = v >= 8
            ? 0.2 * mMin + 0.9 * mMin * g + 3.5
            : 0.1 * mMin + 1.8 * mMin * g + 3.5;
        var kcalMin = vo2 * pesoKg / 1000.0 * 5.0;
        return kcalMin * minutos;
    }

    public static double NetoKcal(double bruto, double minutos, double gastoSinDeporteDiario)
    {
        if (minutos <= 0) return Math.Max(0, bruto);
        var reposo = gastoSinDeporteDiario / 1440.0 * minutos;
        return Math.Max(0, bruto - reposo);
    }

    public static double DeBloques(
        IEnumerable<BloqueCardio> bloques,
        double pesoKg,
        double? gastoSinDeporteDiario = null)
    {
        double bruto = 0;
        double minutos = 0;
        foreach (var b in bloques)
        {
            var m = b.Minutos * Math.Max(1, b.Repeticiones);
            bruto += BrutoKcal(m, b.Velocidad, b.Inclinacion, pesoKg);
            minutos += m;
        }

        return gastoSinDeporteDiario is { } g
            ? NetoKcal(bruto, minutos, g)
            : bruto;
    }

    public static double DeProtocolo(ProtocoloCardio protocolo, double pesoKg, double? gastoSinDeporteDiario = null)
        => protocolo.Kcal is > 0
            ? protocolo.Kcal.Value
            : DeBloques(protocolo.Bloques, pesoKg, gastoSinDeporteDiario);

    public static double DeSesion(
        double minutos,
        double? velocidad,
        double? inclinacion,
        IEnumerable<BloqueCardio>? bloques,
        double pesoKg,
        double? gastoSinDeporteDiario)
    {
        var lista = bloques?.ToList();
        if (lista is { Count: > 0 })
            return DeBloques(lista, pesoKg, gastoSinDeporteDiario);

        var bruto = BrutoKcal(minutos, velocidad, inclinacion, pesoKg);
        return gastoSinDeporteDiario is { } g
            ? NetoKcal(bruto, minutos, g)
            : bruto;
    }
}
