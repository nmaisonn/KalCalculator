using KcalTracker.Calculo;
using KcalTracker.Datos.Plantillas;
using KcalTracker.Dominio;

namespace KcalTracker.Tests;

public class AvanceComposicionTests
{
    private static readonly DateOnly Inicio = new(2026, 8, 16);
    private static readonly DateOnly Hoy = new(2026, 9, 28);

    [Fact]
    public void ElPlazoArrancaEnLaFechaDeObjetivoYDejaAfueraLasPesadasViejas()
    {
        var avance = AvanceComposicion.De(DePlantilla(), Inicio, Hoy, 15);

        Assert.Equal(4, avance.Pesadas);
        Assert.Equal(Inicio, avance.Primera);
        Assert.Equal(new DateOnly(2026, 9, 15), avance.Ultima);
        Assert.Equal(74.1, avance.Peso!.Desde);
        Assert.Equal(72.2, avance.Peso.Hasta);
        Assert.InRange(avance.Peso.Delta, -1.91, -1.89);
        Assert.Equal(18.8, avance.GrasaPct!.Desde);
        Assert.Equal(17.2, avance.GrasaPct.Hasta);
        Assert.Contains("4 pesadas", avance.Lectura);
        Assert.Contains("74,1", avance.Lectura);
        Assert.Contains("72,2", avance.Lectura);
        Assert.Equal(6, avance.Graficos.Count);
        Assert.All(avance.Graficos, g => Assert.NotNull(g.Grafico));
        Assert.Equal(4, avance.Graficos[0].Grafico!.Puntos.Count);
        Assert.True(avance.Graficos[0].Grafico!.Puntos[0].Y < avance.Graficos[0].Grafico!.Puntos[^1].Y);
        Assert.NotNull(avance.Graficos.Single(g => g.Clase == "grasa").Grafico!.ObjetivoY);
        Assert.Contains("15 %", avance.Graficos.Single(g => g.Clase == "grasa").LeyendaObjetivo);
        var grasaKg = avance.Graficos.Single(g => g.Clase == "grasa-kg");
        var musculoKg = avance.Graficos.Single(g => g.Clase == "musculo-kg");
        Assert.Equal("kg2", grasaKg.TipoEje);
        Assert.Equal(Redondeo.Kg(Composicion.GrasaKg(74.1, 18.8)!.Value), Redondeo.Kg(grasaKg.Grafico!.Puntos[0].Valor));
        Assert.Equal(Redondeo.Kg(Composicion.MusculoKg(74.1, 41.2)!.Value), Redondeo.Kg(musculoKg.Grafico!.Puntos[0].Valor));
        Assert.Contains("kg", grasaKg.Lectura);
        Assert.DoesNotContain("kg kg", avance.Graficos[0].Lectura);
        Assert.DoesNotContain("% pp", avance.Graficos.Single(g => g.Clase == "grasa").Lectura);
        Assert.Contains("pp", avance.Graficos.Single(g => g.Clase == "grasa").Lectura);
        Assert.EndsWith("Z", avance.Graficos[0].Grafico!.PathArea);
    }

    [Fact]
    public void UnaPesadaFueraDeLaBasculeSaleDeLaGrafica()
    {
        var lista = DePlantilla();
        lista.Single(m => m.Fecha == new DateOnly(2026, 9, 15)).EsComparable = false;

        var avance = AvanceComposicion.De(lista, Inicio, Hoy, 15);

        Assert.Equal(3, avance.Pesadas);
        Assert.Equal(72.9, avance.Peso!.Hasta);
        Assert.Equal(18.4, avance.GrasaPct!.Hasta);
        Assert.DoesNotContain(avance.Graficos[0].Grafico!.Puntos, p => p.Fecha == new DateOnly(2026, 9, 15));
        Assert.Equal(new DateOnly(2026, 9, 6), avance.Ultima);
    }

