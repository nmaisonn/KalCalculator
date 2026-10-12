using ClosedXML.Excel;
using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

/// <summary>
/// Extracto del período para analizar (comida, entreno desglosado, mediciones).
/// Pensado para pasárselo a una IA: fechas ISO, hojas chatas, hoja Para_IA con las reglas.
/// </summary>
public class ServicioExportacionPeriodo(KcalDbContext db, ServicioPeriodo periodo, ServicioDia dias)
{
    public async Task<byte[]> ArmarAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        var metricas = await periodo.CalcularAsync(desde, hasta, ct);
        var (medicion, bmr, get, perfil, _) = await dias.BaseAsync(ct);

        var comidas = await db.Comidas.AsNoTracking()
            .Include(c => c.TipoComida)
            .Include(c => c.Items)
            .Where(c => c.Fecha >= desde && c.Fecha <= hasta)
            .ToListAsync(ct);
        comidas = comidas
            .OrderBy(c => c.Fecha)
            .ThenBy(c => c.TipoComida?.Orden ?? 99)
            .ThenBy(c => c.Id)
            .ToList();

        var gym = await db.SesionesGym.AsNoTracking()
            .Include(s => s.Ejercicios)
            .Where(s => s.Fecha >= desde && s.Fecha <= hasta)
            .OrderBy(s => s.Fecha).ThenBy(s => s.Id)
            .ToListAsync(ct);

        var cardio = await db.SesionesCardio.AsNoTracking()
            .Include(s => s.Protocolo)
            .Where(s => s.Fecha >= desde && s.Fecha <= hasta)
            .OrderBy(s => s.Fecha).ThenBy(s => s.Id)
            .ToListAsync(ct);

        var deporte = await db.ActividadLogs.AsNoTracking()
            .Where(s => s.Fecha >= desde && s.Fecha <= hasta)
            .OrderBy(s => s.Fecha).ThenBy(s => s.Id)
            .ToListAsync(ct);

        var mediciones = await db.Mediciones.AsNoTracking()
            .Where(m => m.Fecha >= desde && m.Fecha <= hasta)
            .OrderBy(m => m.Fecha)
            .ToListAsync(ct);

