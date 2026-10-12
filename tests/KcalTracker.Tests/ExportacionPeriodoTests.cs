using ClosedXML.Excel;
using KcalTracker.Calculo;
using KcalTracker.Dominio;
using KcalTracker.Servicios;

namespace KcalTracker.Tests;

public class ExportacionPeriodoTests
{
    private static readonly DateOnly Lunes = new(2026, 9, 14);
    private static readonly DateOnly Martes = new(2026, 9, 15);
    private static readonly DateOnly Miercoles = new(2026, 9, 16);
    private static readonly DateOnly Jueves = new(2026, 9, 17);

    [Fact]
    public async Task ElExcelDelPeriodoTraeDiasComidasYAlimentosConKcal()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        var (desayuno, almuerzo) = await SembrarTiposAsync(dbAna);
        dbAna.Comidas.Add(new Comida
        {
            Fecha = Lunes,
            TipoComidaId = desayuno,
            Items =
            [
                Item("Pan", 127, 2, 4.5, 1.2, 2),
                Item("Café con leche", 50, 1, 6, 2, 1)
            ]
        });
        dbAna.Comidas.Add(new Comida
        {
            Fecha = Lunes,
            TipoComidaId = almuerzo,
            Items = [Item("Pollo", 165, 1, 31, 0, 3.6)]
        });
        dbAna.Comidas.Add(new Comida
        {
            Fecha = Miercoles,
            TipoComidaId = desayuno,
            Items = [Item("Yogur", 80, 1, 8, 10, 2)]
        });
        dbAna.SesionesGym.Add(new SesionGym { Fecha = Miercoles, Nombre = "Push", Kcal = 280 });
        await dbAna.SaveChangesAsync();

        using var wb = Abrir(await Exportar(dbAna, Lunes, Miercoles));
        var resumen = wb.Worksheet("Resumen");
        var dias = wb.Worksheet("Días");
        var comidas = wb.Worksheet("Comidas");
        var alimentos = wb.Worksheet("Alimentos");

        Assert.Equal("2026-09-14", ValorClave(resumen, "periodo_desde"));
        Assert.Equal("2026-09-16", ValorClave(resumen, "periodo_hasta"));
        Assert.Equal("2", ValorClave(resumen, "dias_con_comida"));
        Assert.Equal("3", ValorClave(resumen, "dias_totales"));

        Assert.Equal(4, dias.LastRowUsed()!.RowNumber());
        Assert.Equal("sí", Texto(dias, "comida_cargada", 2));
        Assert.Equal(Redondeo.Kcal(127 * 2 + 50 + 165), Entero(dias, "ingesta_kcal", 2));
        Assert.Equal("2026-09-14", Texto(dias, "fecha_iso", 2));
        Assert.Equal("no", Texto(dias, "comida_cargada", 3));
        Assert.True(Celda(dias, "ingesta_kcal", 3).IsEmpty());
        Assert.Equal("sí", Texto(dias, "comida_cargada", 4));
        Assert.Equal(80, Entero(dias, "ingesta_kcal", 4));
        Assert.Equal(280, Entero(dias, "gym_kcal", 4));
        Assert.Equal(280, Entero(dias, "gasto_kcal", 4));

        Assert.Equal(4, comidas.LastRowUsed()!.RowNumber());
        Assert.Equal("Desayuno", Texto(comidas, "comida", 2));
        Assert.Equal(Redondeo.Kcal(127 * 2 + 50), Entero(comidas, "kcal", 2));
        Assert.Equal("Almuerzo", Texto(comidas, "comida", 3));
        Assert.Equal(165, Entero(comidas, "kcal", 3));
        Assert.Equal(80, Entero(comidas, "kcal", 4));