    [Fact]
    public void AlAgregarUnLunesComparableSeAlargaLaSerie()
    {
        var lista = DePlantilla();
        lista.Add(new Medicion
        {
            Fecha = new DateOnly(2026, 9, 21),
            PesoKg = 71.8,
            GrasaPct = 16.9,
            MusculoPct = 42.4,
            Visceral = 6,
            EsComparable = true
        });

        var con = AvanceComposicion.De(lista, Inicio, Hoy, 15);
        Assert.Equal(5, con.Pesadas);
        Assert.Equal(71.8, con.Peso!.Hasta);
        Assert.Equal(16.9, con.GrasaPct!.Hasta);
        Assert.Equal(5, con.Graficos[0].Grafico!.Puntos.Count);

        lista[^1].EsComparable = false;
        var sin = AvanceComposicion.De(lista, Inicio, Hoy, 15);
        Assert.Equal(4, sin.Pesadas);
        Assert.Equal(72.2, sin.Peso!.Hasta);
    }

    [Fact]
    public void ElHistorialCompletoIncluyeLasPesadasAnterioresAlPlazo()
    {
        var hist = AvanceComposicion.De(DePlantilla(), null, Hoy, 15);
        Assert.Equal(7, hist.Pesadas);
        Assert.Equal(new DateOnly(2022, 11, 4), hist.Primera);
        Assert.Contains("todo el historial", hist.Lectura, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LasGraficasArrancanEnLaMedicionDeReferencia()
    {
        var lista = DePlantilla();
        lista.Single(m => m.Fecha == Inicio).EsReferencia = true;
        var ancla = lista.Single(m => m.EsReferencia);

        var hist = AvanceComposicion.De(lista, ancla.Fecha, Hoy, 15, ancla);

        Assert.Equal(4, hist.Pesadas);
        Assert.Equal(Inicio, hist.Primera);
        Assert.Equal(74.1, hist.Peso!.Desde);
        Assert.DoesNotContain(hist.Graficos[0].Grafico!.Puntos, p => p.Fecha < Inicio);
        Assert.Contains("referencia", hist.Lectura, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("16/8/2026", hist.Lectura);
    }

    [Fact]
    public void LaReferenciaEntraAunqueNoEsteContraLaBascule()
    {
        var lista = DePlantilla();
        var ancla = lista.Single(m => m.Fecha == Inicio);
        ancla.EsReferencia = true;
        ancla.EsComparable = false;

        var hist = AvanceComposicion.De(lista, ancla.Fecha, Hoy, 15, ancla);

        Assert.Equal(Inicio, hist.Primera);
        Assert.Equal(74.1, hist.Peso!.Desde);
        Assert.Contains(hist.Graficos[0].Grafico!.Puntos, p => p.Fecha == Inicio);
    }

    [Fact]
    public void ConUnaSolaPesadaNoHayPendiente()
    {
        var lista = DePlantilla().Where(m => m.Fecha == Inicio).ToList();
        var avance = AvanceComposicion.De(lista, Inicio, Hoy, 15);

        Assert.Equal(1, avance.Pesadas);
        Assert.Single(avance.Graficos);
        Assert.Null(avance.Graficos[0].Grafico);
        Assert.Contains("una sola pesada", avance.Lectura, StringComparison.OrdinalIgnoreCase);
        Assert.Null(GraficoSerie.De([(Inicio, 74.1)]));
    }

    [Fact]
    public void SinComparablesEnElPlazoNoHayGraficos()
    {
        var lista = DePlantilla();
        foreach (var m in lista)
            m.EsComparable = false;

        var avance = AvanceComposicion.De(lista, Inicio, Hoy, 15);
        Assert.Equal(0, avance.Pesadas);
        Assert.Empty(avance.Graficos);
        Assert.Contains("No hay pesadas marcadas", avance.Lectura);
    }

    private static List<Medicion> DePlantilla()
        => PlantillaExcel.Mediciones.Select(p => new Medicion
        {
            Fecha = p.Fecha,
            PesoKg = p.PesoKg,
            GrasaPct = p.GrasaPct,
            MusculoPct = p.MusculoPct,
            Visceral = p.Visceral,
            EsComparable = true
        }).ToList();
}
