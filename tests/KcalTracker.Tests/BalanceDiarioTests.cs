using KcalTracker.Calculo;
using KcalTracker.Dominio;

namespace KcalTracker.Tests;

public class BalanceDiarioTests
{
    private const double Peso = 72.2;
    private const double GrasaPct = 17.2;
    private const double Factor = 1.2;

    private static double GastoBase()
        => Metabolismo.GastoSinDeporte(
            Metabolismo.Calcular(MetodoBmr.KatchMcArdle, null, Peso, GrasaPct, null, null, null),
            Factor)!.Value;

    [Fact]
    public void CA03_SesionDeGymDe330SeSumaUnaSolaVez()
    {
        var gasto = BalanceDiario.CalcularGasto(GastoBase(), kcalGym: [330]);

        Assert.Equal(2324, Redondeo.Kcal(gasto.Total));
        Assert.Equal(330, gasto.Gym);
        Assert.Equal(0, gasto.Actividades);
        Assert.False(gasto.PosibleDobleConteo);
    }

    [Fact]
    public void CA11_SinComidasElDeficitNoEstaDefinido()
    {
        var dia = BalanceDiario.DelDia(GastoBase(), comidas: []);

        Assert.False(dia.HayRegistro);
        Assert.Null(dia.Deficit);
        Assert.Null(dia.Ingesta);
        Assert.Equal(1994, Redondeo.Kcal(dia.Gasto.Total));
    }

    [Fact]
    public void RN03_ConComidaElDeficitEsGastoMenosIngesta()
    {
        var dia = BalanceDiario.DelDia(
            GastoBase(),
            comidas: [new ItemIngesta(1, 1500, 80, 120, 50)],
            kcalGym: [330]);

        Assert.True(dia.HayRegistro);
        Assert.Equal(2324 - 1500, Redondeo.Kcal(dia.Deficit!.Value));
    }

    [Fact]
    public void CA09_ItemSinMacrosSumaKcalYNoInventaProteina()
    {
        var conMacros = new ItemIngesta(1, 100, 20, 0, 2);
        var rudy = new ItemIngesta(1, 950, null, null, null);

        var soloRudy = BalanceDiario.SumarIngesta([rudy]);
        Assert.Equal(950, soloRudy.Kcal);
        Assert.Equal(950, soloRudy.KcalSinMacros);
        Assert.Null(soloRudy.Proteina);

        var mixto = BalanceDiario.SumarIngesta([conMacros, rudy]);
        Assert.Equal(1050, mixto.Kcal);
        Assert.Equal(950, mixto.KcalSinMacros);
        Assert.Equal(20, mixto.Proteina);
    }

    [Fact]
    public void CA38_GymYActividadDeCatalogoElMismoDiaAdviertenDobleConteo()
    {
        var gasto = BalanceDiario.CalcularGasto(
            GastoBase(),
            kcalActividades: [330],
            kcalGym: [330]);

        Assert.True(gasto.PosibleDobleConteo);
        Assert.Equal(Redondeo.Kcal(GastoBase() + 660), Redondeo.Kcal(gasto.Total));
    }

    [Fact]
    public void CardioSinActividadDeCatalogoNoEsDobleConteo()
    {
        var gasto = BalanceDiario.CalcularGasto(GastoBase(), kcalCardio: [180]);

        Assert.False(gasto.PosibleDobleConteo);
        Assert.Equal(180, gasto.Cardio);
        Assert.Equal(Redondeo.Kcal(GastoBase() + 180), Redondeo.Kcal(gasto.Total));
    }
}
