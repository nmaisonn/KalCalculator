using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

public sealed record ResultadoAlcance(
    bool Llega,
    string Clase,
    string Titulo,
    string Texto,
    int? PlazoSugerido,
    TipoObjetivo? TipoSugerido,
    int? PctSugerido,
    string? Cuenta = null);

/// <summary>
/// Viabilidad: % objetivo + semanas → kcal necesarias; cada tipo tiene un tope.
/// Si el tope no cubre, se sugiere otro tipo viable, más semanas o un % más cerca.
/// </summary>
public static class AlcanceObjetivo
{
    public static ResultadoAlcance? De(
        ResultadoPlan? plan,
        ResultadoDeficit? def,
        double? grasaActualPct,
        double grasaObjetivoPct,
        TipoObjetivo tipo)
    {
        if (plan is null) return null;

        if (!plan.UsaComposicion)
        {
            if (grasaActualPct is not > 0)
            {
                return new ResultadoAlcance(true, "", "Sin % de grasa actual",
                    "El recorte sale del gasto. Cuando cargues el % se puede chequear si ese objetivo de grasa entra en estas semanas.",
                    null, null, null);
            }

            return null;
        }

        var nombre = TipoObjetivoNombres.De(tipo);
        var obj = grasaObjetivoPct.ToString("0.#");
        var plazo = plan.Plazo?.PlazoSemanas ?? 0;
        var dias = Math.Max(1, plazo * 7);
        var trans = plan.Plazo?.SemanasTranscurridas ?? 0;
        var necesario = def?.Necesario ?? plan.DeficitDiario;
        var techo = plan.TechoDiario;
        var topeCorte = def is { TopeCorte: > 0 } ? def.TopeCorte : techo;
        var pctCorte = plan.PctGastoMaxAplicado > 0
            ? plan.PctGastoMaxAplicado
            : ConstantesNutricion.Calcular(grasaActualPct).PctGastoMax;
        var topeRecomp = def is { TopeRecomp: > 0 }
            ? def.TopeRecomp
            : techo * ConstantesNutricion.PctRecomp(grasaActualPct, pctCorte) / pctCorte;
        var corteEntra = necesario <= topeCorte + 0.5;
        var recompEntra = necesario <= topeRecomp + 0.5;
        var kcalKg = plan.KcalPorKgGrasaAplicadas;

        if (tipo == TipoObjetivo.GananciaMusculo)
        {
            if (plan.GrasaAPerderKg > 0.05)
            {
                TipoObjetivo? sug = corteEntra ? TipoObjetivo.Corte : recompEntra ? TipoObjetivo.Recomposicion : null;
                var extra = sug is { } t
                    ? $" Optá por {TipoObjetivoNombres.De(t).ToLower()}, o cambiá el % o las semanas."
                    : " Cambiá el % o las semanas; con ganancia el % no baja.";
                return new ResultadoAlcance(false, "bad", "Con ganancia el % no baja",
                    $"Según {plazo} semanas y {obj} %, con ganancia de músculo no es posible llegar: el superávit no baja el %.{extra}",
                    null, sug, null,
                    CuentaGrasa(plan, grasaObjetivoPct) +
                    " El superávit suma energía: la grasa no baja, así que el % objetivo no se usa en esta cuenta.");
            }

            return Ok("Es alcanzable", "La ganancia no apunta a bajar el %. El superávit está dentro de lo habitual.");
        }

        if (plan.GrasaAPerderKg <= 0.05)
        {
            return Ok("Objetivo alcanzado",
                $"Ya estás en o por debajo del {obj} % de grasa. Bajá el % objetivo si querés seguir recortando.");
        }

        var topeTipo = tipo == TipoObjetivo.Recomposicion ? topeRecomp : topeCorte;
        var kgTope = KgEn(topeTipo, dias, kcalKg);
        var pctTope = PctTras(plan, kgTope);
        var semTope = SemanasTotales(plan.KcalTotales, topeTipo);
        var entra = necesario <= topeTipo + 0.5;

        if (!entra)
        {
            var s = Sugerido(semTope, plazo);
            TipoObjetivo? tipoSug = null;
            var extra = "";
            if (tipo == TipoObjetivo.Recomposicion && corteEntra)
            {
                tipoSug = TipoObjetivo.Corte;
                extra = $" El corte sí entra ({Formato.Kcal(necesario)} kcal/día, techo {Formato.Kcal(topeCorte)}).";
            }
            else if (tipo == TipoObjetivo.Corte && recompEntra)
            {
                tipoSug = TipoObjetivo.Recomposicion;
                extra = $" La recomposición sí entra ({Formato.Kcal(necesario)} kcal/día, techo {Formato.Kcal(topeRecomp)}).";
            }
            else if (!corteEntra && !recompEntra)
            {
                extra = " Ni el corte llega: hace falta alargar las semanas o un % más cerca del actual.";
            }

            var pctTxt = pctTope is { } p
                ? $" Con ese techo, en el plazo llegás como mucho a ~{p.ToString("0.#")} %."
                : "";

            return new ResultadoAlcance(false, tipo == TipoObjetivo.Corte ? "bad" : "warn",
                "Con este objetivo no es posible llegar",
                $"Según {plazo} semanas y {obj} %, con {nombre.ToLower()} no es posible llegar a ese %. " +
                $"Hace falta {Formato.Kcal(necesario)} kcal/día; el techo de {nombre.ToLower()} es {Formato.Kcal(topeTipo)} kcal/día.{pctTxt}{extra} " +
                "Optá por otro objetivo, cambiá el % o las semanas.",
                s, tipoSug, PctEntero(pctTope, grasaObjetivoPct),
                Cuenta(plan, grasaObjetivoPct, necesario, topeTipo, dias, trans, techo, corteEntra, nombre));
        }

        return Ok("Es alcanzable",
            $"Según {plazo} semanas y {obj} %, con {nombre.ToLower()} sí es posible: hacen falta {Formato.Kcal(necesario)} kcal/día y el techo es {Formato.Kcal(topeTipo)}.",
            Cuenta(plan, grasaObjetivoPct, necesario, topeTipo, dias, trans, techo, corteEntra, nombre));
    }

