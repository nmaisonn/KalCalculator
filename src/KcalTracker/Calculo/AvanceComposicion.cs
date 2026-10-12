using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

public sealed record ResumenCambio(
    double Desde,
    double Hasta,
    double Delta,
    int Puntos,
    DateOnly FechaDesde,
    DateOnly FechaHasta);

public sealed record VistaGraficoSerie(
    string Titulo,
    string Aria,
    string Clase,
    string TipoEje,
    string Lectura,
    string EtiquetaMax,
    string EtiquetaMin,
    GraficoLinea? Grafico,
    string? LeyendaObjetivo);

public sealed record VistaAvance(
    DateOnly? Desde,
    DateOnly Hasta,
    int Pesadas,
    DateOnly? Primera,
    DateOnly? Ultima,
    ResumenCambio? Peso,
    ResumenCambio? GrasaPct,
    ResumenCambio? MusculoPct,
    string Lectura,
    IReadOnlyList<VistaGraficoSerie> Graficos);

/// <summary>
/// Series de composición contra la báscula. Si hay medición de referencia,
/// el primer punto es esa pesada (no la más vieja del historial).
/// </summary>
public static class AvanceComposicion
{
    public static IReadOnlyList<Medicion> ComparablesEnVentana(
        IEnumerable<Medicion> todas,
        DateOnly? desde,
        DateOnly hasta,
        Medicion? referencia = null)
    {
        var anclaFecha = referencia is { EsReferencia: true } ? referencia.Fecha : (DateOnly?)null;
        var corte = desde ?? anclaFecha;
        return todas
            .Where(m => m.Fecha <= hasta && (corte is null || m.Fecha >= corte.Value))
            .Where(m => m.EsComparable || EsAncla(m, referencia))
            .OrderBy(m => m.Fecha)
            .ToList();
    }

    public static VistaAvance De(
        IEnumerable<Medicion> todas,
        DateOnly? desde,
        DateOnly hasta,
        double? grasaObjetivoPct = null,
        Medicion? referencia = null)
    {
        var ancla = referencia is { EsReferencia: true } ? referencia : null;
        var corte = desde ?? ancla?.Fecha;
        var pts = ComparablesEnVentana(todas, corte, hasta, ancla);
        var peso = Resumir(pts.Select(m => (m.Fecha, (double?)m.PesoKg)));
        var grasaPct = Resumir(pts.Select(m => (m.Fecha, m.GrasaPct)));
        var musculoPct = Resumir(pts.Select(m => (m.Fecha, m.MusculoPct)));
        var graficos = ArmarGraficos(pts, peso, grasaPct, musculoPct, grasaObjetivoPct);

        return new VistaAvance(
            corte, hasta, pts.Count,
            pts.Count == 0 ? null : pts[0].Fecha,
            pts.Count == 0 ? null : pts[^1].Fecha,
            peso, grasaPct, musculoPct,
            LecturaDe(corte, pts.Count, peso, grasaPct, ancla is not null),
            graficos);
    }

    private static bool EsAncla(Medicion m, Medicion? referencia)
    {
        if (referencia is not { EsReferencia: true }) return false;
        if (referencia.Id != 0 && m.Id != 0) return m.Id == referencia.Id;
        return m.Fecha == referencia.Fecha;
    }

    private static List<(DateOnly Fecha, double Valor)> KgDerivado(
        IReadOnlyList<Medicion> pts,
        Func<Medicion, double?> derivar)
        => pts
            .Select(m => (m.Fecha, Kg: derivar(m)))
            .Where(p => p.Kg is not null)
            .Select(p => (p.Fecha, p.Kg!.Value))
            .ToList();

