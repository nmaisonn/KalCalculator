using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

public enum Catalogo
{
    Comida,
    Actividad,
    Categoria,
    Musculo
}

public sealed record ItemCatalogo(int Id, string Nombre, int Orden, bool Activo, int Usos);

/// <summary>
/// Alta, renombrado, orden y baja lógica de los cuatro catálogos de tipos (RF-I3, RF-I4).
/// Desactivar no toca el histórico (RF-I13, CA-27).
/// </summary>
public class ServicioCatalogos(KcalDbContext db)
{
    public async Task<IReadOnlyList<ItemCatalogo>> ListarAsync(Catalogo catalogo, CancellationToken ct = default)
    {
        var items = catalogo switch
        {
            Catalogo.Comida => await db.TiposComida.OrderBy(t => t.Orden).Select(t => new { t.Id, t.Nombre, t.Orden, t.Activo }).ToListAsync(ct),
            Catalogo.Actividad => await db.TiposActividad.OrderBy(t => t.Orden).Select(t => new { t.Id, t.Nombre, t.Orden, t.Activo }).ToListAsync(ct),
            Catalogo.Categoria => await db.CategoriasAlimento.Where(t => t.UsuarioId == db.UsuarioIdActual).OrderBy(t => t.Orden).Select(t => new { t.Id, t.Nombre, t.Orden, t.Activo }).ToListAsync(ct),
            Catalogo.Musculo => await db.GruposMusculares.OrderBy(t => t.Orden).Select(t => new { t.Id, t.Nombre, t.Orden, t.Activo }).ToListAsync(ct),
            _ => throw new ArgumentOutOfRangeException(nameof(catalogo))
        };

        var usos = await ContarUsosAsync(catalogo, ct);
        return items.Select(t => new ItemCatalogo(t.Id, t.Nombre, t.Orden, t.Activo, usos.GetValueOrDefault(t.Id))).ToList();
    }

    public async Task<IReadOnlyList<ItemCatalogo>> ActivosParaCargaNuevaAsync(Catalogo catalogo, CancellationToken ct = default)
        => (await ListarAsync(catalogo, ct)).Where(i => i.Activo).ToList();

