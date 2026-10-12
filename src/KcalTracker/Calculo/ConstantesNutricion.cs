using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

public sealed record ConstantesSugeridas(
    double KcalPorKgGrasa,
    double KcalPorKgMagro,
    double PctGastoMax,
    double PctPesoSemanal,
    double CobMinContraste,
    double CobMinCalibrado);

/// <summary>
/// Valores de partida con base fisiológica. El usuario puede pisarlos.
/// Grasa: 9,4 kcal/g de lípido × 87 % del tejido adiposo (Hall/Forbes).
/// Magro: 1,8 kcal/g (modelo de Hall). Techos más conservadores si hay poca grasa.
/// </summary>
public static class ConstantesNutricion
{
    public const double KcalPorGLipido = 9.4;
    public const double FraccionLipidoAdiposo = 0.87;
    public const double KcalPorGMagro = 1.8;

    public static double KcalPorKgGrasaBase
        => Math.Round(KcalPorGLipido * FraccionLipidoAdiposo * 1000, MidpointRounding.AwayFromZero);

    public static double KcalPorKgMagroBase
        => Math.Round(KcalPorGMagro * 1000, MidpointRounding.AwayFromZero);

    public static ConstantesSugeridas Calcular(double? grasaPct)
    {
        var gastoMax = 0.25;
        var pesoSem = 0.0075;
        if (grasaPct is > 0 and < 100)
        {
            // 10 % + 0,8 puntos por cada 1 % de grasa. Piso 15 %, techo 30 % (Helms/ACSM).
            gastoMax = Limitar(0.10 + grasaPct.Value * 0.008, 0.15, 0.30);
            gastoMax = APaso(gastoMax, 0.001);
            // 0,4 % + 0,02 puntos por cada 1 % de grasa. Piso 0,5 %/sem, techo 1 %.
            pesoSem = Limitar(0.004 + grasaPct.Value * 0.0002, 0.005, 0.010);
            pesoSem = APaso(pesoSem, 0.0005);
        }

        return new ConstantesSugeridas(
            KcalPorKgGrasaBase,
            KcalPorKgMagroBase,
            gastoMax,
            pesoSem,
            0.50,
            0.70);
    }

    /// <summary>
    /// Recomp: la mitad del techo de corte efectivo, entre 8 y 15 % del GET.
    /// Sin % de grasa y sin override se usa el 10 % ISSN.
    /// </summary>
    public static double PctRecomp(double? grasaPct, double? pctCorteEfectivo = null)
    {
        if (pctCorteEfectivo is > 0)
            return Limitar(APaso(pctCorteEfectivo.Value * 0.5, 0.001), 0.08, 0.15);
        if (grasaPct is null or <= 0 or >= 100)
            return ValoresDeFabrica.PctDeficitRecomp;
        return Limitar(APaso(Calcular(grasaPct).PctGastoMax * 0.5, 0.001), 0.08, 0.15);
    }

    /// <summary>
    /// Superávit de ganancia: ~10 % del GET, un poco más si estás magro
    /// y menos si hay más grasa (5–12 %).
    /// </summary>
    public static double PctSuperavit(double? grasaPct)
    {
        if (grasaPct is null or <= 0 or >= 100)
            return 0.10;
        return APaso(Math.Clamp(0.10 + (15 - grasaPct.Value) * 0.004, 0.05, 0.12), 0.001);
    }

    /// <summary>
    /// Techos de corte: Helms/ACSM según el % actual, salvo que el usuario
    /// haya guardado un valor distinto al de fábrica.
    /// </summary>
    public static ConstantesSugeridas TechosDePlan(double? grasaPct, double cfgPctGasto, double cfgPctPeso)
    {
        var sug = Calcular(grasaPct);
        return sug with
        {
            PctGastoMax = Distinto(cfgPctGasto, ValoresDeFabrica.PctGastoMax) ? cfgPctGasto : sug.PctGastoMax,
            PctPesoSemanal = Distinto(cfgPctPeso, ValoresDeFabrica.PctPesoSemanal) ? cfgPctPeso : sug.PctPesoSemanal
        };
    }

    private static bool Distinto(double a, double b) => Math.Abs(a - b) > 0.00015;

    private static double Limitar(double v, double min, double max)
        => Math.Clamp(v, min, max);

    private static double APaso(double v, double paso)
        => Math.Round(v / paso, MidpointRounding.AwayFromZero) * paso;
}
