using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class EsquemaTests
{
    [Fact]
    public void ElModeloTieneTodasLasTablasDelDominio()
    {
        using var baseDatos = new BaseDePrueba();
        using var db = baseDatos.Como(null);

        var tablas = db.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(n => n is not null)
            .ToHashSet();

        string[] esperadas =
        [
            "Usuarios", "Configuraciones", "Perfiles",
            "TiposComida", "TiposActividad", "CategoriasAlimento", "GruposMusculares",
            "Alimentos", "Comidas", "ComidaItems", "Combos", "ComboItems",
            "Actividades", "ActividadLogs",
            "Ejercicios", "Rutinas", "RutinaDias", "RutinaEjercicios",
            "SesionesGym", "SesionEjercicios", "SeriesRealizadas",
            "ProtocolosCardio", "BloquesCardio", "SesionesCardio",
            "Mediciones"
        ];

        foreach (var tabla in esperadas)
            Assert.Contains(tabla, tablas);
    }

    [Fact]
    public async Task LaConfiguracionNaceConLosValoresDocumentados()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using var db = baseDatos.Como(ana);
        db.Configuraciones.Add(new Configuracion());
        db.Perfiles.Add(new Perfil());
        await db.SaveChangesAsync();

        var cfg = await db.Configuraciones.SingleAsync();
        Assert.Equal(ValoresDeFabrica.KcalPorKgGrasa, cfg.KcalPorKgGrasa);
        Assert.Equal(ValoresDeFabrica.KcalPorKgMagro, cfg.KcalPorKgMagro);
        Assert.Equal(MetodoBmr.KatchMcArdle, cfg.MetodoBmr);
        Assert.Equal(0.25, cfg.PctGastoMax);
        Assert.Equal(0.0075, cfg.PctPesoSemanal);
        Assert.Equal(0.50, cfg.CobMinContraste);
        Assert.Equal(0.70, cfg.CobMinCalibrado);

        var perfil = await db.Perfiles.SingleAsync();
        Assert.Equal(ValoresDeFabrica.FactorActividad, perfil.FactorActividad);
        Assert.Equal(8, perfil.PlazoSemanas);
        Assert.Equal(TipoObjetivo.Corte, perfil.TipoObjetivo);
        Assert.Equal(FuenteAlimentos.Propios, perfil.FuenteAlimentos);
        Assert.Null(perfil.DeficitDiarioKcal);
        Assert.Null(perfil.GrasaObjetivoPct);
    }

    [Fact]
    public async Task UnDiaDeRutinaGuardaLasKcalEstimadasYLaSesionPuedeAjustarlas()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using var db = baseDatos.Como(ana);
        var rutina = new Rutina
        {
            Nombre = "Mi rutina",
            EsActiva = true,
            Dias =
            [
                new RutinaDia { Nombre = "Espalda y bíceps", Orden = 1, KcalEstimadas = 330 }
            ]
        };
        db.Rutinas.Add(rutina);
        await db.SaveChangesAsync();

        db.SesionesGym.Add(new SesionGym
        {
            Fecha = new DateOnly(2026, 9, 17),
            RutinaDiaId = rutina.Dias[0].Id,
            Nombre = rutina.Dias[0].Nombre,
            Kcal = 280
        });
        await db.SaveChangesAsync();

        Assert.Equal(330, (await db.RutinaDias.SingleAsync()).KcalEstimadas);
        Assert.Equal(280, (await db.SesionesGym.SingleAsync()).Kcal);
    }

    [Fact]
    public async Task NoPuedeHaberDosRutinasActivasEnLaMismaCuenta()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using var db = baseDatos.Como(ana);
        db.Rutinas.Add(new Rutina { Nombre = "A", EsActiva = true });
        await db.SaveChangesAsync();

        db.Rutinas.Add(new Rutina { Nombre = "B", EsActiva = true });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DosCuentasPuedenTenerCadaUnaSuRutinaActiva()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, beto) = await baseDatos.CrearAnaYBeto();

        await using (var dbAna = baseDatos.Como(ana))
        {
            dbAna.Rutinas.Add(new Rutina { Nombre = "De Ana", EsActiva = true });
            await dbAna.SaveChangesAsync();
        }

        await using var dbBeto = baseDatos.Como(beto);
        dbBeto.Rutinas.Add(new Rutina { Nombre = "De Beto", EsActiva = true });
        await dbBeto.SaveChangesAsync();

        Assert.Single(await dbBeto.Rutinas.ToListAsync());
    }

    [Fact]
    public async Task UnProtocoloDeCardioGuardaBloquesYLaDuracionSeDeriva()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using var db = baseDatos.Como(ana);
        db.ProtocolosCardio.Add(new ProtocoloCardio
        {
            Nombre = "HIIT jueves",
            Bloques =
            [
                new BloqueCardio { Orden = 1, Minutos = 5, Velocidad = 6, Inclinacion = 1, Repeticiones = 1 },
                new BloqueCardio { Orden = 2, Minutos = 1, Velocidad = 10, Inclinacion = 2, Repeticiones = 8 },
                new BloqueCardio { Orden = 3, Minutos = 3, Velocidad = 5, Inclinacion = 0, Repeticiones = 1 }
            ]
        });
        await db.SaveChangesAsync();

        var protocolo = await db.ProtocolosCardio.Include(p => p.Bloques).SingleAsync();
        var duracion = protocolo.Bloques.Sum(b => b.Minutos * b.Repeticiones);
        Assert.Equal(16, duracion);
    }

    [Fact]
    public async Task ElPesoDeUnEjercicioPuedeSerSoloObservacion()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using var db = baseDatos.Como(ana);
        var grupo = new GrupoMuscular { Nombre = "Pecho", Orden = 1 };
        var ejercicio = new Ejercicio { Nombre = "Peck deck", GrupoMuscular = grupo };
        db.GruposMusculares.Add(grupo);
        db.Ejercicios.Add(ejercicio);

        var rutina = new Rutina
        {
            Nombre = "Mi rutina",
            Dias =
            [
                new RutinaDia
                {
                    Nombre = "Pecho",
                    Orden = 1,
                    Ejercicios =
                    [
                        new RutinaEjercicio
                        {
                            Ejercicio = ejercicio,
                            Orden = 1,
                            Series = 3,
                            RepsMin = 10,
                            RepsMax = 12,
                            Observacion = "Máximo máquina"
                        }
                    ]
                }
            ]
        };
        db.Rutinas.Add(rutina);
        await db.SaveChangesAsync();

        var prescripcion = await db.RutinaEjercicios.SingleAsync();
        Assert.Null(prescripcion.PesoRef);
        Assert.False(prescripcion.PuedeMas);
        Assert.Equal("Máximo máquina", prescripcion.Observacion);
    }
}
