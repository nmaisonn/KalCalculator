using KcalTracker.Datos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

/// <summary>El usuario actual, sustituible entre consultas para simular dos cuentas.</summary>
public class UsuarioActualDePrueba : IUsuarioActual
{
    public int? UsuarioId { get; set; }
}

/// <summary>
/// Una base SQLite en memoria compartida por todos los contextos de la prueba. Es la misma
/// base física para las dos cuentas a propósito: si el aislamiento se apoyara en usar bases
/// distintas, la prueba no verificaría nada.
/// </summary>
public sealed class BaseDePrueba : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly DbContextOptions<KcalDbContext> _opciones;

    public BaseDePrueba()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();

        _opciones = new DbContextOptionsBuilder<KcalDbContext>()
            .UseSqlite(_conexion)
            .Options;

        using var db = new KcalDbContext(_opciones, new UsuarioActualDePrueba());
        db.Database.EnsureCreated();
    }

    public CatalogoCompartido Catalogo { get; } = new();

    /// <summary>Un contexto que ve el sistema como lo ve esa cuenta.</summary>
    public DbContextOptions<KcalDbContext> Opciones => _opciones;

    public KcalDbContext Como(int? usuarioId)
        => new(_opciones, new UsuarioActualDePrueba { UsuarioId = usuarioId }, Catalogo);

    public async Task SembrarCatalogoAsync()
    {
        await using var db = Como(null);
        await new KcalTracker.Servicios.SemillaCatalogoCompartido(db, Catalogo).AsegurarAsync();
    }

    public async Task<(int Ana, int Beto)> CrearAnaYBeto()
    {
        await using var db = Como(null);
        var ana = new Dominio.Usuario { Email = "ana@test.com", HashClave = "x", CreadoEl = DateTime.UtcNow };
        var beto = new Dominio.Usuario { Email = "beto@test.com", HashClave = "x", CreadoEl = DateTime.UtcNow };
        db.Usuarios.AddRange(ana, beto);
        await db.SaveChangesAsync();
        return (ana.Id, beto.Id);
    }

    public void Dispose()
    {
        _conexion.Dispose();
    }
}
