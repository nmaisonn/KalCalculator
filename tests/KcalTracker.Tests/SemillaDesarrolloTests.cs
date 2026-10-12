using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KcalTracker.Tests;

public class SemillaDesarrolloTests
{
    [Fact]
    public async Task ElSegundoArranqueNoPisaElPlazoQueGuardoElUsuario()
    {
        using var baseDatos = new BaseDePrueba();
        await using var servicios = Armar(baseDatos);

        await SemillaDesarrollo.AsegurarAsync(servicios);

        int id;
        await using (var db = baseDatos.Como(null))
            id = (await db.Usuarios.SingleAsync(u => u.Email == SemillaDesarrollo.Email)).Id;

        await using (var db = baseDatos.Como(id))
        {
            var perfil = await db.Perfiles.SingleAsync();
            Assert.Equal(11, perfil.PlazoSemanas);
            perfil.PlazoSemanas = 8;
            await db.SaveChangesAsync();
        }

        await SemillaDesarrollo.AsegurarAsync(servicios);

        await using (var db = baseDatos.Como(id))
        {
            var perfil = await db.Perfiles.SingleAsync();
            Assert.Equal(8, perfil.PlazoSemanas);
        }
    }

    [Fact]
    public async Task SiYaHayOtraCuentaNoCreaLaDemo()
    {
        using var baseDatos = new BaseDePrueba();
        await using var servicios = Armar(baseDatos);

        await using (var db = baseDatos.Como(null))
        {
            db.Usuarios.Add(new Usuario
            {
                Email = "ya@existe.local",
                Nombre = "Existente",
                HashClave = "x",
                CreadoEl = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await SemillaDesarrollo.AsegurarAsync(servicios);

        await using (var db = baseDatos.Como(null))
        {
            Assert.Equal(1, await db.Usuarios.CountAsync());
            Assert.Null(await db.Usuarios.SingleOrDefaultAsync(u => u.Email == SemillaDesarrollo.Email));
        }
    }

    private static ServiceProvider Armar(BaseDePrueba baseDatos)
    {
        var servicios = new ServiceCollection();
        servicios.AddSingleton(baseDatos.Opciones);
        servicios.AddSingleton(baseDatos.Catalogo);
        servicios.AddSingleton<IPasswordHasher<Usuario>>(new PasswordHasher<Usuario>());
        return servicios.BuildServiceProvider();
    }
}
