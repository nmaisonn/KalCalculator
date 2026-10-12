using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

/// <summary>
/// CA-23, CA-24 y CA-25. Las tres se verifican contra la misma base física con dos
/// cuentas, porque si cada una tuviera la suya el aislamiento no se estaría probando.
/// </summary>
public class AislamientoTests
{
    [Fact]
    public async Task CA23_UnaCuentaNoVeLosAlimentosDeOtra()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, beto) = await baseDatos.CrearAnaYBeto();

        await using (var dbAna = baseDatos.Como(ana))
        {
            dbAna.Alimentos.Add(PanDe("Ana"));
            await dbAna.SaveChangesAsync();
        }

        await using (var dbBeto = baseDatos.Como(beto))
        {
            dbBeto.Alimentos.Add(PanDe("Beto"));
            await dbBeto.SaveChangesAsync();
        }

        await using (var dbAna = baseDatos.Como(ana))
        {
            var nombres = await dbAna.Alimentos.Select(a => a.Nombre).ToListAsync();
            Assert.Equal(["Pan de Ana"], nombres);
        }

        await using (var dbBeto = baseDatos.Como(beto))
        {
            var nombres = await dbBeto.Alimentos.Select(a => a.Nombre).ToListAsync();
            Assert.Equal(["Pan de Beto"], nombres);
        }
    }

    [Fact]
    public async Task CA24_UnaConsultaSinFiltroPorUsuarioDevuelveVacioNoDatosAjenos()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, beto) = await baseDatos.CrearAnaYBeto();

        await using (var dbAna = baseDatos.Como(ana))
        {
            dbAna.Alimentos.Add(PanDe("Ana"));
            await dbAna.SaveChangesAsync();
        }

        // La consulta no filtra por usuario: se apoya en el filtro global (D-13).
        // Si el aislamiento dependiera de que quien escribe se acuerde del Where,
        // acá aparecería el pan de Ana.
        await using var dbBeto = baseDatos.Como(beto);
        var alimentos = await dbBeto.Alimentos.ToListAsync();

        Assert.Empty(alimentos);
    }

    [Fact]
    public async Task CA24_SinUsuarioActualTampocoSeVenDatos()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using (var dbAna = baseDatos.Como(ana))
        {
            dbAna.Alimentos.Add(PanDe("Ana"));
            await dbAna.SaveChangesAsync();
        }

        await using var sinSesion = baseDatos.Como(null);
        Assert.Empty(await sinSesion.Alimentos.ToListAsync());
    }

    [Fact]
    public async Task CA25_NoSePuedeRegistrarUnaComidaConUnAlimentoDeOtraCuenta()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, beto) = await baseDatos.CrearAnaYBeto();

        int alimentoDeBeto;
        await using (var dbBeto = baseDatos.Como(beto))
        {
            var alimento = PanDe("Beto");
            dbBeto.Alimentos.Add(alimento);
            await dbBeto.SaveChangesAsync();
            alimentoDeBeto = alimento.Id;
        }

        int desayunoDeAna;
        await using (var dbAna = baseDatos.Como(ana))
        {
            var tipo = new TipoComida { Nombre = "Desayuno", Orden = 1 };
            dbAna.TiposComida.Add(tipo);
            await dbAna.SaveChangesAsync();
            desayunoDeAna = tipo.Id;
        }

        await using (var dbAna = baseDatos.Como(ana))
        {
            dbAna.Comidas.Add(new Comida
            {
                Fecha = new DateOnly(2026, 9, 17),
                TipoComidaId = desayunoDeAna,
                Items =
                [
                    new ComidaItem
                    {
                        AlimentoId = alimentoDeBeto,
                        Cantidad = 1,
                        Nombre = "Pan de Beto",
                        Porcion = "50 g",
                        Kcal = 127
                    }
                ]
            });

            var error = await Assert.ThrowsAsync<ReferenciaAjenaException>(
                () => dbAna.SaveChangesAsync());

            Assert.Contains("otra cuenta", error.Message);
        }

        await using (var dbAna = baseDatos.Como(ana))
        {
            Assert.Empty(await dbAna.Comidas.ToListAsync());
        }
    }

    [Fact]
    public async Task CA25_SiElAlimentoEsDeLaMismaCuentaSeAcepta()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using var dbAna = baseDatos.Como(ana);

        var tipo = new TipoComida { Nombre = "Desayuno", Orden = 1 };
        var alimento = PanDe("Ana");
        dbAna.TiposComida.Add(tipo);
        dbAna.Alimentos.Add(alimento);
        await dbAna.SaveChangesAsync();

        dbAna.Comidas.Add(new Comida
        {
            Fecha = new DateOnly(2026, 9, 17),
            TipoComidaId = tipo.Id,
            Items =
            [
                new ComidaItem
                {
                    AlimentoId = alimento.Id,
                    Cantidad = 1,
                    Nombre = alimento.Nombre,
                    Porcion = alimento.Porcion,
                    Kcal = alimento.Kcal
                }
            ]
        });

        await dbAna.SaveChangesAsync();

        Assert.Single(await dbAna.Comidas.Include(c => c.Items).ToListAsync());
    }

    [Fact]
    public async Task CA23_TampocoSeVenMedicionesRutinasNiSesionesAjenas()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, beto) = await baseDatos.CrearAnaYBeto();

        await using (var dbBeto = baseDatos.Como(beto))
        {
            dbBeto.Mediciones.Add(new Medicion { Fecha = new DateOnly(2026, 9, 15), PesoKg = 80 });
            dbBeto.Rutinas.Add(new Rutina
            {
                Nombre = "Rutina de Beto",
                Dias = [new RutinaDia { Nombre = "Día 1", Orden = 1, KcalEstimadas = 330 }]
            });
            dbBeto.SesionesGym.Add(new SesionGym { Fecha = new DateOnly(2026, 9, 17), Nombre = "Día 1", Kcal = 330 });
            dbBeto.SesionesCardio.Add(new SesionCardio { Fecha = new DateOnly(2026, 9, 17), Minutos = 30, Kcal = 180 });
            await dbBeto.SaveChangesAsync();
        }

        await using var dbAna = baseDatos.Como(ana);
        Assert.Empty(await dbAna.Mediciones.ToListAsync());
        Assert.Empty(await dbAna.Rutinas.ToListAsync());
        Assert.Empty(await dbAna.RutinaDias.ToListAsync());
        Assert.Empty(await dbAna.SesionesGym.ToListAsync());
        Assert.Empty(await dbAna.SesionesCardio.ToListAsync());
    }

    [Fact]
    public async Task CA25_NoSePuedeEntrenarUnDiaDeRutinaDeOtraCuenta()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, beto) = await baseDatos.CrearAnaYBeto();

        int diaDeBeto;
        await using (var dbBeto = baseDatos.Como(beto))
        {
            var rutina = new Rutina
            {
                Nombre = "Rutina de Beto",
                Dias = [new RutinaDia { Nombre = "Día 1", Orden = 1, KcalEstimadas = 330 }]
            };
            dbBeto.Rutinas.Add(rutina);
            await dbBeto.SaveChangesAsync();
            diaDeBeto = rutina.Dias[0].Id;
        }

        await using var dbAna = baseDatos.Como(ana);
        dbAna.SesionesGym.Add(new SesionGym
        {
            Fecha = new DateOnly(2026, 9, 17),
            RutinaDiaId = diaDeBeto,
            Nombre = "Día 1",
            Kcal = 330
        });

        var error = await Assert.ThrowsAsync<ReferenciaAjenaException>(() => dbAna.SaveChangesAsync());
        Assert.Contains("otra cuenta", error.Message);
    }

    private static Alimento PanDe(string quien) => new()
    {
        Nombre = $"Pan de {quien}",
        Porcion = "50 g",
        Kcal = 127,
        Proteina = 4.3
    };
}
