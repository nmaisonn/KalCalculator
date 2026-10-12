namespace KcalTracker.Dominio;

public enum MetodoBmr
{
    KatchMcArdle = 0,
    Mifflin = 1,
    Medido = 2
}

/// <summary>
/// Una fila por cuenta. Ningún número del cálculo vive en el código (RNF-17):
/// si hay que cambiar un umbral, se cambia acá y no con un deploy.
/// </summary>
public class Configuracion : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public double KcalPorKgGrasa { get; set; } = ValoresDeFabrica.KcalPorKgGrasa;
    public double KcalPorKgMagro { get; set; } = ValoresDeFabrica.KcalPorKgMagro;
    public MetodoBmr MetodoBmr { get; set; } = MetodoBmr.KatchMcArdle;
    public double? BmrMedido { get; set; }
    public double PctGastoMax { get; set; } = ValoresDeFabrica.PctGastoMax;
    public double PctPesoSemanal { get; set; } = ValoresDeFabrica.PctPesoSemanal;
    public double CobMinContraste { get; set; } = ValoresDeFabrica.CobMinContraste;
    public double CobMinCalibrado { get; set; } = ValoresDeFabrica.CobMinCalibrado;
}
