namespace KcalTracker.Calculo;

/// <summary>RF-D3: grasa y magra en kg se derivan al leer, no se guardan.</summary>
public static class Composicion
{
    public static double? GrasaKg(double pesoKg, double? grasaPct)
        => grasaPct is null ? null : pesoKg * grasaPct.Value / 100;

    public static double? MagraKg(double pesoKg, double? grasaPct)
        => grasaPct is null ? null : pesoKg * (1 - grasaPct.Value / 100);

    public static double? MusculoKg(double pesoKg, double? musculoPct)
        => musculoPct is null ? null : pesoKg * musculoPct.Value / 100;
}
