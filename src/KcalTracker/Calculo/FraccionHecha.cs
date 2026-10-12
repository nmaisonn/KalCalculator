namespace KcalTracker.Calculo;

/// <summary>
/// 1 = completo. 0,5 = la mitad de kcal (entreno o comida).
/// El % se convierte a factor: 50 → 0,5.
/// </summary>
public static class FraccionHecha
{
    public const double Min = 0.1;
    public const double Max = 20;

    public static double? De(double cantidad, double porcentaje)
    {
        var v = porcentaje > 0 ? porcentaje / 100.0 : cantidad;
        if (v is < Min or > Max) return null;
        return Math.Round(v, 4);
    }

    public static bool EsCompleta(double v) => Math.Abs(v - 1) < 0.02;
    public static bool EsMitad(double v) => Math.Abs(v - 0.5) < 0.02;
    public static int Porcentaje(double v) => (int)Math.Round((v <= 0 ? 1 : v) * 100);

    public static double FactorDeItem(double cantidad, double cantidadBase, double grupoFactor)
    {
        var g = grupoFactor <= 0 ? 1 : grupoFactor;
        var b = cantidadBase <= 0 ? 1 : cantidadBase;
        var den = b * g;
        return den <= 0 ? 1 : Math.Round(cantidad / den, 4);
    }
}
