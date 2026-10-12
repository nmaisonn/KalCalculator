namespace KcalTracker.Calculo;

/// <summary>
/// El plazo corre desde la fecha de inicio (semana X de Y).
/// El recorte diario se arma el día 0 con % y semanas; no se acelera al pasar el tiempo.
/// </summary>
public sealed record PlazoCorrido(
    int PlazoSemanas,
    DateOnly? Inicio,
    DateOnly Hoy,
    double SemanasTranscurridas,
    double SemanasRestantes,
    int DiasRestantes)
{
    public static PlazoCorrido De(int plazoSemanas, DateOnly? inicio, DateOnly? hoy = null)
    {
        var dia = hoy ?? DateOnly.FromDateTime(DateTime.Today);
        var plazo = Math.Max(1, plazoSemanas);
        if (inicio is null || inicio > dia)
            return new PlazoCorrido(plazo, inicio, dia, 0, plazo, plazo * 7);

        var transcurridos = Math.Max(0, dia.DayNumber - inicio.Value.DayNumber);
        var total = plazo * 7;
        var restantes = Math.Max(1, total - transcurridos);
        return new PlazoCorrido(plazo, inicio, dia, transcurridos / 7.0, restantes / 7.0, restantes);
    }
}

public sealed record RitmoObjetivo(
    PlazoCorrido Plazo,
    double? SemanasAEsteRitmo,
    int? PlazoSugerido,
    double? DeficitSiAlarga,
    bool VaMasLento,
    string Texto)
{
    public static RitmoObjetivo De(
        PlazoCorrido plazo,
        ResultadoPlan? plan,
        ResultadoCalibracion? cal,
        ResultadoDeficit? def = null)
    {
        string baseTxt;
        if (plazo.Inicio is null)
        {
            baseTxt = $"Plazo de {plazo.PlazoSemanas} semanas. Sin fecha de inicio se cuentan completas.";
        }
        else
        {
            baseTxt = $"Van {plazo.SemanasTranscurridas.ToString("0.#")} de {plazo.PlazoSemanas} semanas. " +
                      $"Quedan {plazo.SemanasRestantes.ToString("0.#")}.";
        }

        if (plan is null || (plan.UsaComposicion && plan.GrasaAPerderKg <= 0.05))
            return new RitmoObjetivo(plazo, null, null, null, false, baseTxt);

        var diario = def?.Diario ?? plan.DeficitDiario;
        var esSuperavit = def?.EsSuperavit == true;
        var techo = plan.TechoDiario;
        var excedeTecho = !esSuperavit && techo > 0 && diario > techo + 0.5;

        if (cal is { Dias: >= 7, GrasaPerdidaKg: > 0.05 })
        {
            var kgSem = cal.GrasaPerdidaKg / (cal.Dias / 7.0);
            if (kgSem < 0.02)
            {
                return new RitmoObjetivo(plazo, null, plazo.PlazoSemanas + 4, null, true,
                    baseTxt + " La báscula casi no movió grasa. Conviene alargar el plazo o revisar si el recorte se está cumpliendo.");
            }

            var semMas = plan.GrasaAPerderKg / kgSem;
            var total = plazo.SemanasTranscurridas + semMas;
            var lento = total > plazo.PlazoSemanas + 0.5;
            int? sugerido = lento
                ? (int)Math.Clamp(Math.Ceiling(total), plazo.PlazoSemanas + 1, 52)
                : null;
            double? deficitAlarga = null;
            if (sugerido is { } n)
            {
                var semRest = Math.Max(1, n - plazo.SemanasTranscurridas);
                deficitAlarga = plan.KcalTotales / (semRest * 7);
            }

            var ritmo = $" A este ritmo (~{kgSem.ToString("0.00")} kg de grasa/sem) el % llega en {total.ToString("0.#")} semanas.";
            if (lento && sugerido is { } s && deficitAlarga is { } d)
            {
                ritmo += $" Podés alargar a {s} semanas (recorte ~{Redondeo.Kcal(d)} kcal/día) o mantener la fecha y el recorte actual.";
                return new RitmoObjetivo(plazo, total, sugerido, deficitAlarga, lento, baseTxt + ritmo);
            }

            return Cierre(plazo, plan, def, diario, esSuperavit, excedeTecho, baseTxt + ritmo, total);
        }

        return Cierre(plazo, plan, def, diario, esSuperavit, excedeTecho, baseTxt, def?.SemanasAEsteRitmo);
    }

    private static RitmoObjetivo Cierre(
        PlazoCorrido plazo,
        ResultadoPlan plan,
        ResultadoDeficit? def,
        double diario,
        bool esSuperavit,
        bool excedeTecho,
        string baseTxt,
        double? semanasRitmo)
    {
        if (esSuperavit)
        {
            var extraSuper = plan.UsaComposicion
                ? " El % de grasa no baja mientras comas por encima del gasto."
                : " Comés por encima del gasto cotidiano.";
            return new RitmoObjetivo(plazo, semanasRitmo, null, null, false,
                baseTxt + $" Superávit de {Redondeo.Kcal(diario)} kcal/día.{extraSuper}");
        }

        if (!excedeTecho)
        {
            var extra = $" El recorte elegido ({Redondeo.Kcal(diario)} kcal/día) está por debajo del techo de {Redondeo.Kcal(plan.TechoDiario)} kcal.";
            if (plan.UsaComposicion && def?.SemanasAEsteRitmo is { } sem && sem > plazo.PlazoSemanas + 0.5)
                extra += $" A este ritmo el % llega en ~{sem.ToString("0.#")} semanas.";
            return new RitmoObjetivo(plazo, semanasRitmo ?? def?.SemanasAEsteRitmo, null, null, false, baseTxt + extra);
        }

        var semRest = Math.Max(1, plan.KcalTotales / (plan.TechoDiario * 7));
        var total = plazo.SemanasTranscurridas + semRest;
        var sugerido = (int)Math.Clamp(Math.Ceiling(total), plazo.PlazoSemanas + 1, 52);
        var restSugeridas = Math.Max(1, sugerido - plazo.SemanasTranscurridas);
        var deficitAlarga = plan.KcalTotales / (restSugeridas * 7);
        return new RitmoObjetivo(plazo, semanasRitmo ?? total, sugerido, deficitAlarga, true,
            baseTxt + $" El recorte elegido ({Redondeo.Kcal(diario)} kcal/día) supera el techo de {Redondeo.Kcal(plan.TechoDiario)} kcal. Podés alargar a {sugerido} semanas (recorte ~{Redondeo.Kcal(deficitAlarga)} kcal/día) o mantener la fecha.");
    }
}
