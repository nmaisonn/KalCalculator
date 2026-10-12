using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KcalTracker.Datos;

/// <summary>
/// Solo para `dotnet ef`. En ejecución el contexto se construye con el usuario de la
/// petición; acá no hay petición, y el filtro global queda en 0 (no coincide con nadie).
/// </summary>
public class KcalDbContextFactory : IDesignTimeDbContextFactory<KcalDbContext>
{
    public KcalDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<KcalDbContext>()
            .UseSqlite(RutaSqlite.Cadena())
            .Options;

        return new KcalDbContext(opciones, new UsuarioActualFijo(null));
    }
}

public sealed class UsuarioActualFijo(int? id) : IUsuarioActual
{
    public int? UsuarioId { get; } = id;
}
