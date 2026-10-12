namespace KcalTracker.Calculo;

public sealed record DiaDePeriodo(
    DateOnly Fecha,
    bool HayRegistro,
    double? Ingesta,
    double? Proteina,
    double? Carbos,
    double? Grasa,
    double KcalAfuera,
    double Gasto,
    double? Deficit,
    double KcalGym,
    double KcalCardio,
    double KcalDeporte);

public sealed record MetricasPeriodo(
    IReadOnlyList<DiaDePeriodo> Serie,
    int DiasTotales,
    int DiasConRegistro,
    double Cobertura,
    double DeficitAcumulado,
    double DeficitMedio,
    int Adherencia,
    double IngestaMedia,
    double ProteinaMedia,
    double CarbosMedia,
    double GrasaMedia,
    double PctAfuera,
    int DiasAfuera,
    int SesionesGym,
    double KcalGym,
    int SesionesCardio,
    double KcalCardio,
    int SesionesDeporte,
    double KcalDeporte,
    int DiasSinActividad,
    double GrasaEstimadaKg,
    double? GrasaMedidaKg,
    bool MuestraContraste,
    string? LecturaContraste,
    DateOnly? ContrasteDesde,
    DateOnly? ContrasteHasta);

/// <summary>RN-11. Solo cuenta días con registro de comida.</summary>
public static class Periodo
{
    public static MetricasPeriodo Calcular(
        IReadOnlyList<DiaDePeriodo> serie,
        double objetivoDiario,
        double kcalPorKgGrasa,
        double cobMinContraste,
        double? grasaMedidaKg,
        bool esSuperavit = false,
        IReadOnlyList<DiaDePeriodo>? serieContraste = null)
    {
        var conR = serie.Where(d => d.HayRegistro).ToList();
        var n = conR.Count;
        var totales = serie.Count;
        var cobertura = totales == 0 ? 0 : (double)n / totales;
        var acumulado = conR.Sum(d => d.Deficit ?? 0);
        var medio = n == 0 ? 0 : acumulado / n;
        var ingestaTot = conR.Sum(d => d.Ingesta ?? 0);
        var afueraTot = conR.Sum(d => d.KcalAfuera);
        var paraEstimada = serieContraste ?? serie;
        var conEstimada = paraEstimada.Where(d => d.HayRegistro).ToList();
        var acumuladoEstimada = conEstimada.Sum(d => d.Deficit ?? 0);
        var grasaEstimada = kcalPorKgGrasa == 0 ? 0 : acumuladoEstimada / kcalPorKgGrasa;
        var muestra = cobertura >= cobMinContraste && grasaMedidaKg is not null && conEstimada.Count > 0;

        string? lectura = null;
        if (muestra && grasaMedidaKg is { } medida)
        {
            var disc = grasaEstimada - medida;
            lectura = Math.Abs(disc) < 0.15
                ? "El modelo está calibrado: lo estimado y lo medido coinciden."
                : disc > 0
                    ? "Se está subestimando la ingesta, sobreestimando el gasto, o ambas."
                    : "Se está perdiendo más de lo previsto: revisar si parte es masa magra.";
        }

        var adherencia = conR.Count(d =>
            esSuperavit
                ? -(d.Deficit ?? 0) >= objetivoDiario
                : (d.Deficit ?? 0) >= objetivoDiario);

        return new MetricasPeriodo(
            serie, totales, n, cobertura, acumulado, medio,
            adherencia,
            n == 0 ? 0 : ingestaTot / n,
            n == 0 ? 0 : conR.Average(d => d.Proteina ?? 0),
            n == 0 ? 0 : conR.Average(d => d.Carbos ?? 0),
            n == 0 ? 0 : conR.Average(d => d.Grasa ?? 0),
            ingestaTot == 0 ? 0 : afueraTot / ingestaTot,
            conR.Count(d => d.KcalAfuera > 0),
            conR.Count(d => d.KcalGym > 0), conR.Sum(d => d.KcalGym),
            conR.Count(d => d.KcalCardio > 0), conR.Sum(d => d.KcalCardio),
            conR.Count(d => d.KcalDeporte > 0), conR.Sum(d => d.KcalDeporte),
            conR.Count(d => d.KcalGym + d.KcalCardio + d.KcalDeporte == 0),
            grasaEstimada, grasaMedidaKg, muestra, lectura,
            serieContraste is { Count: > 0 } ? serieContraste[0].Fecha : null,
            serieContraste is { Count: > 0 } ? serieContraste[^1].Fecha : null);
    }
}
