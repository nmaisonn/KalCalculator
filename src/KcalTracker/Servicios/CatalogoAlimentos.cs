using KcalTracker.Datos;
using KcalTracker.Datos.Plantillas;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

/// <summary>
/// El catálogo de la app vive en una cuenta interna de solo lectura. Cada usuario
/// elige si lo ve, si ve solo lo suyo, o ambos.
/// </summary>
public static class CatalogoAlimentos
{
    public static IQueryable<Alimento> Visibles(
        IQueryable<Alimento> alimentos,
        int usuarioId,
        int catalogoId,
        FuenteAlimentos fuente)
        => fuente switch
        {
            FuenteAlimentos.Catalogo when catalogoId != 0
                => alimentos.Where(a => a.UsuarioId == catalogoId),
            FuenteAlimentos.Ambos => alimentos,
            _ => alimentos.Where(a => a.UsuarioId == usuarioId)
        };

    public static IReadOnlyList<Alimento> SinDuplicarCatalogo(
        IEnumerable<Alimento> alimentos,
        int usuarioId)
    {
        var lista = alimentos.ToList();
        var propios = lista
            .Where(a => a.UsuarioId == usuarioId)
            .Select(a => Clave(a.Nombre, a.Porcion))
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        return lista
            .Where(a => a.UsuarioId == usuarioId || !propios.Contains(Clave(a.Nombre, a.Porcion)))
            .ToList();
    }

    public static async Task<IReadOnlyList<Alimento>> CargarAsync(
        IQueryable<Alimento> alimentos,
        int usuarioId,
        int catalogoId,
        FuenteAlimentos fuente,
        CancellationToken ct = default)
        => SinDuplicarCatalogo(
            await Visibles(alimentos, usuarioId, catalogoId, fuente).ToListAsync(ct),
            usuarioId);

    public static bool EsDeLaApp(Alimento alimento, int catalogoId)
        => catalogoId != 0 && alimento.UsuarioId == catalogoId;

    private static string Clave(string nombre, string porcion)
        => nombre.Trim() + "\n" + porcion.Trim();
}

public class SemillaCatalogoCompartido(KcalDbContext db, CatalogoCompartido ids)
{
    public async Task AsegurarAsync(CancellationToken ct = default)
    {
        var cuenta = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == CatalogoCompartido.Email, ct);
        if (cuenta is null)
        {
            cuenta = new Usuario
            {
                Email = CatalogoCompartido.Email,
                Nombre = "Catálogo de la app",
                HashClave = "x",
                CreadoEl = DateTime.UtcNow,
                BienvenidaPendiente = false
            };
            db.Usuarios.Add(cuenta);
            await db.SaveChangesAsync(ct);
        }

        ids.Id = cuenta.Id;

        if (!await db.Configuraciones.IgnoreQueryFilters().AnyAsync(c => c.UsuarioId == cuenta.Id, ct))
            db.Configuraciones.Add(new Configuracion { UsuarioId = cuenta.Id });
        if (!await db.Perfiles.IgnoreQueryFilters().AnyAsync(p => p.UsuarioId == cuenta.Id, ct))
            db.Perfiles.Add(new Perfil { UsuarioId = cuenta.Id, FuenteAlimentos = FuenteAlimentos.Catalogo });
        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);

        var categorias = await db.CategoriasAlimento.IgnoreQueryFilters()
            .Where(c => c.UsuarioId == cuenta.Id)
            .ToListAsync(ct);
        for (var i = 0; i < CatalogoApp.Categorias.Count; i++)
        {
            var nombre = CatalogoApp.Categorias[i];
            if (categorias.Any(c => string.Equals(c.Nombre, nombre, StringComparison.CurrentCultureIgnoreCase)))
                continue;
            var cat = new CategoriaAlimento
            {
                UsuarioId = cuenta.Id,
                Nombre = nombre,
                Orden = i + 1,
                Activo = true
            };
            db.CategoriasAlimento.Add(cat);
            categorias.Add(cat);
        }
        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);

        var existentes = await db.Alimentos.IgnoreQueryFilters()
            .Where(a => a.UsuarioId == cuenta.Id)
            .Select(a => a.Nombre + "\n" + a.Porcion)
            .ToListAsync(ct);
        var claves = existentes.ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        foreach (var a in CatalogoApp.Alimentos)
        {
            var clave = a.Nombre.Trim() + "\n" + a.Porcion.Trim();
            if (claves.Contains(clave)) continue;

            db.Alimentos.Add(new Alimento
            {
                UsuarioId = cuenta.Id,
                Nombre = a.Nombre,
                Porcion = a.Porcion,
                Kcal = a.Kcal,
                Proteina = a.Proteina,
                Carbos = a.Carbos,
                Grasa = a.Grasa,
                CategoriaAlimentoId = categorias
                    .FirstOrDefault(c => string.Equals(c.Nombre, a.Categoria, StringComparison.CurrentCultureIgnoreCase))
                    ?.Id,
                EsAfuera = a.EsAfuera,
                Origen = OrigenDato.Plantilla,
                Activo = true
            });
            claves.Add(clave);
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }
}
