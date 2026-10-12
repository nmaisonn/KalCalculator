using KcalTracker.Calculo;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class RutinasTests
{
    [Fact]
    public async Task SePuedeCrearUnaRutinaConDiasYEjercicios()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        var svc = new ServicioRutinas(db);

        db.Ejercicios.Add(new Ejercicio { Nombre = "Jalón", Activo = true });
        await db.SaveChangesAsync();
        var jalon = await db.Ejercicios.SingleAsync();

        var (ok, _, rutina) = await svc.CrearAsync("Mi rutina", "4 días", activar: true);
        Assert.True(ok);
        Assert.True(rutina!.EsActiva);

        Assert.True((await svc.AgregarDiaAsync(rutina.Id, "Espalda + bíceps", 330)).Ok);
        var dia = await db.RutinaDias.SingleAsync();
        Assert.True((await svc.AgregarEjercicioAsync(dia.Id, jalon.Id, 4, 6, 10, 65, true, null)).Ok);

        var recargada = await db.Rutinas.Include(r => r.Dias).ThenInclude(d => d.Ejercicios).SingleAsync();
        Assert.Equal("Espalda + bíceps", recargada.Dias.Single().Nombre);
        Assert.Equal(330, recargada.Dias.Single().KcalEstimadas);
        Assert.Equal(65, recargada.Dias.Single().Ejercicios.Single().PesoRef);
        Assert.True(recargada.Dias.Single().Ejercicios.Single().PuedeMas);
    }

    [Fact]
    public async Task ActivarUnaRutinaApagaLaAnterior()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        var svc = new ServicioRutinas(db);

        var a = (await svc.CrearAsync("A", null, true)).Rutina!;
        var b = (await svc.CrearAsync("B", null, false)).Rutina!;
        Assert.True((await db.Rutinas.SingleAsync(r => r.Id == a.Id)).EsActiva);

        Assert.True((await svc.ActivarAsync(b.Id)).Ok);

        Assert.False((await db.Rutinas.SingleAsync(r => r.Id == a.Id)).EsActiva);
        Assert.True((await db.Rutinas.SingleAsync(r => r.Id == b.Id)).EsActiva);
    }

    [Fact]
    public async Task EditarLaRutinaNoCambiaUnaSesionYaRegistrada()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        var svc = new ServicioRutinas(db);

        db.Ejercicios.Add(new Ejercicio { Nombre = "Press", Activo = true });
        await db.SaveChangesAsync();
        var press = await db.Ejercicios.SingleAsync();
        var rutina = (await svc.CrearAsync("Gym", null, true)).Rutina!;
        await svc.AgregarDiaAsync(rutina.Id, "Pecho", 310);
        var dia = await db.RutinaDias.SingleAsync();
        await svc.AgregarEjercicioAsync(dia.Id, press.Id, 3, 8, 10, 40, false, null);

        db.SesionesGym.Add(new SesionGym
        {
            Fecha = new DateOnly(2026, 9, 10),
            Nombre = "Pecho",
            RutinaDiaId = dia.Id,
            Kcal = 310,
            Ejercicios =
            [
                new SesionEjercicio { Nombre = "Press", EjercicioId = press.Id, Series = 3, Reps = 8, Peso = 40 }
            ]
        });
        await db.SaveChangesAsync();

        await svc.GuardarDiaAsync(dia.Id, "Pecho pesado", 390);
        await svc.GuardarEjercicioAsync(
            (await db.RutinaEjercicios.SingleAsync()).Id, 5, 5, 6, 80, true, null);

        var sesion = await db.SesionesGym.Include(s => s.Ejercicios).SingleAsync();
        Assert.Equal("Pecho", sesion.Nombre);
        Assert.Equal(310, sesion.Kcal);
        Assert.Equal(3, sesion.Ejercicios.Single().Series);
        Assert.Equal(40, sesion.Ejercicios.Single().Peso);
        Assert.Equal(390, (await db.RutinaDias.SingleAsync()).KcalEstimadas);
    }

    [Fact]
    public async Task SePuedeReordenarEjerciciosDeUnDia()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        var svc = new ServicioRutinas(db);

        db.Ejercicios.AddRange(
            new Ejercicio { Nombre = "Jalón", Activo = true },
            new Ejercicio { Nombre = "Remo", Activo = true });
        await db.SaveChangesAsync();
        var idsEj = await db.Ejercicios.Select(e => e.Id).ToListAsync();
        var rutina = (await svc.CrearAsync("Espalda", null, true)).Rutina!;
        await svc.AgregarDiaAsync(rutina.Id, "A", 300);
        var dia = await db.RutinaDias.SingleAsync();
        Assert.True((await svc.AgregarEjercicioAsync(dia.Id, idsEj[0], 4, 8, 10, 50, false, null)).Ok);
        Assert.True((await svc.AgregarEjercicioAsync(dia.Id, idsEj[1], 3, 8, 10, 40, false, null)).Ok);

        var items = await db.RutinaEjercicios.OrderBy(e => e.Orden).ToListAsync();
        Assert.Equal(1, items[0].Orden);
        Assert.Equal(2, items[1].Orden);

        Assert.True((await svc.ReordenarEjerciciosAsync(dia.Id, [items[1].Id, items[0].Id])).Ok);

        var reordenados = await db.RutinaEjercicios.OrderBy(e => e.Orden).ToListAsync();
        Assert.Equal(items[1].Id, reordenados[0].Id);
        Assert.Equal(items[0].Id, reordenados[1].Id);

        Assert.False((await svc.ReordenarEjerciciosAsync(dia.Id, [items[0].Id])).Ok);
        Assert.False((await svc.ReordenarEjerciciosAsync(dia.Id, [999, items[0].Id])).Ok);
    }

    [Fact]
    public async Task LaProgresionDeUnEjercicioOrdenaLasSesionesConPeso()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);

        db.Ejercicios.Add(new Ejercicio { Nombre = "Jalón", Activo = true });
        await db.SaveChangesAsync();
        var jalon = await db.Ejercicios.SingleAsync();

        db.SesionesGym.AddRange(
            new SesionGym
            {
                Fecha = new DateOnly(2026, 8, 4),
                Nombre = "Espalda",
                Ejercicios = [new SesionEjercicio { Nombre = "Jalón", EjercicioId = jalon.Id, Peso = 65, Reps = 8 }]
            },
            new SesionGym
            {
                Fecha = new DateOnly(2026, 9, 1),
                Nombre = "Espalda",
                Ejercicios = [new SesionEjercicio { Nombre = "Jalón", EjercicioId = jalon.Id, Peso = 67.5, Reps = 7 }]
            },
            new SesionGym
            {
                Fecha = new DateOnly(2026, 9, 15),
                Nombre = "Espalda",
                Ejercicios = [new SesionEjercicio { Nombre = "Jalón", EjercicioId = jalon.Id, Peso = null, Reps = 8 }]
            });
        await db.SaveChangesAsync();

        var pts = await new ServicioGym(db).ProgresionAsync(jalon.Id);
        Assert.Equal(2, pts.Count);
        Assert.Equal(65, pts[0].Peso);
        Assert.Equal(67.5, pts[1].Peso);
        var g = Progresion.Grafico(pts);
        Assert.NotNull(g);
        Assert.Equal(2, g.Puntos.Count);
    }
}
