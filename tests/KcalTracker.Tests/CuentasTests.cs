using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class CuentasTests
{
    [Fact]
    public async Task RegistrarCreaLaCuentaConElEmailNormalizado()
    {
        using var baseDatos = new BaseDePrueba();
        await using var db = baseDatos.Como(null);
        var cuentas = new ServicioCuentas(db, new PasswordHasher<Usuario>());

        var (resultado, usuario) = await cuentas.RegistrarAsync("Ana@Test.COM", "clave-segura", "Ana");

        Assert.Equal(ResultadoRegistro.Creado, resultado);
        Assert.Equal("ana@test.com", usuario!.Email);
        Assert.Equal("Ana", usuario.Nombre);
        Assert.NotEqual("clave-segura", usuario.HashClave);

        await using var dbAna = baseDatos.Como(usuario.Id);
        var cfg = await dbAna.Configuraciones.SingleAsync();
        var perfil = await dbAna.Perfiles.SingleAsync();
        Assert.Equal(ValoresDeFabrica.KcalPorKgGrasa, cfg.KcalPorKgGrasa);
        Assert.Equal(ValoresDeFabrica.FactorActividad, perfil.FactorActividad);
        Assert.Equal(8, perfil.PlazoSemanas);
        Assert.Equal(FuenteAlimentos.Ambos, perfil.FuenteAlimentos);
    }

    [Fact]
    public async Task RegistrarRechazaUnEmailYaUsado()
    {
        using var baseDatos = new BaseDePrueba();
        await using var db = baseDatos.Como(null);
        var cuentas = new ServicioCuentas(db, new PasswordHasher<Usuario>());

        await cuentas.RegistrarAsync("ana@test.com", "clave-segura", null);
        var (resultado, usuario) = await cuentas.RegistrarAsync("ANA@test.com", "otra-clave", null);

        Assert.Equal(ResultadoRegistro.EmailYaUsado, resultado);
        Assert.Null(usuario);
    }

    [Fact]
    public async Task VerificarAceptaLaClaveCorrectaYRechazaLaIncorrecta()
    {
        using var baseDatos = new BaseDePrueba();
        await using var db = baseDatos.Como(null);
        var cuentas = new ServicioCuentas(db, new PasswordHasher<Usuario>());

        await cuentas.RegistrarAsync("ana@test.com", "clave-segura", null);

        Assert.NotNull(await cuentas.VerificarAsync("ana@test.com", "clave-segura"));
        Assert.Null(await cuentas.VerificarAsync("ana@test.com", "clave-incorrecta"));
        Assert.Null(await cuentas.VerificarAsync("nadie@test.com", "clave-segura"));
    }

    [Fact]
    public async Task CambiarClaveRechazaLaActualIncorrectaYAceptaLaBuena()
    {
        using var baseDatos = new BaseDePrueba();
        await using var db = baseDatos.Como(null);
        var cuentas = new ServicioCuentas(db, new PasswordHasher<Usuario>());
        var (_, usuario) = await cuentas.RegistrarAsync("ana@test.com", "clave-segura", null);

        Assert.Equal(
            ServicioCuentas.ResultadoCambioClave.ClaveActualIncorrecta,
            await cuentas.CambiarClaveAsync(usuario!.Id, "no-es", "nueva-clave"));

        Assert.Equal(
            ServicioCuentas.ResultadoCambioClave.Ok,
            await cuentas.CambiarClaveAsync(usuario.Id, "clave-segura", "nueva-clave"));

        Assert.Null(await cuentas.VerificarAsync("ana@test.com", "clave-segura"));
        Assert.NotNull(await cuentas.VerificarAsync("ana@test.com", "nueva-clave"));
    }

    [Fact]
    public async Task EliminarCuentaBorraSusDatosYNoLosDeOtra()
    {
        using var baseDatos = new BaseDePrueba();
        await using (var db = baseDatos.Como(null))
        {
            var cuentas = new ServicioCuentas(db, new PasswordHasher<Usuario>());
            await cuentas.RegistrarAsync("ana@test.com", "clave-segura", null);
            await cuentas.RegistrarAsync("beto@test.com", "clave-segura", null);
        }

        int ana, beto;
        await using (var db = baseDatos.Como(null))
        {
            ana = (await db.Usuarios.SingleAsync(u => u.Email == "ana@test.com")).Id;
            beto = (await db.Usuarios.SingleAsync(u => u.Email == "beto@test.com")).Id;
        }

        await using (var dbAna = baseDatos.Como(ana))
        {
            dbAna.Alimentos.Add(new Alimento { Nombre = "Pan de Ana", Porcion = "50 g", Kcal = 127 });
            await dbAna.SaveChangesAsync();
        }

        await using (var dbBeto = baseDatos.Como(beto))
        {
            dbBeto.Alimentos.Add(new Alimento { Nombre = "Pan de Beto", Porcion = "50 g", Kcal = 127 });
            await dbBeto.SaveChangesAsync();
        }

        await using (var db = baseDatos.Como(ana))
        {
            var cuentas = new ServicioCuentas(db, new PasswordHasher<Usuario>());
            await cuentas.EliminarCuentaAsync(ana);
        }

        await using (var db = baseDatos.Como(null))
        {
            Assert.Null(await db.Usuarios.FirstOrDefaultAsync(u => u.Id == ana));
            Assert.NotNull(await db.Usuarios.FirstOrDefaultAsync(u => u.Id == beto));
        }

        await using var dbBetoFinal = baseDatos.Como(beto);
        Assert.Equal(["Pan de Beto"], await dbBetoFinal.Alimentos.Select(a => a.Nombre).ToListAsync());
    }

    [Fact]
    public async Task CompletarFilasPorCuentaCreaConfiguracionYPerfilFaltantes()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();

        await using (var db = baseDatos.Como(null))
        {
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);
        }

        await using var dbAna = baseDatos.Como(ana);
        Assert.NotNull(await dbAna.Configuraciones.SingleAsync());
        Assert.NotNull(await dbAna.Perfiles.SingleAsync());
    }
}