    public async Task<(bool Ok, string? Error)> AgregarAsync(Catalogo catalogo, string nombre, CancellationToken ct = default)
    {
        nombre = nombre.Trim();
        if (nombre.Length == 0) return (false, "El nombre no puede quedar vacío.");

        if (await ExisteNombreAsync(catalogo, nombre, null, ct))
            return (false, "Ya hay un ítem con ese nombre.");

        var orden = await ProximoOrdenAsync(catalogo, ct);

        switch (catalogo)
        {
            case Catalogo.Comida:
                db.TiposComida.Add(new TipoComida { Nombre = nombre, Orden = orden });
                break;
            case Catalogo.Actividad:
                db.TiposActividad.Add(new TipoActividad { Nombre = nombre, Orden = orden });
                break;
            case Catalogo.Categoria:
                db.CategoriasAlimento.Add(new CategoriaAlimento { Nombre = nombre, Orden = orden });
                break;
            case Catalogo.Musculo:
                db.GruposMusculares.Add(new GrupoMuscular { Nombre = nombre, Orden = orden });
                break;
        }

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RenombrarAsync(Catalogo catalogo, int id, string nombre, CancellationToken ct = default)
    {
        nombre = nombre.Trim();
        if (nombre.Length == 0) return (false, "El nombre no puede quedar vacío.");

        var item = await BuscarAsync(catalogo, id, ct);
        if (item is null) return (false, "No está.");

        if (await ExisteNombreAsync(catalogo, nombre, id, ct))
            return (false, "Ya hay un ítem con ese nombre.");

        item.Nombre = nombre;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CambiarActivoAsync(Catalogo catalogo, int id, bool activo, CancellationToken ct = default)
    {
        var item = await BuscarAsync(catalogo, id, ct);
        if (item is null) return (false, "No está.");

        item.Activo = activo;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> MoverAsync(Catalogo catalogo, int id, int direccion, CancellationToken ct = default)
    {
        var lista = (await ListarEntidadesAsync(catalogo, ct)).OrderBy(t => t.Orden).ToList();
        var indice = lista.FindIndex(t => t.Id == id);
        var destino = indice + direccion;
        if (indice < 0 || destino < 0 || destino >= lista.Count)
            return (false, null);

        (lista[indice].Orden, lista[destino].Orden) = (lista[destino].Orden, lista[indice].Orden);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>
    /// Borra de verdad solo si nadie lo usó. Si tiene histórico, hay que desactivar (CA-27).
    /// </summary>
    public async Task<(bool Ok, string? Error)> EliminarAsync(Catalogo catalogo, int id, CancellationToken ct = default)
    {
        var usos = await ContarUsosDeAsync(catalogo, id, ct);
        if (usos > 0)
            return (false, $"Está en {usos} registro{(usos == 1 ? "" : "s")}. Desactivalo en lugar de borrarlo.");

        var item = await BuscarAsync(catalogo, id, ct);
        if (item is null) return (false, "No está.");

        db.Remove(item);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>
    /// Garantiza el tipo «De afuera» y se lo asigna a los alimentos marcados sin categoría.
    /// </summary>
    public async Task<CategoriaAlimento> AsegurarAfueraAsync(CancellationToken ct = default)
    {
        var existentes = await db.CategoriasAlimento
            .Where(c => c.UsuarioId == db.UsuarioIdActual).ToListAsync(ct);
        var afuera = existentes.FirstOrDefault(c =>
            string.Equals(c.Nombre.Trim(), CategoriaAlimento.NombreAfuera, StringComparison.CurrentCultureIgnoreCase));
        var restaurante = existentes.FirstOrDefault(c =>
            string.Equals(c.Nombre.Trim(), "Restaurante", StringComparison.CurrentCultureIgnoreCase));
        if (afuera is null && restaurante is not null)
        {
            restaurante.Nombre = CategoriaAlimento.NombreAfuera;
            afuera = restaurante;
            await db.SaveChangesAsync(ct);
        }
        else if (afuera is null)
        {
            afuera = new CategoriaAlimento
            {
                Nombre = CategoriaAlimento.NombreAfuera,
                Orden = existentes.Count == 0 ? 0 : existentes.Max(c => c.Orden) + 1,
                Activo = true
            };
            db.CategoriasAlimento.Add(afuera);
            await db.SaveChangesAsync(ct);
        }
        else if (restaurante is not null && restaurante.Id != afuera.Id)
        {
            var deRestaurante = await db.Alimentos.Where(a => a.CategoriaAlimentoId == restaurante.Id).ToListAsync(ct);
            foreach (var a in deRestaurante)
                a.CategoriaAlimentoId = afuera.Id;
            db.CategoriasAlimento.Remove(restaurante);
            await db.SaveChangesAsync(ct);
        }

        var huerfanos = await db.Alimentos
            .Where(a => a.EsAfuera && a.CategoriaAlimentoId == null && a.UsuarioId == db.UsuarioIdActual)
            .ToListAsync(ct);
        if (huerfanos.Count > 0)
        {
            foreach (var a in huerfanos)
                a.CategoriaAlimentoId = afuera.Id;
            await db.SaveChangesAsync(ct);
        }

        return afuera;
    }

    private async Task<ITipoDeCatalogo?> BuscarAsync(Catalogo catalogo, int id, CancellationToken ct)
        => catalogo switch
        {
            Catalogo.Comida => await db.TiposComida.FirstOrDefaultAsync(t => t.Id == id, ct),
            Catalogo.Actividad => await db.TiposActividad.FirstOrDefaultAsync(t => t.Id == id, ct),
            Catalogo.Categoria => await db.CategoriasAlimento
                .FirstOrDefaultAsync(t => t.Id == id && t.UsuarioId == db.UsuarioIdActual, ct),
            Catalogo.Musculo => await db.GruposMusculares.FirstOrDefaultAsync(t => t.Id == id, ct),
            _ => null
        };

    private async Task<List<ITipoDeCatalogo>> ListarEntidadesAsync(Catalogo catalogo, CancellationToken ct)
    {
        IEnumerable<ITipoDeCatalogo> items = catalogo switch
        {
            Catalogo.Comida => await db.TiposComida.ToListAsync(ct),
            Catalogo.Actividad => await db.TiposActividad.ToListAsync(ct),
            Catalogo.Categoria => await db.CategoriasAlimento.Where(t => t.UsuarioId == db.UsuarioIdActual).ToListAsync(ct),
            Catalogo.Musculo => await db.GruposMusculares.ToListAsync(ct),
            _ => []
        };
        return items.ToList();
    }

    private async Task<int> ProximoOrdenAsync(Catalogo catalogo, CancellationToken ct)
    {
        var max = catalogo switch
        {
            Catalogo.Comida => await db.TiposComida.Select(t => (int?)t.Orden).MaxAsync(ct),
            Catalogo.Actividad => await db.TiposActividad.Select(t => (int?)t.Orden).MaxAsync(ct),
            Catalogo.Categoria => await db.CategoriasAlimento
                .Where(t => t.UsuarioId == db.UsuarioIdActual)
                .Select(t => (int?)t.Orden).MaxAsync(ct),
            Catalogo.Musculo => await db.GruposMusculares.Select(t => (int?)t.Orden).MaxAsync(ct),
            _ => 0
        };
        return (max ?? 0) + 1;
    }

    private async Task<bool> ExisteNombreAsync(Catalogo catalogo, string nombre, int? exceptoId, CancellationToken ct)
    {
        var normalizado = nombre.Trim();
        return catalogo switch
        {
            Catalogo.Comida => await db.TiposComida.AnyAsync(t => t.Nombre == normalizado && t.Id != exceptoId, ct),
            Catalogo.Actividad => await db.TiposActividad.AnyAsync(t => t.Nombre == normalizado && t.Id != exceptoId, ct),
            Catalogo.Categoria => await db.CategoriasAlimento.AnyAsync(
                t => t.UsuarioId == db.UsuarioIdActual && t.Nombre == normalizado && t.Id != exceptoId, ct),
            Catalogo.Musculo => await db.GruposMusculares.AnyAsync(t => t.Nombre == normalizado && t.Id != exceptoId, ct),
            _ => false
        };
    }

    private async Task<Dictionary<int, int>> ContarUsosAsync(Catalogo catalogo, CancellationToken ct)
        => catalogo switch
        {
            Catalogo.Comida => await db.Comidas.GroupBy(c => c.TipoComidaId).ToDictionaryAsync(g => g.Key, g => g.Count(), ct),
            Catalogo.Actividad => await db.Actividades.GroupBy(a => a.TipoActividadId).ToDictionaryAsync(g => g.Key, g => g.Count(), ct),
            Catalogo.Categoria => await db.Alimentos.Where(a => a.CategoriaAlimentoId != null)
                .GroupBy(a => a.CategoriaAlimentoId!.Value).ToDictionaryAsync(g => g.Key, g => g.Count(), ct),
            Catalogo.Musculo => await db.Ejercicios.Where(e => e.GrupoMuscularId != null)
                .GroupBy(e => e.GrupoMuscularId!.Value).ToDictionaryAsync(g => g.Key, g => g.Count(), ct),
            _ => []
        };

    private async Task<int> ContarUsosDeAsync(Catalogo catalogo, int id, CancellationToken ct)
        => catalogo switch
        {
            Catalogo.Comida => await db.Comidas.CountAsync(c => c.TipoComidaId == id, ct),
            Catalogo.Actividad => await db.Actividades.CountAsync(a => a.TipoActividadId == id, ct),
            Catalogo.Categoria => await db.Alimentos.CountAsync(a => a.CategoriaAlimentoId == id, ct),
            Catalogo.Musculo => await db.Ejercicios.CountAsync(e => e.GrupoMuscularId == id, ct),
            _ => 0
        };
}
