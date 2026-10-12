using KcalTracker.Calculo;

namespace KcalTracker.Tests;

public class ParserPorcionTests
{
    [Theory]
    [InlineData("pure 250g", "pure", 250)]
    [InlineData("puré 250 g", "puré", 250)]
    [InlineData("puré de papa, 250gr", "puré de papa", 250)]
    [InlineData("250 g de puré", "puré", 250)]
    [InlineData("1,5 kg de pollo", "pollo", 1500)]
    public void ExtraeNombreYGramos(string texto, string nombre, double gramos)
    {
        var c = ParserPorcion.Parse(texto);
        Assert.Equal(nombre, c.Nombre, ignoreCase: true);
        Assert.Equal(gramos, c.Gramos);
    }

    [Fact]
    public void SinCantidadDejaElNombreEntero()
    {
        var c = ParserPorcion.Parse("manzana");
        Assert.Equal("manzana", c.Nombre);
        Assert.Null(c.Gramos);
    }

    [Fact]
    public void EscalaDe100gA250g()
    {
        var (kcal, p, c, g) = EscaladoNutricion.AGramos(80, 2, 14, 1, 100, 250);
        Assert.Equal(200, kcal);
        Assert.Equal(5, p);
        Assert.Equal(35, c);
        Assert.Equal(2.5, g);
    }
}
