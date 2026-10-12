using System.Text;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class AlimentosTests
{
    [Fact]
    public async Task ImportarAgregaNuevosYOmiteDuplicados()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        db.Alimentos.Add(new Alimento { Nombre = "Pan", Porcion = "50 g", Kcal = 127, Activo = true });
        await db.SaveChangesAsync();

        var r = await new ServicioImportacionAlimentos(db).ImportarFilasAsync(
        [
            new(2, "Pan", "50 g", 127, 4, 25, 1, null, false),
            new(3, "Manzana", "1 (180 g)", 94, 0.5, 22, 0.3, null, false),
        ]);

        Assert.Equal(1, r.Agregados);
        Assert.Equal(1, r.YaEstaban);
        Assert.Equal(2, await db.Alimentos.CountAsync());
        Assert.Contains(await db.Alimentos.ToListAsync(), a => a.Nombre == "Manzana" && a.Kcal == 94);
    }

    [Fact]
    public async Task ImportarAsignaElTramoPorNombre()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        db.TiposComida.Add(new TipoComida { Nombre = "Almuerzo", Orden = 1, Activo = true });
        await db.SaveChangesAsync();

        var r = await new ServicioImportacionAlimentos(db).ImportarFilasAsync(
        [
            new(2, "Pechuga", "150 g", 165, 31, 0, 3.6, "almuerzo", false),
        ]);

        Assert.Equal(1, r.Agregados);
        var pechuga = await db.Alimentos.Include(a => a.TipoComida).SingleAsync();
        Assert.Equal("Almuerzo", pechuga.TipoComida?.Nombre);
    }

    [Fact]
    public async Task ImportarCsvConPuntoYComa()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        db.TiposComida.Add(new TipoComida { Nombre = "Cena", Orden = 1, Activo = true });
        await db.SaveChangesAsync();

        var csv = """
            Nombre;Porción;kcal;Proteína;Carbos;Grasa;Tipo de comida;De afuera
            Cuadril;250 g;375;55;0;17,5;Cena;no
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var r = await new ServicioImportacionAlimentos(db).ImportarAsync(stream, "alimentos.csv");

        Assert.Equal(1, r.Agregados);
        var fila = await db.Alimentos.Include(a => a.TipoComida).SingleAsync();
        Assert.Equal("Cuadril", fila.Nombre);
        Assert.Equal(375, fila.Kcal);
        Assert.Equal(17.5, fila.Grasa);
        Assert.Equal("Cena", fila.TipoComida?.Nombre);
    }

    [Fact]
    public async Task ImportarXlsxDeLaPlantilla()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        db.TiposComida.Add(new TipoComida { Nombre = "Almuerzo", Orden = 1, Activo = true });
        await db.SaveChangesAsync();

        using var stream = new MemoryStream(ServicioImportacionAlimentos.PlantillaXlsx());
        var r = await new ServicioImportacionAlimentos(db).ImportarAsync(stream, "alimentos.xlsx");

        Assert.Equal(1, r.Agregados);
        var pechuga = await db.Alimentos.Include(a => a.TipoComida).SingleAsync();
        Assert.Equal("Pechuga al horno", pechuga.Nombre);
        Assert.Equal(165, pechuga.Kcal);
        Assert.Equal(3.6, pechuga.Grasa);
        Assert.Equal("Almuerzo", pechuga.TipoComida?.Nombre);
    }

    [Fact]
    public async Task GuardarAsignaTipoDeComida()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        var cena = new TipoComida { Nombre = "Cena", Orden = 1, Activo = true };
        db.TiposComida.Add(cena);
        await db.SaveChangesAsync();

        var alimentos = new ServicioAlimentos(db);
        await alimentos.GuardarAsync(new Alimento
        {
            Nombre = "Tortilla", Porcion = "1", Kcal = 180, TipoComidaId = cena.Id, Activo = true
        });

        var guardado = await db.Alimentos.Include(a => a.TipoComida).SingleAsync();
        Assert.Equal("Cena", guardado.TipoComida?.Nombre);

        await alimentos.GuardarAsync(new Alimento
        {
            Id = guardado.Id, Nombre = "Tortilla", Porcion = "1", Kcal = 180,
            TipoComidaId = null, Activo = true
        });
        Assert.Null((await db.Alimentos.SingleAsync()).TipoComidaId);
    }

    [Fact]
    public async Task GuardarAsignaTipoDeAlimentoYMarcaAfuera()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        var afuera = new CategoriaAlimento { Nombre = "De afuera", Orden = 1, Activo = true };
        var fruta = new CategoriaAlimento { Nombre = "Fruta", Orden = 0, Activo = true };
        db.CategoriasAlimento.AddRange(fruta, afuera);
        await db.SaveChangesAsync();

        var alimentos = new ServicioAlimentos(db);
        await alimentos.GuardarAsync(new Alimento
        {
            Nombre = "Mila del club", Porcion = "1", Kcal = 1250,
            CategoriaAlimentoId = afuera.Id, Activo = true
        });

        var guardado = await db.Alimentos.Include(a => a.CategoriaAlimento).SingleAsync();
        Assert.Equal("De afuera", guardado.CategoriaAlimento?.Nombre);
        Assert.True(guardado.EsAfuera);
    }

    [Fact]
    public void HoyAgrupaAlimentosPorTipo()
    {
        var fruta = new CategoriaAlimento { Id = 1, Nombre = "Fruta", Orden = 0 };
        var afuera = new CategoriaAlimento { Id = 2, Nombre = "De afuera", Orden = 1 };
        var grupos = GruposAlimentoHoy.De(
        [
            new Alimento { Nombre = "Mila", Kcal = 700, CategoriaAlimentoId = 2, CategoriaAlimento = afuera },
            new Alimento { Nombre = "Manzana", Kcal = 94, CategoriaAlimentoId = 1, CategoriaAlimento = fruta },
            new Alimento { Nombre = "Mandarina", Kcal = 48, CategoriaAlimentoId = 1, CategoriaAlimento = fruta },
            new Alimento { Nombre = "Huevo", Kcal = 72 }
        ]);

        Assert.Equal(["Fruta", "De afuera", "Sin tipo"], grupos.Select(g => g.Etiqueta).ToArray());
        Assert.Equal(["Mandarina", "Manzana"], grupos[0].Items.Select(a => a.Nombre).ToArray());
    }

    [Fact]
    public void ElBuscadorEncuentraSinTildes()
    {
        Assert.True(ServicioImportacionAlimentos.TextoContiene("Puré de papas", "pure"));
        Assert.False(ServicioImportacionAlimentos.TextoContiene("Pan", "manzana"));
    }
}
