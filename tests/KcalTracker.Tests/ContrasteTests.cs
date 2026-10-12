using KcalTracker.Calculo;
using KcalTracker.Dominio;

namespace KcalTracker.Tests;

public class ContrasteTests
{
    [Fact]
    public void ElParVaContraLaUltimaPesadaComparable()
    {
        var meds = new[]
        {
            M(1, 6, 73.1, 18),
            M(2, 15, 72.2, 17.2),
            M(3, 20, 73.8, 18.9, comparable: false)
        };

        var par = ContrasteComposicion.Par(meds, new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 21));

        Assert.NotNull(par);
        Assert.Equal(new DateOnly(2026, 9, 6), par.Value.Inicial.Fecha);
        Assert.Equal(new DateOnly(2026, 9, 15), par.Value.Ultima.Fecha);
        Assert.Equal(0.74, Redondeo.Kg(ContrasteComposicion.GrasaPerdidaKg(par.Value.Inicial, par.Value.Ultima)!.Value));
    }

    [Fact]
    public void SiLaUnicaDelPeriodoEsComparableUsaLaAnterior()
    {
        var meds = new[]
        {
            M(1, 6, 73.1, 18),
            M(2, 18, 72.0, 17.0)
        };

        var par = ContrasteComposicion.Par(meds, new DateOnly(2026, 9, 15), new DateOnly(2026, 9, 21));

        Assert.NotNull(par);
        Assert.Equal(new DateOnly(2026, 9, 6), par.Value.Inicial.Fecha);
        Assert.Equal(new DateOnly(2026, 9, 18), par.Value.Ultima.Fecha);
    }

    [Fact]
    public void EnElPeriodoParteDeLaPrimeraComparableNoDeUnaAnterior()
    {
        var meds = new[]
        {
            M(1, 6, 73.1, 18),
            M(2, 15, 72.2, 17.2),
            M(3, 21, 71.8, 16.8)
        };
        meds[0].Fecha = new DateOnly(2026, 8, 19);

        var par = ContrasteComposicion.Par(meds, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 21));

        Assert.NotNull(par);
        Assert.Equal(new DateOnly(2026, 9, 15), par.Value.Inicial.Fecha);
        Assert.Equal(new DateOnly(2026, 9, 21), par.Value.Ultima.Fecha);
    }

    [Fact]
    public void SinDosComparablesNoHayPar()
    {
        var meds = new[]
        {
            M(1, 15, 72.2, 17.2),
            M(2, 20, 73.8, 18.9, comparable: false)
        };

        var par = ContrasteComposicion.Par(meds, new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 21));
        Assert.Null(par);
    }

    [Fact]
    public void LaGrasaEstimadaCortaEnLaUltimaPesada()
    {
        var semana = new[]
        {
            Dia(14, 770),
            Dia(15, 770),
            Dia(16, 770),
            Dia(17, 770)
        };
        var ventana = semana.Take(2).ToList();
        var m = Periodo.Calcular(semana, 500, 7700, 0.5, 0.20, serieContraste: ventana);

        Assert.Equal(0.20, m.GrasaEstimadaKg, 3);
        Assert.Equal(new DateOnly(2026, 9, 14), m.ContrasteDesde);
        Assert.Equal(new DateOnly(2026, 9, 15), m.ContrasteHasta);
        Assert.True(m.MuestraContraste);
    }

    private static Medicion M(int id, int dia, double peso, double grasa, bool comparable = true)
        => new()
        {
            Id = id,
            Fecha = new DateOnly(2026, 9, dia),
            PesoKg = peso,
            GrasaPct = grasa,
            EsComparable = comparable
        };

    private static DiaDePeriodo Dia(int dia, double deficit)
        => new(
            new DateOnly(2026, 9, dia), true, 1800, 120, 180, 50, 0, 2500, deficit, 0, 0, 0);
}
