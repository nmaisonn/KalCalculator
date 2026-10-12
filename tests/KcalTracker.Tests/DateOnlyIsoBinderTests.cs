using KcalTracker;

namespace KcalTracker.Tests;

public class DateOnlyIsoBinderTests
{
    [Theory]
    [InlineData("2026-09-17", 2026, 9, 17)]
    [InlineData("17/09/2026", 2026, 9, 17)]
    [InlineData("18/09/2026", 2026, 9, 18)]
    [InlineData("8/9/2026", 2026, 9, 8)]
    public void AceptaIsoYFormatoUruguayo(string crudo, int y, int m, int d)
    {
        Assert.True(DateOnlyIsoBinder.TryParse(crudo, out var fecha));
        Assert.Equal(new DateOnly(y, m, d), fecha);
    }
}

public class DoubleIsoBinderTests
{
    [Theory]
    [InlineData("180.5", 180.5)]
    [InlineData("180,5", 180.5)]
    [InlineData("14.5", 14.5)]
    [InlineData("0.75", 0.75)]
    public void AceptaPuntoYComa(string crudo, double esperado)
    {
        Assert.True(DoubleIsoBinder.TryParse(crudo, out var valor));
        Assert.Equal(esperado, valor);
    }
}
