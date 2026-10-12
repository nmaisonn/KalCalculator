using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class CatalogosTests
{
    [Fact]
    public async Task CA27_RenombrarYDesactivarNoTocaElHistoricoNiSeOfreceParaCargasNuevas()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        var catalogos = new ServicioCatalogos(db);

        await catalogos.AgregarAsync(Catalogo.Comida, "Merienda");
        await catalogos.AgregarAsync(Catalogo.Comida, "Media mañana");

        var tipos = await db.TiposComida.OrderBy(t => t.Orden).ToListAsync();
        var merienda = tipos.Single(t => t.Nombre == "Merienda");
        var media = tipos.Single(t => t.Nombre == "Media mañana");

        db.Comidas.Add(new Comida
        {
            Fecha = new DateOnly(2026, 9, 10),
            TipoComidaId = merienda.Id,
            Items = [Item("Yogur", 120)]
        });
        db.Comidas.Add(new Comida
        {
            Fecha = new DateOnly(2026, 9, 10),
            TipoComidaId = media.Id,
            Items = [Item("Manzana", 94)]
        });
        await db.SaveChangesAsync();

        await catalogos.RenombrarAsync(Catalogo.Comida, merienda.Id, "Meriendita");
        await catalogos.CambiarActivoAsync(Catalogo.Comida, media.Id, false);

        var delDia = await db.Comidas
            .Include(c => c.TipoComida)
            .Include(c => c.Items)
            .Where(c => c.Fecha == new DateOnly(2026, 9, 10))
            .ToListAsync();

        Assert.Equal(2, delDia.Count);
        Assert.Equal(214, delDia.Sum(c => c.Items.Sum(i => i.Kcal * i.Cantidad)));
        Assert.Contains(delDia, c => c.TipoComida!.Nombre == "Meriendita");
        Assert.Contains(delDia, c => c.TipoComida!.Nombre == "Media mañana" && !c.TipoComida.Activo);

        var paraCargar = await catalogos.ActivosParaCargaNuevaAsync(Catalogo.Comida);
        Assert.Contains(paraCargar, t => t.Nombre == "Meriendita");
        Assert.DoesNotContain(paraCargar, t => t.Nombre == "Media mañana");
    }

    [Fact]
    public async Task NoSeBorraUnTipoEnUsoSeOfreceDesactivar()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using var db = baseDatos.Como(ana);
        var catalogos = new ServicioCatalogos(db);

        await catalogos.AgregarAsync(Catalogo.Comida, "Cena");
        var cena = await db.TiposComida.SingleAsync();
        db.Comidas.Add(new Comida
        {
            Fecha = new DateOnly(2026, 9, 10),
            TipoComidaId = cena.Id,
            Items = [Item("Pollo", 200)]
        });
        await db.SaveChangesAsync();

        var (ok, error) = await catalogos.EliminarAsync(Catalogo.Comida, cena.Id);

        Assert.False(ok);
        Assert.Contains("Desactivalo", error);
        Assert.NotNull(await db.TiposComida.SingleAsync());
    }

    private static ComidaItem Item(string nombre, double kcal) => new()
    {
        Nombre = nombre,
        Porcion = "1",
        Cantidad = 1,
        Kcal = kcal
    };
}