        Assert.Equal(5, alimentos.LastRowUsed()!.RowNumber());
        Assert.Equal("Pan", Texto(alimentos, "alimento", 2));
        Assert.Equal(2, Celda(alimentos, "cantidad", 2).GetValue<double>());
        Assert.Equal(127, Entero(alimentos, "kcal_porcion", 2));
        Assert.Equal(254, Entero(alimentos, "kcal", 2));
        Assert.Equal("Pollo", Texto(alimentos, "alimento", 4));
        Assert.Equal(165, Entero(alimentos, "kcal", 4));
        Assert.Equal("Yogur", Texto(alimentos, "alimento", 5));
    }

    [Fact]
    public async Task ElExcelDesglosaEntrenoYMedicionesDelPeriodo()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await SembrarTiposAsync(dbAna);
        dbAna.SesionesGym.Add(new SesionGym
        {
            Fecha = Lunes,
            Nombre = "Push",
            Kcal = 280,
            Ejercicios =
            [
                new SesionEjercicio { Orden = 1, Nombre = "Press banca", GrupoMuscular = "Pecho", Series = 3, Reps = 8, Peso = 60 }
            ]
        });
        dbAna.SesionesCardio.Add(new SesionCardio
        {
            Fecha = Martes, Minutos = 20, Velocidad = 10, Inclinacion = 1, Kcal = 180, Nota = "HIIT"
        });
        dbAna.ActividadLogs.Add(new ActividadLog
        {
            Fecha = Miercoles, Nombre = "Fútbol", Tipo = "Deporte", Minutos = 60, Kcal = 450
        });
        dbAna.Mediciones.Add(new Medicion
        {
            Fecha = Lunes, PesoKg = 74.1, GrasaPct = 18.8, MusculoPct = 41.2, Visceral = 7,
            EsComparable = true, EsReferencia = true
        });
        dbAna.Mediciones.Add(new Medicion
        {
            Fecha = Jueves, PesoKg = 73.0, GrasaPct = 18.0, EsComparable = true
        });
        await dbAna.SaveChangesAsync();

        using var wb = Abrir(await Exportar(dbAna, Lunes, Miercoles));
        var paraIa = string.Join("\n", wb.Worksheet("Para_IA").CellsUsed().Select(c => c.GetString()));
        Assert.Contains("EXTRACTO", paraIa, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GET", paraIa);

        var gym = wb.Worksheet("Gym");
        Assert.Equal("Push", Texto(gym, "sesion", 2));
        Assert.Equal(280, Entero(gym, "kcal", 2));

        var ejercicios = wb.Worksheet("Ejercicios gym");
        Assert.Equal("Press banca", Texto(ejercicios, "ejercicio", 2));
        Assert.Equal(3, Celda(ejercicios, "series", 2).GetValue<int>());
        Assert.Equal(60, Celda(ejercicios, "peso_kg", 2).GetValue<double>());

        var cardio = wb.Worksheet("Cardio");
        Assert.Equal("HIIT", Texto(cardio, "protocolo", 2));
        Assert.Equal(180, Entero(cardio, "kcal", 2));
        Assert.Equal(20, Celda(cardio, "minutos", 2).GetValue<double>());

        var deporte = wb.Worksheet("Deporte");
        Assert.Equal("Fútbol", Texto(deporte, "actividad", 2));
        Assert.Equal(450, Entero(deporte, "kcal", 2));

        var meds = wb.Worksheet("Mediciones");
        Assert.Equal(2, meds.LastRowUsed()!.RowNumber());
        Assert.Equal("2026-09-14", Texto(meds, "fecha_iso", 2));
        Assert.Equal(74.1, Celda(meds, "peso_kg", 2).GetValue<double>(), 1);
        Assert.Equal(Redondeo.Kg(Composicion.GrasaKg(74.1, 18.8)!.Value), Celda(meds, "grasa_kg", 2).GetValue<double>());
        Assert.Equal("sí", Texto(meds, "referencia", 2));
        Assert.DoesNotContain("2026-09-17", meds.Column(Col(meds, "fecha_iso")).CellsUsed().Select(c => c.GetString()));
    }

    [Fact]
    public async Task UnDiaFueraDelRangoNoEntra()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        var (desayuno, _) = await SembrarTiposAsync(dbAna);
        dbAna.Comidas.Add(new Comida
        {
            Fecha = Jueves,
            TipoComidaId = desayuno,
            Items = [Item("Fuera", 999, 1, 1, 1, 1)]
        });
        await dbAna.SaveChangesAsync();

        using var wb = Abrir(await Exportar(dbAna, Lunes, Miercoles));
        var alimentos = wb.Worksheet("Alimentos");
        Assert.Equal(1, alimentos.LastRowUsed()!.RowNumber());
        Assert.Equal("alimento", alimentos.Cell(1, Col(alimentos, "alimento")).GetString());
        Assert.Equal(1, wb.Worksheet("Comidas").LastRowUsed()!.RowNumber());
    }

    [Fact]
    public async Task LasComidasDeOtraCuentaNoSalen()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, beto) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using (var dbBeto = baseDatos.Como(beto))
        {
            var (desayuno, _) = await SembrarTiposAsync(dbBeto);
            dbBeto.Comidas.Add(new Comida
            {
                Fecha = Lunes,
                TipoComidaId = desayuno,
                Items = [Item("De Beto", 400, 1, 10, 10, 10)]
            });
            await dbBeto.SaveChangesAsync();
        }

        await using var dbAna = baseDatos.Como(ana);
        await SembrarTiposAsync(dbAna);
        using var wb = Abrir(await Exportar(dbAna, Lunes, Lunes));
        Assert.Equal(1, wb.Worksheet("Alimentos").LastRowUsed()!.RowNumber());
        Assert.Equal("no", Texto(wb.Worksheet("Días"), "comida_cargada", 2));
    }

    private static async Task<byte[]> Exportar(KcalTracker.Datos.KcalDbContext db, DateOnly desde, DateOnly hasta)
    {
        var dia = new ServicioDia(db);
        return await new ServicioExportacionPeriodo(db, new ServicioPeriodo(db, dia), dia)
            .ArmarAsync(desde, hasta);
    }

    private static XLWorkbook Abrir(byte[] bytes) => new(new MemoryStream(bytes));

    private static async Task<(int Desayuno, int Almuerzo)> SembrarTiposAsync(KcalTracker.Datos.KcalDbContext db)
    {
        var desayuno = new TipoComida { Nombre = "Desayuno", Orden = 1 };
        var almuerzo = new TipoComida { Nombre = "Almuerzo", Orden = 2 };
        db.TiposComida.AddRange(desayuno, almuerzo);
        await db.SaveChangesAsync();
        return (desayuno.Id, almuerzo.Id);
    }

    private static ComidaItem Item(string nombre, double kcal, double cantidad, double? p, double? c, double? g)
        => new()
        {
            Nombre = nombre,
            Porcion = "1",
            Kcal = kcal,
            Cantidad = cantidad,
            Proteina = p,
            Carbos = c,
            Grasa = g
        };

    private static int Col(IXLWorksheet ws, string header)
    {
        var ultima = ws.Row(1).LastCellUsed()!.Address.ColumnNumber;
        for (var i = 1; i <= ultima; i++)
        {
            if (ws.Cell(1, i).GetString() == header)
                return i;
        }
        Assert.Fail($"No está la columna '{header}' en {ws.Name}.");
        return 0;
    }

    private static IXLCell Celda(IXLWorksheet ws, string header, int fila) => ws.Cell(fila, Col(ws, header));
    private static string Texto(IXLWorksheet ws, string header, int fila) => Celda(ws, header, fila).GetString();
    private static int Entero(IXLWorksheet ws, string header, int fila) => Celda(ws, header, fila).GetValue<int>();

    private static string ValorClave(IXLWorksheet ws, string clave)
    {
        var ultima = ws.LastRowUsed()!.RowNumber();
        for (var f = 2; f <= ultima; f++)
        {
            if (ws.Cell(f, 1).GetString() == clave)
                return ws.Cell(f, 2).GetFormattedString();
        }
        Assert.Fail($"No está '{clave}' en {ws.Name}.");
        return "";
    }
}