    public static ResumenCambio? Resumir(IEnumerable<(DateOnly Fecha, double? Valor)> serie)
    {
        var pts = serie
            .Where(p => p.Valor is not null)
            .Select(p => (p.Fecha, Valor: p.Valor!.Value))
            .ToList();
        if (pts.Count == 0) return null;
        var desde = pts[0].Valor;
        var hasta = pts[^1].Valor;
        return new ResumenCambio(desde, hasta, hasta - desde, pts.Count, pts[0].Fecha, pts[^1].Fecha);
    }

    private static IReadOnlyList<VistaGraficoSerie> ArmarGraficos(
        IReadOnlyList<Medicion> pts,
        ResumenCambio? peso,
        ResumenCambio? grasaPct,
        ResumenCambio? musculoPct,
        double? grasaObjetivoPct)
    {
        if (pts.Count == 0) return [];

        var lista = new List<VistaGraficoSerie>
        {
            Caja(
                "Peso",
                "peso",
                "kg",
                "Peso en kilogramos",
                peso,
                GraficoSerie.De(pts.Select(m => (m.Fecha, m.PesoKg)).ToList()),
                v => Formato.Kg(v, 1) + " kg",
                "kg")
        };

        var ptsGrasa = pts.Where(m => m.GrasaPct is not null).Select(m => (m.Fecha, m.GrasaPct!.Value)).ToList();
        if (ptsGrasa.Count >= 2)
        {
            lista.Add(Caja(
                "Grasa",
                "grasa",
                "pct",
                "Porcentaje de grasa",
                grasaPct,
                GraficoSerie.De(ptsGrasa, grasaObjetivoPct),
                v => v.ToString("0.#") + " %",
                "pp",
                grasaObjetivoPct is { } g
                    ? $"Línea punteada: objetivo {g.ToString("0.#")} %"
                    : null));
        }

        var ptsGrasaKg = KgDerivado(pts, m => Composicion.GrasaKg(m.PesoKg, m.GrasaPct));
        if (ptsGrasaKg.Count >= 2)
        {
            lista.Add(Caja(
                "Grasa (kg)",
                "grasa-kg",
                "kg2",
                "Kilogramos de grasa, peso por porcentaje",
                Resumir(pts.Select(m => (m.Fecha, Composicion.GrasaKg(m.PesoKg, m.GrasaPct)))),
                GraficoSerie.De(ptsGrasaKg),
                v => Formato.Kg(v) + " kg",
                "kg",
                "Se deriva del peso × %; no se tipea."));
        }

        var ptsMusc = pts.Where(m => m.MusculoPct is not null).Select(m => (m.Fecha, m.MusculoPct!.Value)).ToList();
        if (ptsMusc.Count >= 2)
        {
            lista.Add(Caja(
                "Músculo",
                "musculo",
                "pct",
                "Porcentaje de músculo",
                musculoPct,
                GraficoSerie.De(ptsMusc),
                v => v.ToString("0.#") + " %",
                "pp"));
        }

        var ptsMuscKg = KgDerivado(pts, m => Composicion.MusculoKg(m.PesoKg, m.MusculoPct));
        if (ptsMuscKg.Count >= 2)
        {
            lista.Add(Caja(
                "Músculo (kg)",
                "musculo-kg",
                "kg2",
                "Kilogramos de músculo, peso por porcentaje",
                Resumir(pts.Select(m => (m.Fecha, Composicion.MusculoKg(m.PesoKg, m.MusculoPct)))),
                GraficoSerie.De(ptsMuscKg),
                v => Formato.Kg(v) + " kg",
                "kg",
                "Se deriva del peso × %; no se tipea."));
        }

        var ptsVisc = pts.Where(m => m.Visceral is not null).Select(m => (m.Fecha, m.Visceral!.Value)).ToList();
        if (ptsVisc.Count >= 2)
        {
            lista.Add(Caja(
                "Grasa visceral",
                "visceral",
                "num",
                "Nivel de grasa visceral",
                Resumir(pts.Select(m => (m.Fecha, m.Visceral))),
                GraficoSerie.De(ptsVisc),
                v => v.ToString("0.#"),
                ""));
        }

        return lista;
    }

