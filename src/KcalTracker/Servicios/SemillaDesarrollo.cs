using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Datos.Plantillas;
using KcalTracker.Dominio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

/// <summary>
/// Cuenta de trabajo local: importa la plantilla del Excel y aplica el perfil
/// calibrado (Omron 1.724 × 1,2 ≈ 2.070 de GET cotidiano, proteína 180 g).
/// Solo corre en Development. No toca una base que ya tenga otra cuenta.
/// </summary>
public static class SemillaDesarrollo
{
    public const string Email = "demo@localhost";
    public const string Clave = "password";

    public static async Task AsegurarAsync(IServiceProvider servicios)
    {
        var opciones = servicios.GetRequiredService<DbContextOptions<KcalDbContext>>();
        var hasher = servicios.GetRequiredService<IPasswordHasher<Usuario>>();
        var catalogo = servicios.GetService<CatalogoCompartido>() ?? new CatalogoCompartido();

        await using var db0 = new KcalDbContext(opciones, new UsuarioActualFijo(null), catalogo);
        var usuario = await db0.Usuarios.FirstOrDefaultAsync(u => u.Email == Email);
        var esAlta = usuario is null;
        if (usuario is null)
        {
            if (await db0.Usuarios.AnyAsync())
                return;

            usuario = new Usuario
            {
                Email = Email,
                Nombre = "Demo",
                CreadoEl = DateTime.UtcNow,
                BienvenidaPendiente = false
            };
            usuario.HashClave = hasher.HashPassword(usuario, Clave);
            db0.Usuarios.Add(usuario);
            await db0.SaveChangesAsync();
            db0.Configuraciones.Add(new Configuracion { UsuarioId = usuario.Id });
            db0.Perfiles.Add(PerfilBase(usuario.Id));
            await db0.SaveChangesAsync();
        }

        await using var db = new KcalDbContext(opciones, new UsuarioActualFijo(usuario.Id), catalogo);
        var plantillas = new ServicioPlantillas(db);
        await plantillas.ImportarAsync(PlantillaExcel.Id, Enum.GetValues<PartePlantilla>());
        await plantillas.SincronizarValoresAsync();

        // El perfil y el cálculo son del usuario: si los pisa cada arranque,
        // Ajustes parece no guardar (plazo, proteína, GER, constantes).
        if (esAlta)
        {
            var cuenta = await db.Usuarios.FirstAsync(u => u.Id == usuario.Id);
            cuenta.BienvenidaPendiente = false;
            cuenta.Nombre = "Demo";
            AplicarPerfilDemo(await db.Perfiles.SingleAsync());

            foreach (var m in await db.Mediciones.ToListAsync())
            {
                m.EsReferencia = m.Fecha == new DateOnly(2026, 8, 16);
                var fila = PlantillaExcel.Mediciones.FirstOrDefault(x => x.Fecha == m.Fecha);
                if (fila?.Nota is { } nota)
                    m.Nota = nota;
            }

            var cfg = await db.Configuraciones.SingleAsync();
            var referencia = await ServicioMediciones.ObtenerReferenciaAsync(db);
            AplicarCfgDemo(cfg, referencia?.GrasaPct);
        }

        await db.SaveChangesAsync();
    }

    private static Perfil PerfilBase(int usuarioId)
    {
        var perfil = new Perfil { UsuarioId = usuarioId, FuenteAlimentos = FuenteAlimentos.Propios };
        AplicarPerfilDemo(perfil);
        return perfil;
    }

    private static void AplicarPerfilDemo(Perfil perfil)
    {
        perfil.AlturaCm = 172;
        perfil.FechaNacimiento = new DateOnly(2001, 1, 1);
        perfil.Sexo = Sexo.Masculino;
        perfil.GrasaObjetivoPct = 15;
        perfil.PlazoSemanas = 11;
        perfil.FechaInicioObjetivo = new DateOnly(2026, 8, 16);
        perfil.ProteinaObjetivoG = 180;
        perfil.FactorActividad = 1.2;
    }

    private static void AplicarCfgDemo(Configuracion cfg, double? grasaPct)
    {
        cfg.MetodoBmr = MetodoBmr.Medido;
        cfg.BmrMedido = 1724;
        var s = ConstantesNutricion.Calcular(grasaPct);
        cfg.KcalPorKgGrasa = s.KcalPorKgGrasa;
        cfg.KcalPorKgMagro = s.KcalPorKgMagro;
        cfg.PctGastoMax = s.PctGastoMax;
        cfg.PctPesoSemanal = s.PctPesoSemanal;
        cfg.CobMinContraste = s.CobMinContraste;
        cfg.CobMinCalibrado = s.CobMinCalibrado;
    }
}