    private static ResultadoAlcance Ok(string titulo, string texto, string? cuenta = null)
        => new(true, "ok", titulo, texto, null, null, null, cuenta);

    private static string CuentaGrasa(ResultadoPlan plan, double grasaObjetivoPct)
    {
        var peso = plan.MasaMagraKg + plan.GrasaActualKg;
        var actual = peso <= 0 ? 0 : 100 * plan.GrasaActualKg / peso;
        return $"Estás en {actual.ToString("0.#")} % ({Formato.Kg(peso, 1)} kg). " +
               $"Para {grasaObjetivoPct.ToString("0.#")} % hay que perder {Formato.Kg(plan.GrasaAPerderKg)} kg de grasa, con masa magra fija ({Formato.Kg(plan.MasaMagraKg)} kg). " +
               $"A {Formato.Kcal(plan.KcalPorKgGrasaAplicadas)} kcal/kg eso pide {Formato.Kcal(plan.KcalTotales)} kcal de déficit.";
    }

    private static string Cuenta(
        ResultadoPlan plan,
        double grasaObjetivoPct,
        double necesario,
        double topeTipo,
        int dias,
        double trans,
        double techo,
        bool corteEntra,
        string nombre)
    {
        var semanas = plan.Plazo?.PlazoSemanas ?? Math.Max(1, (int)Math.Round(dias / 7.0));
        var plazoTxt = trans > 0.05
            ? $"Van {trans.ToString("0.#")} de {semanas} semanas. El recorte diario se fijó el día 0 ({dias} días)."
            : $"El plazo son {dias} días.";
        var topeTxt = $" Hace falta {Formato.Kcal(necesario)} kcal/día. El techo de {nombre.ToLower()} es {Formato.Kcal(topeTipo)} kcal/día.";
        var techoTxt = corteEntra
            ? $" El techo de corte es {Formato.Kcal(techo)} kcal/día."
            : $" El necesario del plazo ({Formato.Kcal(plan.DeficitDiario)} kcal/día) pasa el techo de {Formato.Kcal(techo)} kcal/día.";
        return $"{CuentaGrasa(plan, grasaObjetivoPct)} {plazoTxt}{topeTxt}{techoTxt}";
    }

    private static double KgEn(double diario, int dias, double kcalPorKg)
        => diario <= 0 || kcalPorKg <= 0 || dias <= 0 ? 0 : diario * dias / kcalPorKg;

    private static double? PctTras(ResultadoPlan plan, double kgPerdidos)
    {
        if (plan.MasaMagraKg <= 0) return null;
        var kg = Math.Clamp(kgPerdidos, 0, Math.Max(0, plan.GrasaActualKg));
        var grasa = plan.GrasaActualKg - kg;
        var peso = plan.MasaMagraKg + grasa;
        return peso <= 0 ? null : 100 * grasa / peso;
    }

    private static double SemanasTotales(double kcalTotales, double diario)
        => diario <= 0 ? double.PositiveInfinity : kcalTotales / (diario * 7);

    private static int? Sugerido(double semanasTotales, int plazo)
    {
        if (double.IsInfinity(semanasTotales) || double.IsNaN(semanasTotales)) return null;
        var n = (int)Math.Clamp(Math.Ceiling(semanasTotales), plazo + 1, 52);
        return n;
    }

    private static int? PctEntero(double? pctAlcanzable, double objetivo)
    {
        if (pctAlcanzable is not { } p) return null;
        var sug = (int)Math.Clamp(Math.Round(p, MidpointRounding.AwayFromZero), 8, 40);
        return sug > objetivo + 0.4 ? sug : null;
    }
}