    private static VistaGraficoSerie Caja(
        string titulo,
        string clase,
        string tipoEje,
        string aria,
        ResumenCambio? r,
        GraficoLinea? grafico,
        Func<double, string> fmt,
        string unidadDelta,
        string? leyendaObjetivo = null)
    {
        string eje(double v) => tipoEje switch
        {
            "kg" => Formato.Kg(v, 1),
            "kg2" => Formato.Kg(v),
            "pct" => v.ToString("0.#") + " %",
            _ => v.ToString("0.#")
        };
        return new VistaGraficoSerie(
            titulo, aria, clase, tipoEje,
            LecturaSerie(titulo, r, fmt, unidadDelta, tipoEje),
            grafico is { } g ? eje(g.YMaxValor) : "",
            grafico is { } g2 ? eje(g2.YMinValor) : "",
            grafico, leyendaObjetivo);
    }

    private static string LecturaDe(
        DateOnly? desde,
        int pesadas,
        ResumenCambio? peso,
        ResumenCambio? grasaPct,
        bool desdeReferencia)
    {
        var arranque = desde is { } d
            ? (desdeReferencia ? $"Desde la referencia del {Formato.FechaCorta(d)}" : $"Desde el {Formato.FechaCorta(d)}")
            : "En todo el historial";
        if (pesadas == 0)
            return desde is { } d0
                ? $"No hay pesadas marcadas contra la báscula desde el {Formato.FechaCorta(d0)}. Cargá una en Lista y dejala marcada."
                : "No hay pesadas marcadas contra la báscula. Cargá una en Lista y dejala marcada.";
        if (pesadas == 1)
        {
            var kg = peso is { } p ? $" ({Formato.Kg(p.Hasta, 1)} kg el {Formato.FechaCorta(p.FechaHasta)})" : "";
            return $"{arranque} hay una sola pesada comparable{kg}. Con otra de las mismas condiciones se dibuja la pendiente.";
        }

        var texto = $"{arranque} hay {pesadas} pesadas contra la báscula.";
        if (peso is { } pe)
            texto += $" El peso pasó de {Formato.Kg(pe.Desde, 1)} a {Formato.Kg(pe.Hasta, 1)} kg ({ConSigno(pe.Delta, v => Formato.Kg(v, 1))}).";
        if (grasaPct is { } g)
            texto += $" La grasa, de {g.Desde.ToString("0.#")} % a {g.Hasta.ToString("0.#")} % ({ConSigno(g.Delta, v => v.ToString("0.#"))} pp).";
        return texto;
    }

    private static string LecturaSerie(string titulo, ResumenCambio? r, Func<double, string> fmt, string unidadDelta, string tipoEje)
    {
        if (r is null)
            return $"Todavía no hay {titulo.ToLowerInvariant()} para graficar.";
        if (r.Puntos == 1)
            return $"Una lectura: {fmt(r.Hasta)} el {Formato.FechaCorta(r.FechaHasta)}. Con otra se dibuja la pendiente.";
        if (Math.Abs(r.Delta) <= 0.05)
            return $"Mantuviste {fmt(r.Hasta)} en {r.Puntos} pesadas.";
        string fmtDelta(double v) => unidadDelta == "kg"
            ? Formato.Kg(v, tipoEje == "kg2" ? 2 : 1)
            : v.ToString("0.#");
        var unidad = string.IsNullOrEmpty(unidadDelta) ? "" : " " + unidadDelta;
        return $"De {fmt(r.Desde)} a {fmt(r.Hasta)} · {ConSigno(r.Delta, fmtDelta)}{unidad} en {r.Puntos} pesadas.";
    }

    private static string ConSigno(double n, Func<double, string> fmt)
    {
        var abs = fmt(Math.Abs(n));
        if (n > 0.005) return "+" + abs;
        if (n < -0.005) return "−" + abs;
        return abs;
    }
}
