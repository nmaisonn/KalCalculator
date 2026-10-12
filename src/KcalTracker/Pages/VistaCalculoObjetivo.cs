using KcalTracker.Calculo;
using KcalTracker.Dominio;

namespace KcalTracker.Pages;

public sealed class VistaCalculoObjetivo
{
    public ResultadoDeficit? Deficit { get; init; }
    public ResultadoPlan? Plan { get; init; }
    public RitmoObjetivo? Ritmo { get; init; }
    public TipoObjetivo TipoObjetivo { get; init; }
    public double GrasaObjetivoPct { get; init; }
    public ResultadoMacros? Macros { get; init; }
    public ResultadoBmr? Bmr { get; init; }
    public double? GastoSinDeporte { get; init; }
    public double FactorActividad { get; init; }
    public double? MasaMagraKg { get; init; }
    public LecturaSeguimiento? Seguimiento { get; init; }
    public ResultadoAlcance? Alcance { get; init; }
    public double? GrasaActualPct { get; init; }
    public string Handler { get; init; } = "Recalcular";
}
