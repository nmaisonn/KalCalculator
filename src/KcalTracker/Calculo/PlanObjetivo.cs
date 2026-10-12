using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

public sealed record ResultadoPlan(
    double MasaMagraKg,
    double PesoObjetivoKg,
    double GrasaActualKg,
    double GrasaObjetivoKg,
    double GrasaAPerderKg,
    double KcalTotales,
    double DeficitDiario,
    double TechoDiario,
    bool EsSostenible,
    double KcalPorKgGrasaAplicadas,
    PlazoCorrido? Plazo = null,
    bool UsaComposicion = true,
    double PctGastoMaxAplicado = 0,
    double PctPesoSemanalAplicado = 0);

/// <summary>RN-04 y RN-05. Las constantes salen de la configuración del usuario.</summary>
public static class PlanObjetivo
{
    /// <summary>
    /// Con % de grasa: kg a perder en el plazo (RN-04).
    /// Sin % de grasa: recorte como % del gasto cotidiano (ISSN), topeado por el techo.
    /// </summary>
    public static ResultadoPlan? De(
        double pesoKg,
        double? grasaPct,
        double? grasaObjetivoPct,
        int plazoSemanas,
        double kcalPorKgGrasa,
        double gastoDelDia,
        double pctGastoMax,
        double pctPesoSemanal,
        DateOnly? inicio = null,
        DateOnly? hoy = null)
    {
        if (grasaPct is > 0 and < 100 && grasaObjetivoPct is >= 0 and < 100)
        {
            return Calcular(
                pesoKg, grasaPct.Value, grasaObjetivoPct.Value, plazoSemanas,
                kcalPorKgGrasa, gastoDelDia, pctGastoMax, pctPesoSemanal, inicio, hoy);
        }

        return PorEnergia(pesoKg, plazoSemanas, kcalPorKgGrasa, gastoDelDia,
            pctGastoMax, pctPesoSemanal, inicio, hoy);
    }

    /// <summary>
    /// Techos Helms/ACSM según el % de grasa, salvo override a mano en configuración.
    /// </summary>
    public static ResultadoPlan? De(
        double pesoKg,
        double? grasaPct,
        double? grasaObjetivoPct,
        int plazoSemanas,
        double kcalPorKgGrasa,
        double gastoDelDia,
        Configuracion cfg,
        DateOnly? inicio = null,
        DateOnly? hoy = null)
    {
        var t = ConstantesNutricion.TechosDePlan(grasaPct, cfg.PctGastoMax, cfg.PctPesoSemanal);
        return De(pesoKg, grasaPct, grasaObjetivoPct, plazoSemanas, kcalPorKgGrasa, gastoDelDia,
            t.PctGastoMax, t.PctPesoSemanal, inicio, hoy);
    }

    public static ResultadoPlan? Calcular(
        double pesoKg,
        double grasaPct,
        double grasaObjetivoPct,
        int plazoSemanas,
        double kcalPorKgGrasa,
        double gastoDelDia,
        double pctGastoMax,
        double pctPesoSemanal,
        DateOnly? inicio = null,
        DateOnly? hoy = null)
    {
        if (grasaObjetivoPct >= 100 || plazoSemanas <= 0) return null;

        var corrido = PlazoCorrido.De(plazoSemanas, inicio, hoy);
        var diasDelPlan = Math.Max(1, plazoSemanas * 7);
        var masaMagra = pesoKg * (1 - grasaPct / 100);
        var pesoObjetivo = masaMagra / (1 - grasaObjetivoPct / 100);
        var grasaObjetivoKg = pesoObjetivo - masaMagra;
        var grasaActualKg = pesoKg * (grasaPct / 100);
        var grasaAPerder = grasaActualKg - grasaObjetivoKg;
        var kcalTotales = grasaAPerder * kcalPorKgGrasa;
        var deficitDiario = kcalTotales / diasDelPlan;

        var techoPorGasto = gastoDelDia * pctGastoMax;
        var techoPorPeso = pesoKg * pctPesoSemanal * kcalPorKgGrasa / 7;
        var techo = Math.Min(techoPorGasto, techoPorPeso);

        return new ResultadoPlan(
            masaMagra, pesoObjetivo, grasaActualKg, grasaObjetivoKg, grasaAPerder,
            kcalTotales, deficitDiario, techo,
            deficitDiario <= techo + 0.5,
            kcalPorKgGrasa,
            corrido,
            true,
            pctGastoMax,
            pctPesoSemanal);
    }

    /// <summary>
    /// Sin composición: el recorte es un % del GET (ISSN 10–20 %),
    /// nunca mayor que el techo de gasto/peso.
    /// </summary>
    public static ResultadoPlan? PorEnergia(
        double pesoKg,
        int plazoSemanas,
        double kcalPorKgGrasa,
        double gastoDelDia,
        double pctGastoMax,
        double pctPesoSemanal,
        DateOnly? inicio = null,
        DateOnly? hoy = null)
    {
        if (pesoKg <= 0 || plazoSemanas <= 0 || gastoDelDia <= 0) return null;

        var corrido = PlazoCorrido.De(plazoSemanas, inicio, hoy);
        var techoPorGasto = gastoDelDia * pctGastoMax;
        var techoPorPeso = pesoKg * pctPesoSemanal * kcalPorKgGrasa / 7;
        var techo = Math.Min(techoPorGasto, techoPorPeso);
        var corte = Math.Min(techo, gastoDelDia * ValoresDeFabrica.PctDeficitCorte);
        var kcalTotales = corte * Math.Max(1, plazoSemanas * 7);

        return new ResultadoPlan(
            0, pesoKg, 0, 0, 0,
            kcalTotales, corte, techo,
            corte <= techo + 0.5,
            kcalPorKgGrasa,
            corrido,
            false,
            pctGastoMax,
            pctPesoSemanal);
    }
}
