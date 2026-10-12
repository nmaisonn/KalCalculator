using KcalTracker.Calculo;
using KcalTracker.Datos.Plantillas;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Tests;

public class MedicionesTests
{
    [Fact]
    public async Task CA19_EliminarLaUltimaMedicionRecalculaElBmrSobreLaAnterior()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id, [PartePlantilla.Mediciones]);
        var servicio = new ServicioMediciones(dbAna);

        var antes = await servicio.ReferenciaAsync();
        Assert.Equal(new DateOnly(2026, 9, 15), antes!.Medicion.Fecha);
        Assert.Equal(1661, antes.BmrKcal);

        var aBorrar = await dbAna.Mediciones.SingleAsync(m => m.Fecha == new DateOnly(2026, 9, 15));
        var resultado = await servicio.EliminarAsync(aBorrar.Id);

        Assert.True(resultado.Ok);
        Assert.Contains("2026-09-06", resultado.Aviso);
        Assert.Contains("1655", resultado.Aviso);
        Assert.Contains("1661", resultado.Aviso);

        var despues = await servicio.ReferenciaAsync();
        Assert.Equal(new DateOnly(2026, 9, 6), despues!.Medicion.Fecha);
        Assert.Equal(1655, despues.BmrKcal);
        Assert.Equal(13.16, Redondeo.Kg(Composicion.GrasaKg(73.1, 18)!.Value));
    }

    [Fact]
    public async Task GrasaYMagraSeDerivanAlLeer()
    {
        var grasa = Composicion.GrasaKg(72.2, 17.2);
        var magra = Composicion.MagraKg(72.2, 17.2);
        var musculo = Composicion.MusculoKg(72.2, 42.2);

        Assert.Equal(12.42, Redondeo.Kg(grasa!.Value));
        Assert.Equal(59.78, Redondeo.Kg(magra!.Value));
        Assert.Equal(30.47, Redondeo.Kg(musculo!.Value));
    }

    [Fact]
    public async Task LaMedicionMarcadaMandaAunqueHayaUnaMasNueva()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        await new ServicioPlantillas(dbAna).ImportarAsync(PlantillaExcel.Id, [PartePlantilla.Mediciones]);
        var servicio = new ServicioMediciones(dbAna);
        var agosto = await dbAna.Mediciones.SingleAsync(m => m.Fecha == new DateOnly(2026, 8, 16));
        await servicio.MarcarReferenciaAsync(agosto.Id);

        var referencia = await servicio.ReferenciaAsync();
        Assert.Equal(new DateOnly(2026, 8, 16), referencia!.Medicion.Fecha);
        Assert.Equal(74.1, referencia.Medicion.PesoKg);
    }

    [Fact]
    public async Task UnaPesadaNuevaEntraEnLaBasculeSalvoQueSeDesmarque()
    {
        using var baseDatos = new BaseDePrueba();
        var (ana, _) = await baseDatos.CrearAnaYBeto();
        await using (var db = baseDatos.Como(null))
            await ServicioCuentas.CompletarFilasPorCuentaAsync(db);

        await using var dbAna = baseDatos.Como(ana);
        var servicio = new ServicioMediciones(dbAna);
        var alta = await servicio.GuardarAsync(new Medicion
        {
            Fecha = new DateOnly(2026, 9, 21),
            PesoKg = 73,
            GrasaPct = 18
        });
        Assert.True(alta.Ok);

        var creada = await dbAna.Mediciones.SingleAsync();
        Assert.True(creada.EsComparable);

        await servicio.MarcarComparableAsync(creada.Id, false);
        Assert.False((await dbAna.Mediciones.SingleAsync()).EsComparable);
    }
}
