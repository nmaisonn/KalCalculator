namespace KcalTracker.Calculo;

/// <summary>
/// Ritmo real en los días cargados vs el recorte diario fijado el día 0.
/// El calendario marca la semana; los huecos no suman ni restan kcal.
/// </summary>
public sealed record LecturaSeguimiento(
    string Titulo,
    string Texto,
    string Clase);

public static class SeguimientoObjetivo
{
    public static LecturaSeguimiento? De(
        PlazoCorrido? plazo,
        IReadOnlyList<DiaDePeriodo>? serie,
        double necesarioDiario,
        bool superavit)
    {
        if (plazo is null || necesarioDiario <= 0) return null;
        if (plazo.Inicio is null)
        {
            return new LecturaSeguimiento("Seguimiento",
                "Sin fecha de arranque no se puede comparar lo hecho con lo esperado. Cargala arriba.",
                "");
        }

        if (plazo.SemanasTranscurridas < 0.15)
        {
            return new LecturaSeguimiento("Seguimiento",
                "Todavía no pasó una semana del arranque. Cuando haya días cargados se compara el déficit hecho con el que hacía falta.",
                "");
        }

        var dias = serie?.ToList() ?? [];
        var trans = plazo.SemanasTranscurridas;
        var plazoSem = plazo.PlazoSemanas;
        var diasCorridos = Math.Max(1, (int)Math.Round(trans * 7));
        var diario = Redondeo.Kcal(necesarioDiario);

        var conR = dias.Where(d => d.HayRegistro).ToList();
        if (conR.Count == 0)
        {
            return new LecturaSeguimiento("Seguimiento",
                $"Van {trans.ToString("0.#")} de {plazoSem} semanas, pero no hay comida cargada. Sin eso no se puede decir si el ritmo alcanza.",
                "warn");
        }

        var hecho = conR.Sum(d => superavit ? -(d.Deficit ?? 0) : (d.Deficit ?? 0));
        var esperado = diario * conR.Count;
        var verbo = superavit ? "superávit" : "déficit";
        var delta = hecho - esperado;
        var cobertura = $"{conR.Count} de {diasCorridos} días";
        var incompleto = conR.Count < diasCorridos;
        var cuenta = incompleto
            ? $"En los {conR.Count} días que cargaste llevás {Formato.Kcal(hecho)} kcal de {verbo}; a {Formato.Kcal(diario)} kcal/día hacían falta {Formato.Kcal(esperado)}."
            : $"Llevás {Formato.Kcal(hecho)} kcal de {verbo}; a {Formato.Kcal(diario)} kcal/día, a esta altura hacían falta {Formato.Kcal(esperado)}.";
        var huecos = incompleto
            ? " Los días sin carga no se cuentan ni a favor ni en contra."
            : "";

        if (delta >= -20)
        {
            var extra = delta > 20
                ? $" Vas {Formato.Kcal(delta)} kcal por encima del ritmo."
                : " Vas en línea con el ritmo.";
            return new LecturaSeguimiento("Seguimiento",
                $"Van {trans.ToString("0.#")} de {plazoSem} semanas. {cuenta}{extra}{huecos} Cargaste {cobertura}.",
                "ok");
        }

        return new LecturaSeguimiento("Seguimiento",
            $"Van {trans.ToString("0.#")} de {plazoSem} semanas. {cuenta} Vas {Formato.Kcal(-delta)} kcal abajo del ritmo.{huecos} Cargaste {cobertura}.",
            "warn");
    }
}
