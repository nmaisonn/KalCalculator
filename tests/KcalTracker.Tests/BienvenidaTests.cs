using KcalTracker.Calculo;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class BienvenidaTests
{
    [Fact]
    public async Task ArranqueYObjetivoQuedanGuardadosAntesDeImportarCatalogos()
    {
        using var baseDatos = new BaseDePrueba();
        await using (var dbAlta = baseDatos.Como(null))
        {
            var cuentas = new ServicioCuentas(dbAlta, new PasswordHasher<Usuario>());
            var (_, usuario) = await cuentas.RegistrarAsync("ana@test.com", "clave-segura", "Ana");
            Assert.True(usuario!.BienvenidaPendiente);
        }

        int ana;
        await using (var db = baseDatos.Como(null))
            ana = (await db.Usuarios.SingleAsync(u => u.Email == "ana@test.com")).Id;

        await using (var db = baseDatos.Como(ana))
        {
            var perfil = await db.Perfiles.SingleAsync();
            var cfg = await db.Configuraciones.SingleAsync();
            perfil.AlturaCm = 172;
            perfil.Sexo = Sexo.Masculino;
            perfil.FactorActividad = 1.2;
            perfil.GrasaObjetivoPct = 15;
            perfil.PlazoSemanas = 11;
            perfil.TipoObjetivo = TipoObjetivo.Recomposicion;
            cfg.MetodoBmr = MetodoBmr.Medido;
            cfg.BmrMedido = 1724;
            await db.SaveChangesAsync();

            var mediciones = new ServicioMediciones(db);
            var alta = await mediciones.GuardarAsync(new Medicion
            {
                Fecha = new DateOnly(2026, 8, 16),
                PesoKg = 74.1,
                GrasaPct = 18.8,
                Nota = "Arranque"
            });
            Assert.True(alta.Ok);
            var creada = await db.Mediciones.SingleAsync();
            await mediciones.MarcarReferenciaAsync(creada.Id);
        }

        await using (var db = baseDatos.Como(ana))
        {
            var usuario = await db.Usuarios.SingleAsync();
            Assert.True(usuario.BienvenidaPendiente);
            Assert.Empty(await db.TiposComida.ToListAsync());

            var perfil = await db.Perfiles.SingleAsync();
            Assert.Equal(172, perfil.AlturaCm);
            Assert.Equal(TipoObjetivo.Recomposicion, perfil.TipoObjetivo);
            Assert.Equal(11, perfil.PlazoSemanas);

            var referencia = await new ServicioMediciones(db).ReferenciaAsync();
            Assert.Equal(74.1, referencia!.Medicion.PesoKg);
            Assert.True(referencia.Medicion.EsReferencia);

            var gasto = Metabolismo.GastoSinDeporte(
                new ResultadoBmr(1724, MetodoBmr.Medido, MetodoBmr.Medido, false, null), 1.2);
            var plan = PlanObjetivo.Calcular(
                74.1, 18.8, 15, 11, ValoresDeFabrica.KcalPorKgGrasa, gasto!.Value, 0.25, 0.008)!;
            var deficit = DeficitObjetivo.De(plan, gasto, 18.8, TipoObjetivo.Recomposicion, null);
            Assert.Equal(352, Redondeo.Kcal(deficit.Corte));
            Assert.Equal(
                DeficitObjetivo.TopeRecompDe(gasto.Value, double.PositiveInfinity, deficit.Corte, 18.8),
                Redondeo.Kcal(deficit.Recomposicion));
        }
    }
}