        return Escribir(new DatosExport(desde, hasta, metricas, perfil, medicion, bmr, get,
            comidas, gym, cardio, deporte, mediciones));
    }

    private sealed record DatosExport(
        DateOnly Desde,
        DateOnly Hasta,
        MetricasPeriodo Metricas,
        Perfil Perfil,
        Medicion? Referencia,
        ResultadoBmr? Bmr,
        double? Get,
        IReadOnlyList<Comida> Comidas,
        IReadOnlyList<SesionGym> Gym,
        IReadOnlyList<SesionCardio> Cardio,
        IReadOnlyList<ActividadLog> Deporte,
        IReadOnlyList<Medicion> Mediciones);

    private static byte[] Escribir(DatosExport d)
    {
        using var wb = new XLWorkbook();
        ParaIa(wb.AddWorksheet("Para_IA"));
        Contexto(wb.AddWorksheet("Contexto"), d);
        Resumen(wb.AddWorksheet("Resumen"), d);
        Dias(wb.AddWorksheet("Días"), d.Metricas.Serie);
        Alimentos(wb.AddWorksheet("Alimentos"), d.Comidas);
        HojaComidas(wb.AddWorksheet("Comidas"), d.Comidas);
        Gym(wb.AddWorksheet("Gym"), d.Gym);
        EjerciciosGym(wb.AddWorksheet("Ejercicios gym"), d.Gym);
        Cardio(wb.AddWorksheet("Cardio"), d.Cardio);
        Deporte(wb.AddWorksheet("Deporte"), d.Deporte);
        Mediciones(wb.AddWorksheet("Mediciones"), d.Mediciones);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void ParaIa(IXLWorksheet ws)
    {
        string[] lineas =
        [
            "EXTRACTO DE SEGUIMIENTO (kcal-tracker) PARA ANÁLISIS",
            "",
            "Usá este archivo como fuente. Las hojas son tablas chatas: fila 1 = nombres de columna, una fila = un hecho. fecha_iso es YYYY-MM-DD (texto). No hay celdas combinadas.",
            "",
            "GASTO DEL DÍA",
            "gasto_kcal = get_kcal + gym_kcal + cardio_kcal + deporte_kcal.",
            "GET = gasto de un día SIN entrenar (metabolismo basal × factor de actividad sedentario). Gym, cardio y deporte se suman BRUTOS, no van metidos en el GET. No restes el GET del gym.",
            "",
            "INGESTA",
            "Cada alimento: kcal = kcal_porcion × cantidad. Las comidas son el subtotal de esos ítems. de_afuera = sí cuenta kcal pero suele no traer macros.",
            "Un día con comida_cargada = no NO entra en promedios ni en el déficit acumulado. No lo trates como ingesta 0.",
            "",
            "DÉFICIT",
            "deficit_kcal = gasto_kcal − ingesta_kcal. Positivo = comiste menos de lo que gastaste. Negativo = superávit.",
            "",
            "HOJAS",
            "Contexto: persona, objetivo, GET/BMR vigentes (de la medición de referencia, no de cada día).",
            "Resumen: totales y medias SOLO de días con comida cargada.",
            "Días: un día del calendario por fila, con el desglose del gasto.",
            "Alimentos: grano fino de la dieta (cada ítem de cada comida).",
            "Comidas: subtotal por comida del día.",
            "Gym: cada sesión y sus kcal que entran al gasto. Ejercicios gym: press, series, reps, peso (no suman kcal extra: las kcal están en Gym).",
            "Cardio y Deporte: cada sesión/actividad y sus kcal.",
            "Mediciones: báscula en el período. grasa_kg y musculo_kg = peso × %. comparable = ayunas, misma hora (las otras se guardan pero no contrastan con el modelo). referencia = la que manda el GET.",
            "",
            "PUNTO DE PARTIDA SUGERIDO PARA LA IA",
            "Analizá adherencia proteica, % de kcal de afuera, si el extra de gym/cardio/deporte explica el déficit, y si las mediciones (grasa_kg) pegan con el déficit acumulado (~7700 kcal por kg de grasa, según la config típica). Distinguí días sin carga de días en superávit. No inventes comidas ni entrenos que no estén en las tablas."
        ];
        ws.Cell(1, 1).Value = "texto";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("1F6B4A");
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        for (var i = 0; i < lineas.Length; i++)
        {
            ws.Cell(i + 2, 1).Value = lineas[i];
            ws.Cell(i + 2, 1).Style.Alignment.WrapText = true;
            if (lineas[i] is "GASTO DEL DÍA" or "INGESTA" or "DÉFICIT" or "HOJAS" or "PUNTO DE PARTIDA SUGERIDO PARA LA IA"
                || lineas[i].StartsWith("EXTRACTO", StringComparison.Ordinal))
                ws.Cell(i + 2, 1).Style.Font.Bold = true;
        }
        ws.Column(1).Width = 118;
        ws.SheetView.FreezeRows(1);
    }

    private static void Contexto(IXLWorksheet ws, DatosExport d)
    {
        var p = d.Perfil;
        var edad = p.FechaNacimiento is { } nac ? Metabolismo.Edad(nac, d.Hasta) : null;
        ClaveValor(ws,
            ("periodo_desde", Iso(d.Desde)),
            ("periodo_hasta", Iso(d.Hasta)),
            ("objetivo", TipoObjetivoNombres.De(p.TipoObjetivo)),
            ("objetivo_detalle", TipoObjetivoNombres.Detalle(p.TipoObjetivo)),
            ("grasa_objetivo_pct", p.GrasaObjetivoPct),
            ("plazo_semanas", p.PlazoSemanas),
            ("inicio_objetivo", p.FechaInicioObjetivo is { } fi ? Iso(fi) : ""),
            ("proteina_objetivo_g", p.ProteinaObjetivoG),
            ("deficit_diario_fijo_kcal", p.DeficitDiarioKcal),
            ("sexo", p.Sexo is { } s ? s.ToString() : ""),
            ("altura_cm", p.AlturaCm),
            ("edad_anios", edad),
            ("factor_actividad", p.FactorActividad),
            ("medicion_referencia_fecha", d.Referencia is { } r ? Iso(r.Fecha) : ""),
            ("peso_referencia_kg", d.Referencia?.PesoKg),
            ("grasa_referencia_pct", d.Referencia?.GrasaPct),
            ("bmr_kcal", d.Bmr?.Kcal),
            ("bmr_metodo", d.Bmr?.Aplicado is { } m ? Metabolismo.Etiqueta(m) : ""),
            ("get_kcal", d.Get),
            ("get_regla", "GET = BMR × factor_actividad. El gasto del día suma gym+cardio+deporte encima."));
    }

    private static void Resumen(IXLWorksheet ws, DatosExport d)
    {
        var m = d.Metricas;
        var conR = m.Serie.Where(x => x.HayRegistro).ToList();
        var gastoMedio = conR.Count == 0 ? 0 : conR.Average(x => x.Gasto);
        var ingestaTot = conR.Sum(x => x.Ingesta ?? 0);
        var gastoTot = conR.Sum(x => x.Gasto);
        ClaveValor(ws,
            ("periodo_desde", Iso(d.Desde)),
            ("periodo_hasta", Iso(d.Hasta)),
            ("dias_totales", m.DiasTotales),
            ("dias_con_comida", m.DiasConRegistro),
            ("cobertura", Formato.Pct(m.Cobertura)),
            ("ingesta_total_kcal", Redondeo.Kcal(ingestaTot)),
            ("gasto_total_kcal", Redondeo.Kcal(gastoTot)),
            ("ingesta_media_kcal", Redondeo.Kcal(m.IngestaMedia)),
            ("gasto_medio_kcal", Redondeo.Kcal(gastoMedio)),
            ("deficit_acumulado_kcal", Redondeo.Kcal(m.DeficitAcumulado)),
            ("deficit_medio_kcal", Redondeo.Kcal(m.DeficitMedio)),
            ("proteina_media_g", Math.Round(m.ProteinaMedia, 1, MidpointRounding.AwayFromZero)),
            ("carbos_media_g", Math.Round(m.CarbosMedia, 1, MidpointRounding.AwayFromZero)),
            ("grasa_media_g", Math.Round(m.GrasaMedia, 1, MidpointRounding.AwayFromZero)),
            ("sesiones_gym", m.SesionesGym),
            ("kcal_gym", Redondeo.Kcal(m.KcalGym)),
            ("sesiones_cardio", m.SesionesCardio),
            ("kcal_cardio", Redondeo.Kcal(m.KcalCardio)),
            ("sesiones_deporte", m.SesionesDeporte),
            ("kcal_deporte", Redondeo.Kcal(m.KcalDeporte)),
            ("dias_afuera", m.DiasAfuera),
            ("pct_kcal_afuera", Formato.Pct(m.PctAfuera)),
            ("nota", "Medias y acumulados solo con días comida_cargada = sí. Ver hoja Para_IA."));
    }

    private static void Dias(IXLWorksheet ws, IReadOnlyList<DiaDePeriodo> serie)
    {
        Encabezados(ws, "fecha_iso", "fecha", "dia_semana", "comida_cargada",
            "ingesta_kcal", "gasto_kcal", "get_kcal", "gym_kcal", "cardio_kcal", "deporte_kcal",
            "deficit_kcal", "proteina_g", "carbos_g", "grasa_g");
        var f = 2;
        foreach (var d in serie)
        {
            var get = d.Gasto - d.KcalGym - d.KcalCardio - d.KcalDeporte;
            IsoCelda(ws.Cell(f, 1), d.Fecha);
            Fecha(ws.Cell(f, 2), d.Fecha);
            ws.Cell(f, 3).Value = Formato.DiaCorto(d.Fecha);
            ws.Cell(f, 4).Value = d.HayRegistro ? "sí" : "no";
            Entero(ws.Cell(f, 5), d.HayRegistro ? d.Ingesta : null);
            Entero(ws.Cell(f, 6), d.Gasto);
            Entero(ws.Cell(f, 7), get);
            Entero(ws.Cell(f, 8), d.KcalGym);
            Entero(ws.Cell(f, 9), d.KcalCardio);
            Entero(ws.Cell(f, 10), d.KcalDeporte);
            Entero(ws.Cell(f, 11), d.Deficit);
            Macro(ws.Cell(f, 12), d.HayRegistro ? d.Proteina : null);
            Macro(ws.Cell(f, 13), d.HayRegistro ? d.Carbos : null);
            Macro(ws.Cell(f, 14), d.HayRegistro ? d.Grasa : null);
            f++;
        }
        Ajustar(ws, 14, f - 1);
    }

    private static void Alimentos(IXLWorksheet ws, IReadOnlyList<Comida> comidas)
    {
        Encabezados(ws, "fecha_iso", "fecha", "comida", "alimento", "porcion", "cantidad",
            "kcal_porcion", "kcal", "proteina_g", "carbos_g", "grasa_g", "de_afuera", "plato");
        var f = 2;
        foreach (var c in comidas)
        {
            foreach (var i in c.Items.OrderBy(x => x.Id))
            {
                IsoCelda(ws.Cell(f, 1), c.Fecha);
                Fecha(ws.Cell(f, 2), c.Fecha);
                ws.Cell(f, 3).Value = c.TipoComida?.Nombre ?? "Comida";
                ws.Cell(f, 4).Value = i.Nombre;
                ws.Cell(f, 5).Value = i.Porcion;
                ws.Cell(f, 6).Value = i.Cantidad;
                ws.Cell(f, 6).Style.NumberFormat.Format = "0.##";
                Entero(ws.Cell(f, 7), i.Kcal);
                Entero(ws.Cell(f, 8), i.Kcal * i.Cantidad);
                Macro(ws.Cell(f, 9), i.Proteina is { } p ? p * i.Cantidad : null);
                Macro(ws.Cell(f, 10), i.Carbos is { } cb ? cb * i.Cantidad : null);
                Macro(ws.Cell(f, 11), i.Grasa is { } g ? g * i.Cantidad : null);
                ws.Cell(f, 12).Value = i.EsAfuera ? "sí" : "no";
                ws.Cell(f, 13).Value = i.GrupoNombre ?? "";
                f++;
            }
        }
        Ajustar(ws, 13, f - 1);
        ws.Column(4).Width = 32;
    }

    private static void HojaComidas(IXLWorksheet ws, IReadOnlyList<Comida> comidas)
    {
        Encabezados(ws, "fecha_iso", "fecha", "comida", "kcal", "proteina_g", "carbos_g", "grasa_g", "items", "nota");
        var f = 2;
        foreach (var c in comidas)
        {
            var tot = BalanceDiario.SumarIngesta(c.Items.Select(i =>
                new ItemIngesta(i.Cantidad, i.Kcal, i.Proteina, i.Carbos, i.Grasa)));
            IsoCelda(ws.Cell(f, 1), c.Fecha);
            Fecha(ws.Cell(f, 2), c.Fecha);
            ws.Cell(f, 3).Value = c.TipoComida?.Nombre ?? "Comida";
            Entero(ws.Cell(f, 4), tot.Kcal);
            Macro(ws.Cell(f, 5), tot.Proteina);
            Macro(ws.Cell(f, 6), tot.Carbos);
            Macro(ws.Cell(f, 7), tot.Grasa);
            ws.Cell(f, 8).Value = tot.Items;
            ws.Cell(f, 9).Value = c.Nota ?? "";
            f++;
        }
        Ajustar(ws, 9, f - 1);
    }

    private static void Gym(IXLWorksheet ws, IReadOnlyList<SesionGym> sesiones)
    {
        Encabezados(ws, "fecha_iso", "fecha", "sesion", "cantidad", "kcal", "nota", "ejercicios");
        var f = 2;
        foreach (var s in sesiones)
        {
            IsoCelda(ws.Cell(f, 1), s.Fecha);
            Fecha(ws.Cell(f, 2), s.Fecha);
            ws.Cell(f, 3).Value = s.Nombre;
            ws.Cell(f, 4).Value = s.Cantidad;
            ws.Cell(f, 4).Style.NumberFormat.Format = "0.##";
            Entero(ws.Cell(f, 5), s.Kcal);
            ws.Cell(f, 6).Value = s.Nota ?? "";
            ws.Cell(f, 7).Value = s.Ejercicios.Count;
            f++;
        }
        Ajustar(ws, 7, f - 1);
        ws.Column(3).Width = 22;
    }

    private static void EjerciciosGym(IXLWorksheet ws, IReadOnlyList<SesionGym> sesiones)
    {
        Encabezados(ws, "fecha_iso", "fecha", "sesion", "orden", "ejercicio", "grupo_muscular",
            "series", "reps", "peso_kg", "puede_mas", "observacion");
        var f = 2;
        foreach (var s in sesiones)
        {
            foreach (var e in s.Ejercicios.OrderBy(x => x.Orden).ThenBy(x => x.Id))
            {
                IsoCelda(ws.Cell(f, 1), s.Fecha);
                Fecha(ws.Cell(f, 2), s.Fecha);
                ws.Cell(f, 3).Value = s.Nombre;
                ws.Cell(f, 4).Value = e.Orden;
                ws.Cell(f, 5).Value = e.Nombre;
                ws.Cell(f, 6).Value = e.GrupoMuscular ?? "";
                ws.Cell(f, 7).Value = e.Series;
                if (e.Reps is { } r) ws.Cell(f, 8).Value = r;
                if (e.Peso is { } p)
                {
                    ws.Cell(f, 9).Value = p;
                    ws.Cell(f, 9).Style.NumberFormat.Format = "0.##";
                }
                ws.Cell(f, 10).Value = e.PuedeMas ? "sí" : "no";
                ws.Cell(f, 11).Value = e.Observacion ?? "";
                f++;
            }
        }
        Ajustar(ws, 11, f - 1);
        ws.Column(5).Width = 28;
    }

    private static void Cardio(IXLWorksheet ws, IReadOnlyList<SesionCardio> sesiones)
    {
        Encabezados(ws, "fecha_iso", "fecha", "protocolo", "minutos", "velocidad", "inclinacion",
            "cantidad", "kcal", "kcal_min", "kcal_max", "nota");
        var f = 2;
        foreach (var s in sesiones)
        {
            IsoCelda(ws.Cell(f, 1), s.Fecha);
            Fecha(ws.Cell(f, 2), s.Fecha);
            ws.Cell(f, 3).Value = s.Protocolo?.Nombre ?? s.Nota ?? "Cardio";
            ws.Cell(f, 4).Value = s.Minutos;
            ws.Cell(f, 4).Style.NumberFormat.Format = "0.##";
            if (s.Velocidad is { } v)
            {
                ws.Cell(f, 5).Value = v;
                ws.Cell(f, 5).Style.NumberFormat.Format = "0.##";
            }
            if (s.Inclinacion is { } n)
            {
                ws.Cell(f, 6).Value = n;
                ws.Cell(f, 6).Style.NumberFormat.Format = "0.##";
            }
            ws.Cell(f, 7).Value = s.Cantidad;
            ws.Cell(f, 7).Style.NumberFormat.Format = "0.##";
            Entero(ws.Cell(f, 8), s.Kcal);
            Entero(ws.Cell(f, 9), s.KcalMin);
            Entero(ws.Cell(f, 10), s.KcalMax);
            ws.Cell(f, 11).Value = s.Nota ?? "";
            f++;
        }
        Ajustar(ws, 11, f - 1);
        ws.Column(3).Width = 22;
    }

    private static void Deporte(IXLWorksheet ws, IReadOnlyList<ActividadLog> logs)
    {
        Encabezados(ws, "fecha_iso", "fecha", "actividad", "tipo", "minutos", "cantidad", "kcal");
        var f = 2;
        foreach (var s in logs)
        {
            IsoCelda(ws.Cell(f, 1), s.Fecha);
            Fecha(ws.Cell(f, 2), s.Fecha);
            ws.Cell(f, 3).Value = s.Nombre;
            ws.Cell(f, 4).Value = s.Tipo;
            if (s.Minutos is { } min)
            {
                ws.Cell(f, 5).Value = min;
                ws.Cell(f, 5).Style.NumberFormat.Format = "0.##";
            }
            ws.Cell(f, 6).Value = s.Cantidad;
            ws.Cell(f, 6).Style.NumberFormat.Format = "0.##";
            Entero(ws.Cell(f, 7), s.Kcal);
            f++;
        }
        Ajustar(ws, 7, f - 1);
        ws.Column(3).Width = 24;
    }

    private static void Mediciones(IXLWorksheet ws, IReadOnlyList<Medicion> lista)
    {
        Encabezados(ws, "fecha_iso", "fecha", "peso_kg", "grasa_pct", "musculo_pct", "visceral",
            "grasa_kg", "musculo_kg", "magra_kg", "comparable", "referencia", "nota");
        var f = 2;
        foreach (var m in lista)
        {
            IsoCelda(ws.Cell(f, 1), m.Fecha);
            Fecha(ws.Cell(f, 2), m.Fecha);
            Kg(ws.Cell(f, 3), m.PesoKg, 1);
            Pct(ws.Cell(f, 4), m.GrasaPct);
            Pct(ws.Cell(f, 5), m.MusculoPct);
            if (m.Visceral is { } vis) ws.Cell(f, 6).Value = vis;
            Kg(ws.Cell(f, 7), Composicion.GrasaKg(m.PesoKg, m.GrasaPct));
            Kg(ws.Cell(f, 8), Composicion.MusculoKg(m.PesoKg, m.MusculoPct));
            Kg(ws.Cell(f, 9), Composicion.MagraKg(m.PesoKg, m.GrasaPct));
            ws.Cell(f, 10).Value = m.EsComparable ? "sí" : "no";
            ws.Cell(f, 11).Value = m.EsReferencia ? "sí" : "no";
            ws.Cell(f, 12).Value = m.Nota ?? "";
            f++;
        }
        Ajustar(ws, 12, f - 1);
    }

    private static void ClaveValor(IXLWorksheet ws, params (string Clave, object? Valor)[] filas)
    {
        Encabezados(ws, "dato", "valor");
        for (var i = 0; i < filas.Length; i++)
        {
            ws.Cell(i + 2, 1).Value = filas[i].Clave;
            var v = filas[i].Valor;
            if (v is null) continue;
            ws.Cell(i + 2, 2).Value = v switch
            {
                int n => n,
                double d => d,
                string s => s,
                _ => v.ToString() ?? ""
            };
        }
        ws.Column(1).Width = 32;
        ws.Column(2).Width = 88;
    }

    private static void Encabezados(IXLWorksheet ws, params string[] cols)
    {
        for (var i = 0; i < cols.Length; i++)
        {
            var c = ws.Cell(1, i + 1);
            c.Value = cols[i];
            c.Style.Font.Bold = true;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("1F6B4A");
            c.Style.Font.FontColor = XLColor.White;
        }
        ws.SheetView.FreezeRows(1);
    }

    private static void Ajustar(IXLWorksheet ws, int cols, int ultimaFila)
    {
        ws.Columns().AdjustToContents();
        if (ultimaFila >= 2)
            ws.Range(1, 1, ultimaFila, cols).SetAutoFilter();
    }

    private static string Iso(DateOnly f) => f.ToString("yyyy-MM-dd");

    private static void IsoCelda(IXLCell c, DateOnly f)
    {
        c.Value = Iso(f);
        c.Style.NumberFormat.Format = "@";
    }

    private static void Fecha(IXLCell c, DateOnly f)
    {
        c.Value = f.ToDateTime(TimeOnly.MinValue);
        c.Style.DateFormat.Format = "yyyy-mm-dd";
    }

    private static void Entero(IXLCell c, double? v)
    {
        if (v is null) return;
        c.Value = Redondeo.Kcal(v.Value);
        c.Style.NumberFormat.Format = "0";
    }

    private static void Macro(IXLCell c, double? v)
    {
        if (v is null) return;
        c.Value = Math.Round(v.Value, 1, MidpointRounding.AwayFromZero);
        c.Style.NumberFormat.Format = "0.0";
    }

    private static void Kg(IXLCell c, double? v, int dec = 2)
    {
        if (v is null) return;
        c.Value = Redondeo.Kg(v.Value, dec);
        c.Style.NumberFormat.Format = dec == 1 ? "0.0" : "0.00";
    }

    private static void Pct(IXLCell c, double? v)
    {
        if (v is null) return;
        c.Value = Math.Round(v.Value, 1, MidpointRounding.AwayFromZero);
        c.Style.NumberFormat.Format = "0.0";
    }
}
