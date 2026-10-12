namespace KcalTracker.Calculo;

public static class Redondeo
{
    public static int Kcal(double valor)
        => (int)Math.Round(valor, MidpointRounding.AwayFromZero);

    public static double Kg(double valor, int decimales = 2)
        => Math.Round(valor, decimales, MidpointRounding.AwayFromZero);
}
