using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Datos.Plantillas;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class CatalogoAlimentosTests
{
    [Fact]
    public async Task ConAmbosSeVenLosDelCatalogoYLosPropios()
    {
        using var baseDatos = new BaseDePrueba();
        await baseDatos.SembrarCatalogoAsync();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using (var db = baseDatos.Como(ana))
        {
            db.Alimentos.Add(new Alimento { Nombre = "Pan de Ana", Porcion = "1", Kcal = 80, Activo = true });
            await db.SaveChangesAsync();
        }

        await using var dbAna = baseDatos.Como(ana);
        var visibles = await CatalogoAlimentos.CargarAsync(
            dbAna.Alimentos.Where(a => a.Activo),
            ana, baseDatos.Catalogo.Id, FuenteAlimentos.Ambos);

        Assert.Contains(visibles, a => a.Nombre == "Pan de Ana");
        Assert.Contains(visibles, a => a.Nombre == "Huevo");
        Assert.Equal(CatalogoApp.Alimentos.Count + 1, visibles.Count);
    }

    [Fact]
    public async Task ConPropiosNoApareceElCatalogo()
    {
        using var baseDatos = new BaseDePrueba();
        await baseDatos.SembrarCatalogoAsync();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using (var db = baseDatos.Como(ana))
        {
            db.Alimentos.Add(new Alimento { Nombre = "Pan de Ana", Porcion = "1", Kcal = 80, Activo = true });
            await db.SaveChangesAsync();
        }

        await using var dbAna = baseDatos.Como(ana);
        var visibles = await CatalogoAlimentos.CargarAsync(
            dbAna.Alimentos.Where(a => a.Activo),
            ana, baseDatos.Catalogo.Id, FuenteAlimentos.Propios);

        Assert.Equal(["Pan de Ana"], visibles.Select(a => a.Nombre).ToArray());
    }

    [Fact]
    public async Task ConCatalogoNoAparecenLosPropios()
    {
        using var baseDatos = new BaseDePrueba();
        await baseDatos.SembrarCatalogoAsync();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using (var db = baseDatos.Como(ana))
        {
            db.Alimentos.Add(new Alimento { Nombre = "Pan de Ana", Porcion = "1", Kcal = 80, Activo = true });
            await db.SaveChangesAsync();
        }

        await using var dbAna = baseDatos.Como(ana);
        var visibles = await CatalogoAlimentos.CargarAsync(
            dbAna.Alimentos.Where(a => a.Activo),
            ana, baseDatos.Catalogo.Id, FuenteAlimentos.Catalogo);

        Assert.DoesNotContain(visibles, a => a.Nombre == "Pan de Ana");
        Assert.Equal(CatalogoApp.Alimentos.Count, visibles.Count);
    }

    [Fact]
    public async Task SiElUsuarioTieneElMismoAlimentoSeOcultaElDelCatalogo()
    {
        using var baseDatos = new BaseDePrueba();
        await baseDatos.SembrarCatalogoAsync();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        var plantilla = CatalogoApp.Alimentos[0];

        await using (var db = baseDatos.Como(ana))
        {
            db.Alimentos.Add(new Alimento
            {
                Nombre = plantilla.Nombre,
                Porcion = plantilla.Porcion,
                Kcal = 1,
                Activo = true
            });
            await db.SaveChangesAsync();
        }

        await using var dbAna = baseDatos.Como(ana);
        var visibles = await CatalogoAlimentos.CargarAsync(
            dbAna.Alimentos.Where(a => a.Activo),
            ana, baseDatos.Catalogo.Id, FuenteAlimentos.Ambos);

        var panes = visibles.Where(a => a.Nombre == plantilla.Nombre).ToList();
        Assert.Single(panes);
        Assert.Equal(ana, panes[0].UsuarioId);
        Assert.Equal(1, panes[0].Kcal);
    }

    [Fact]
    public async Task CA23_ConCatalogoSigueSinVerseLoDeOtraCuenta()
    {
        using var baseDatos = new BaseDePrueba();
        await baseDatos.SembrarCatalogoAsync();
        var (ana, beto) = await baseDatos.CrearAnaYBeto();

        await using (var dbBeto = baseDatos.Como(beto))
        {
            dbBeto.Alimentos.Add(new Alimento { Nombre = "Pan de Beto", Porcion = "50 g", Kcal = 127, Activo = true });
            await dbBeto.SaveChangesAsync();
        }

        await using var dbAna = baseDatos.Como(ana);
        var nombres = await dbAna.Alimentos.Select(a => a.Nombre).ToListAsync();
        Assert.DoesNotContain("Pan de Beto", nombres);
        Assert.Contains("Huevo", nombres);
    }

    [Fact]
    public async Task NoSePuedeEditarUnAlimentoDelCatalogo()
    {
        using var baseDatos = new BaseDePrueba();
        await baseDatos.SembrarCatalogoAsync();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using var db = baseDatos.Como(ana);
        var delCatalogo = await db.Alimentos.FirstAsync(a => a.UsuarioId == baseDatos.Catalogo.Id);
        var (ok, error, _) = await new ServicioAlimentos(db).GuardarAsync(new Alimento
        {
            Id = delCatalogo.Id,
            Nombre = "Hackeado",
            Porcion = delCatalogo.Porcion,
            Kcal = 1,
            Activo = true
        });

        Assert.False(ok);
        Assert.Contains("catálogo", error, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(delCatalogo.Nombre, (await db.Alimentos.FindAsync(delCatalogo.Id))!.Nombre);
    }

    [Fact]
    public async Task SePuedeRegistrarUnaComidaConUnAlimentoDelCatalogo()
    {
        using var baseDatos = new BaseDePrueba();
        await baseDatos.SembrarCatalogoAsync();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using var db = baseDatos.Como(ana);
        var tipo = new TipoComida { Nombre = "Desayuno", Orden = 1 };
        db.TiposComida.Add(tipo);
        await db.SaveChangesAsync();

        var delCatalogo = await db.Alimentos.FirstAsync(a => a.UsuarioId == baseDatos.Catalogo.Id);
        db.Comidas.Add(new Comida
        {
            Fecha = new DateOnly(2026, 9, 21),
            TipoComidaId = tipo.Id,
            Items =
            [
                new ComidaItem
                {
                    AlimentoId = delCatalogo.Id,
                    Cantidad = 1,
                    Nombre = delCatalogo.Nombre,
                    Porcion = delCatalogo.Porcion,
                    Kcal = delCatalogo.Kcal
                }
            ]
        });

        await db.SaveChangesAsync();
        Assert.Single(await db.Comidas.Include(c => c.Items).ToListAsync());
    }
}

public class CatalogoAppTests
{
    [Fact]
    public void TieneUnaBaseSolidaSinDuplicadosYConMacrosCoherentes()
    {
        var alimentos = CatalogoApp.Alimentos;
        Assert.True(alimentos.Count >= 180, $"El catálogo tiene {alimentos.Count} alimentos.");

        var claves = alimentos
            .Select(a => a.Nombre.Trim() + "\n" + a.Porcion.Trim())
            .ToList();
        Assert.Equal(claves.Count, claves.Distinct(StringComparer.CurrentCultureIgnoreCase).Count());

        var conMacros = alimentos.Where(a => a.Proteina is not null && a.Carbos is not null && a.Grasa is not null).ToList();
        Assert.True(conMacros.Count >= 150);
        foreach (var a in conMacros)
        {
            var desvio = CoherenciaMacros.Desvio(a.Kcal, a.Proteina, a.Carbos, a.Grasa);
            Assert.True(desvio <= 0.03, $"{a.Nombre} ({a.Porcion}): desvío {desvio:P1}");
        }

        Assert.All(alimentos.Where(a => a.EsAfuera), a =>
        {
            Assert.Equal("De afuera", a.Categoria);
            Assert.Null(a.Proteina);
        });
    }
}
