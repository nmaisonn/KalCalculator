namespace KcalTracker.Calculo;

public sealed record ResultadoCalibracion(
    DateOnly Desde,
    DateOnly Hasta,
    int Dias,
    int DiasConRegistro,
    double Cobertura,
    double GrasaPerdidaKg,
    double MusculoGanadoKg,
    double EnergiaLiberada,
    double? GastoRealDiario,
    double? IngestaMedia,
    string? Aviso);

/// <summary>RN-06. El gasto real se infiere de dos mediciones y de la ingesta del período.</summary>
public static class Calibracion
{
    public static ResultadoCalibracion Calcular(
        DateOnly desde,
        DateOnly hasta,
        double pesoInicial,
        double grasaPctInicial,
        double? musculoPctInicial,
        double pesoFinal,
        double grasaPctFinal,
        double? musculoPctFinal,
        int diasConRegistro,
        double kcalIngeridasTotales,
        double kcalPorKgGrasa,
        double kcalPorKgMagro)
    {
        var dias = hasta.DayNumber - desde.DayNumber;
        if (dias <= 0) dias = 1;

        var dGrasa = pesoInicial * grasaPctInicial / 100 - pesoFinal * grasaPctFinal / 100;
        var dMusculo = (musculoPctFinal is { } mf && musculoPctInicial is { } mi)
            ? pesoFinal * mf / 100 - pesoInicial * mi / 100
            : 0;
        var energia = dGrasa * kcalPorKgGrasa - dMusculo * kcalPorKgMagro;
        var cobertura = (double)diasConRegistro / dias;
        var ingestaMedia = diasConRegistro > 0 ? kcalIngeridasTotales / diasConRegistro : (double?)null;
        var gastoReal = ingestaMedia is { } ing ? ing + energia / dias : (double?)null;

        string? aviso = null;
        if (dias < 14 || diasConRegistro < 7)
            aviso = "El período es corto o hay pocos días con registro: el número es indicativo.";

        return new ResultadoCalibracion(
            desde, hasta, dias, diasConRegistro, cobertura,
            dGrasa, dMusculo, energia, gastoReal, ingestaMedia, aviso);
    }
}
