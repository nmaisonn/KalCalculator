namespace KcalTracker.Dominio;

/// <summary>
/// Valores de partida de una cuenta nueva. Viven acá y no en las fórmulas:
/// el cálculo siempre lee la fila de <see cref="Configuracion"/> del usuario.
/// </summary>
public static class ValoresDeFabrica
{
    /// <summary>
    /// Densidad energética del tejido adiposo: 9,4 kcal/g de lípido × 87 % = 8178 kcal/kg.
    /// Wishnofsky (7700) mezcla grasa y magro; acá RN-04 asume magra constante.
    /// </summary>
    public const double KcalPorKgGrasa = 8178;
    /// <summary>Tejido magro en el modelo de Hall: 1,8 kcal/g.</summary>
    public const double KcalPorKgMagro = 1800;
    public const double PctGastoMax = 0.25;
    public const double PctPesoSemanal = 0.0075;
    public const double CobMinContraste = 0.50;
    public const double CobMinCalibrado = 0.70;
    /// <summary>
    /// PAL cotidiano sin entrenamiento. 1.2 es estar casi todo el día sentado;
    /// 1.35 cubre oficina más caminar y es el valor de partida realista
    /// cuando el gym/deporte se suma aparte.
    /// </summary>
    public const double FactorActividad = 1.35;
    public const int PlazoSemanas = 8;
    /// <summary>
    /// Si no hay % de grasa, el recorte no puede salir de kg a perder (RN-04).
    /// ISSN/ACS: 10–20 % del GET. Corte usa el 20 %; recomposición, ~10 %.
    /// </summary>
    public const double PctDeficitCorte = 0.20;
    public const double PctDeficitRecomp = 0.10;

    public static readonly double[] FactoresActividadSugeridos = [1.2, 1.35, 1.5];
}
