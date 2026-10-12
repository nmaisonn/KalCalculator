using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

public enum ResultadoRegistro
{
    Creado,
    EmailYaUsado
}

/// <summary>
/// Alta e ingreso de cuentas. Usa el hasher de ASP.NET Core Identity, que implementa
/// PBKDF2 con sal por contraseña, sin traer el resto del andamiaje de Identity (roles,
/// claims, tokens, doble factor) que está fuera de alcance.
/// </summary>
public class ServicioCuentas(KcalDbContext db, IPasswordHasher<Usuario> hasher)
{
    public async Task<(ResultadoRegistro Resultado, Usuario? Usuario)> RegistrarAsync(
        string email, string clave, string? nombre, CancellationToken ct = default)
    {
        var normalizado = Normalizar(email);

        // Los usuarios no están sujetos al filtro global, así que esta consulta ve todas
        // las cuentas: es lo correcto acá, porque el email tiene que ser único en el sistema.
        if (normalizado == CatalogoCompartido.Email
            || await db.Usuarios.AnyAsync(u => u.Email == normalizado, ct))
        {
            return (ResultadoRegistro.EmailYaUsado, null);
        }

        var usuario = new Usuario
        {
            Email = normalizado,
            Nombre = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim(),
            CreadoEl = DateTime.UtcNow,
            BienvenidaPendiente = true
        };
        usuario.HashClave = hasher.HashPassword(usuario, clave);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);

        db.Configuraciones.Add(new Configuracion { UsuarioId = usuario.Id });
        db.Perfiles.Add(new Perfil { UsuarioId = usuario.Id, FuenteAlimentos = FuenteAlimentos.Ambos });
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return (ResultadoRegistro.Creado, usuario);
    }

    /// <summary>Devuelve la cuenta si el correo y la contraseña coinciden, o null.</summary>
    public async Task<Usuario?> VerificarAsync(string email, string clave, CancellationToken ct = default)
    {
        var normalizado = Normalizar(email);
        if (normalizado == CatalogoCompartido.Email) return null;

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == normalizado, ct);

        if (usuario is null) return null;

        var resultado = hasher.VerifyHashedPassword(usuario, usuario.HashClave, clave);

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.HashClave = hasher.HashPassword(usuario, clave);
            await db.SaveChangesAsync(ct);
            return usuario;
        }

        return resultado == PasswordVerificationResult.Success ? usuario : null;
    }

    public enum ResultadoCambioClave
    {
        Ok,
        ClaveActualIncorrecta
    }

    public async Task<ResultadoCambioClave> CambiarClaveAsync(
        int usuarioId, string actual, string nueva, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstAsync(u => u.Id == usuarioId, ct);
        if (hasher.VerifyHashedPassword(usuario, usuario.HashClave, actual) == PasswordVerificationResult.Failed)
            return ResultadoCambioClave.ClaveActualIncorrecta;

        usuario.HashClave = hasher.HashPassword(usuario, nueva);
        await db.SaveChangesAsync(ct);
        return ResultadoCambioClave.Ok;
    }

    public async Task EliminarCuentaAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstAsync(u => u.Id == usuarioId, ct);
        if (usuario.Email == CatalogoCompartido.Email)
            throw new InvalidOperationException("El catálogo de la app no se borra.");
        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync(ct);
    }

    private static string Normalizar(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Cuentas creadas antes de existir Configuracion y Perfil quedan incompletas.
    /// Se llama al arrancar, con el filtro de usuario ignorado a propósito.
    /// </summary>
    public static async Task CompletarFilasPorCuentaAsync(KcalDbContext db, CancellationToken ct = default)
    {
        var usuarios = await db.Usuarios.Select(u => u.Id).ToListAsync(ct);
        var conCfg = await db.Configuraciones.IgnoreQueryFilters()
            .Select(c => c.UsuarioId).ToListAsync(ct);
        var conPerfil = await db.Perfiles.IgnoreQueryFilters()
            .Select(p => p.UsuarioId).ToListAsync(ct);

        foreach (var id in usuarios.Except(conCfg))
            db.Configuraciones.Add(new Configuracion { UsuarioId = id });

        foreach (var id in usuarios.Except(conPerfil))
            db.Perfiles.Add(new Perfil { UsuarioId = id });

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }
}
