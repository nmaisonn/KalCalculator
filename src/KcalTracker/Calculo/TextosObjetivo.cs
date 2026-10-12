using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

public static class TextosObjetivo
{
    public static string TechoCorte(double? grasaPct, bool porEnergia, double? pctCorteEfectivo = null)
    {
        if (porEnergia)
            return $"techo {Formato.Pct(ValoresDeFabrica.PctDeficitCorte)} de tu gasto cotidiano (ISSN)";
        var pct = PctCorteMostrar(grasaPct, pctCorteEfectivo);
        return $"techo {Formato.Pct(pct)} de tu gasto cotidiano ({FuenteCorte(grasaPct, pct)}); necesario es el % en el plazo";
    }

    public static string TechoRecomp(double? grasaPct, bool porEnergia, double? pctCorteEfectivo = null)
    {
        if (porEnergia)
            return $"techo {Formato.Pct(ValoresDeFabrica.PctDeficitRecomp)} de tu gasto cotidiano (ISSN)";
        var pct = ConstantesNutricion.PctRecomp(grasaPct, pctCorteEfectivo);
        return $"techo {Formato.Pct(pct)} de tu gasto cotidiano ({FuenteRecomp(grasaPct, pctCorteEfectivo)}); necesario es el % en el plazo";
    }

    public static string TechoGanancia(double? grasaPct, bool porEnergia)
    {
        if (porEnergia)
            return $"techo {Formato.Pct(0.10)} de tu gasto cotidiano (ISSN, superávit)";
        return $"techo {Formato.Pct(ConstantesNutricion.PctSuperavit(grasaPct))} de tu gasto cotidiano (superávit, según tu % de grasa); no baja el % de grasa";
    }

    public static string DetalleTipo(TipoObjetivo tipo, double? grasaPct, double? pctCorteEfectivo = null) => tipo switch
    {
        TipoObjetivo.Recomposicion =>
            $"Bajar grasa y sumar músculo a {Formato.Pct(ConstantesNutricion.PctRecomp(grasaPct, pctCorteEfectivo))} del gasto; el % entra si ese ritmo alcanza",
        TipoObjetivo.GananciaMusculo =>
            $"Superávit (~{Formato.Pct(ConstantesNutricion.PctSuperavit(grasaPct))} del gasto) para hipertrofia; el peso no baja",
        _ =>
            $"Bajar de peso con un recorte de hasta {Formato.Pct(PctCorteMostrar(grasaPct, pctCorteEfectivo))} del gasto del día"
    };

    private static double PctCorteMostrar(double? grasaPct, double? pctCorteEfectivo)
        => pctCorteEfectivo is > 0 ? pctCorteEfectivo.Value : ConstantesNutricion.Calcular(grasaPct).PctGastoMax;

    private static bool EsManual(double? grasaPct, double? pctCorteEfectivo)
        => pctCorteEfectivo is > 0
           && Math.Abs(pctCorteEfectivo.Value - ConstantesNutricion.Calcular(grasaPct).PctGastoMax) > 0.00015;

    private static string FuenteCorte(double? grasaPct, double pct)
        => EsManual(grasaPct, pct) ? "el techo que definiste en Ajustes" : "Helms/ACSM, según tu % de grasa";

    private static string FuenteRecomp(double? grasaPct, double? pctCorteEfectivo)
        => EsManual(grasaPct, pctCorteEfectivo) ? "la mitad de tu techo de Ajustes" : "según tu % de grasa";
}
