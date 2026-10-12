using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

public sealed record ResultadoDeficit(
    double Diario,
    bool EsSuperavit,
    double Corte,
    double Recomposicion,
    double Ganancia,
    TipoObjetivo Tipo,
    bool EsManual,
    double? SemanasAEsteRitmo,
    double TopeCorte = 0,
    double TopeRecomp = 0)
{
    public string Nombre => TipoObjetivoNombres.De(Tipo);
    public string Etiqueta => EsSuperavit ? "Superávit diario" : "Déficit diario";
    public double Necesario => Corte;
    public double TopeDelTipo => Tipo switch
    {
        TipoObjetivo.Recomposicion => TopeRecomp,
        TipoObjetivo.GananciaMusculo => 0,
        _ => TopeCorte
    };
}

/// <summary>
/// Con composición: lo que hace falta para el % en el plazo.
/// Cada tipo tiene un tope (corte Helms/ACSM, recomp ~la mitad). Si el tope no cubre,
/// el diario operativo es el tope y el aviso de alcance lo dice.
/// Sin composición: corte 20 % del GET, recomp 10 %, ganancia ~10 % (ISSN).
/// </summary>
public static class DeficitObjetivo
{
    public static ResultadoDeficit De(
        ResultadoPlan? plan,
        double? gastoDelDia,
        double? grasaPct,
        TipoObjetivo tipo,
        double? manualKcal)
    {
        var techo = plan?.TechoDiario ?? double.PositiveInfinity;
        var gasto = gastoDelDia ?? 0;
        var corte = Math.Max(0, plan?.DeficitDiario ?? 0);
        var topeCorte = double.IsPositiveInfinity(techo) ? 0 : techo;
        var pctCorte = PctCorteParaRecomp(plan);
        var topeRecomp = TopeRecompDe(gasto, techo, corte, grasaPct, pctCorte);
        double recomp;
        double ganancia;
        if (plan is { UsaComposicion: false })
        {
            var recompCrudo = gasto > 0
                ? gasto * ConstantesNutricion.PctRecomp(grasaPct, pctCorte)
                : corte * 0.5;
            if (!double.IsPositiveInfinity(techo)) recompCrudo = Math.Min(recompCrudo, techo);
            recomp = ADiez(Math.Max(0, recompCrudo));
            if (!double.IsPositiveInfinity(techo) && recomp > techo + 0.5)
                recomp = Math.Floor(techo / 10.0) * 10;
            topeRecomp = recomp;
            ganancia = Superavit(gasto, grasaPct);
        }
        else
        {
            recomp = AplicarTope(corte, topeRecomp, techo);
            ganancia = Superavit(gasto, grasaPct, corte, techo);
        }
        var esSuperavit = tipo == TipoObjetivo.GananciaMusculo;
        var calculado = tipo switch
        {
            TipoObjetivo.Recomposicion => recomp,
            TipoObjetivo.GananciaMusculo => ganancia,
            _ => corte
        };
        var esManual = manualKcal is > 0;
        var diario = esManual ? manualKcal!.Value : calculado;
        double? semanas = !esSuperavit
            && plan is { UsaComposicion: true, KcalTotales: > 0 }
            && diario > 0
            ? plan.KcalTotales / (diario * 7)
            : null;
        return new ResultadoDeficit(diario, esSuperavit, corte, recomp, ganancia, tipo, esManual, semanas,
            topeCorte, topeRecomp);
    }

    public static double TopeRecompDe(
        double gasto, double techo, double corte, double? grasaPct = null, double? pctCorteEfectivo = null)
    {
        var cap = gasto > 0 ? gasto * ConstantesNutricion.PctRecomp(grasaPct, pctCorteEfectivo) : corte;
        if (!double.IsPositiveInfinity(techo) && techo > 0) cap = Math.Min(cap, techo);
        return ADiez(Math.Max(0, cap));
    }

    /// <summary>Déficit operativo de recomp: lo que hace falta, sin pasar el techo (Helms/ISSN).</summary>
    public static double Recomposicion(double? grasaPct, double corte, double techo, double gasto = 0, double? pctCorteEfectivo = null)
        => AplicarTope(corte, TopeRecompDe(gasto, techo, corte, grasaPct, pctCorteEfectivo), techo);

    public static double Superavit(double gastoDelDia, double? grasaPct, double corte = 0, double techo = double.PositiveInfinity)
    {
        if (gastoDelDia <= 0) return 0;
        var baseKcal = gastoDelDia * ConstantesNutricion.PctSuperavit(grasaPct);
        if (corte > 0 && !double.IsPositiveInfinity(techo) && techo > 0)
        {
            var agresion = Math.Clamp(corte / techo, 0, 1);
            baseKcal *= Math.Clamp(1 - 0.6 * agresion, 0.20, 1);
        }
        return ADiez(Math.Max(0, baseKcal));
    }

    private static double? PctCorteParaRecomp(ResultadoPlan? plan)
    {
        if (plan is not { PctGastoMaxAplicado: > 0 } p) return null;
        if (p.UsaComposicion) return p.PctGastoMaxAplicado;
        if (Math.Abs(p.PctGastoMaxAplicado - ValoresDeFabrica.PctGastoMax) > 0.00015)
            return p.PctGastoMaxAplicado;
        return null;
    }

    private static double AplicarTope(double corte, double tope, double techo)
    {
        if (corte <= 0) return 0;
        var crudo = tope > 0 ? Math.Min(corte, tope) : corte;
        var v = ADiez(Math.Max(0, crudo));
        if (corte <= tope + 0.5 && v + 0.5 < corte)
            v += 10;
        if (!double.IsPositiveInfinity(techo) && v > techo + 0.5)
            v = Math.Floor(techo / 10.0) * 10;
        return v;
    }

    private static double ADiez(double kcal)
        => Math.Round(kcal / 10, MidpointRounding.AwayFromZero) * 10;
}
