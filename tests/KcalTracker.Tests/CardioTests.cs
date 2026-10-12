using KcalTracker.Calculo;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class CardioTests
{
    [Fact]
    public async Task UnaCaminataSeGuardaComoUnTramoYSeMarcaEnHoy()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        var cardio = new ServicioCardio(dbAna);
        await cardio.GuardarProtocoloAsync(
            new ProtocoloCardio { Nombre = "Caminata 40 min", Kcal = 220, Activo = true },
            [new BloqueCardio { Minutos = 40, Repeticiones = 1 }]);

        var proto = await dbAna.ProtocolosCardio.Include(p => p.Bloques)
            .SingleAsync(p => p.Nombre == "Caminata 40 min");
        Assert.Single(proto.Bloques);
        Assert.Equal(40, DuracionProtocolo.Minutos(proto.Bloques));
        Assert.Equal(220, proto.Kcal);

        var fecha = new DateOnly(2026, 9, 23);
        var dia = new ServicioDia(dbAna);
        await dia.AlternarProtocoloAsync(fecha, proto.Id);
        var vista = await dia.CargarAsync(fecha);

        Assert.Contains(vista.Protocolos, p => p.Id == proto.Id);
        Assert.Contains(vista.CardioDelDia, id => id == proto.Id);
        var sesion = Assert.Single(await dbAna.SesionesCardio.Where(s => s.Fecha == fecha).ToListAsync());
        Assert.Equal(40, sesion.Minutos);
        Assert.Equal(220, sesion.Kcal);
    }

    [Fact]
    public async Task DesactivarCardioLoSacaDelCatalogoYDeHoy()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        var cardio = new ServicioCardio(dbAna);
        await cardio.GuardarProtocoloAsync(
            new ProtocoloCardio { Nombre = "Caminata suave", Activo = true },
            [new BloqueCardio { Minutos = 30, Velocidad = 5, Inclinacion = 0, Repeticiones = 1 }]);
        var proto = await dbAna.ProtocolosCardio.SingleAsync(p => p.Nombre == "Caminata suave");

        var fecha = new DateOnly(2026, 9, 23);
        var dia = new ServicioDia(dbAna);
        await dia.AlternarProtocoloAsync(fecha, proto.Id);
        await cardio.DesactivarProtocoloAsync(proto.Id);

        var vista = await dia.CargarAsync(fecha);
        Assert.DoesNotContain(vista.Protocolos, p => p.Id == proto.Id);
        Assert.False(await dbAna.ProtocolosCardio.AnyAsync(p => p.Id == proto.Id && p.Activo));
        Assert.Contains(vista.CardioDelDia, id => id == proto.Id);
    }
}
