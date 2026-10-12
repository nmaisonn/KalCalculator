using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

public sealed record ResultadoProteina(
    double SugeridaG,
    double MinG,
    double MaxG,
    double PesoKg,
    double? MasaMagraKg,
    bool UsaMasaMagra,
    TipoObjetivo Tipo);

/// <summary>
/// Corte y recomposición: Helms/ISSN 2,3–3,1 g/kg magra (sugerido 3,0).
/// Ganancia: 1,6–2,2 g/kg de peso (sugerido 2,0); en surplus no hace falta tanto.
/// Sin % de grasa, corte/recomp cae a 1,6–2,4 g/kg peso con 2,2 de sugerido.
/// </summary>
public static class ProteinaNecesaria
{
    public const double GPorKgMagraMin = 2.3;
    public const double GPorKgMagraSugerido = 3.0;
    public const double GPorKgMagraMax = 3.1;
    public const double GPorKgPesoMin = 1.6;
    public const double GPorKgPesoSugerido = 2.2;
    public const double GPorKgPesoMax = 2.4;
    public const double GPorKgPesoGananciaSugerido = 2.0;

    public static ResultadoProteina? Calcular(double? pesoKg, double? grasaPct, TipoObjetivo tipo = TipoObjetivo.Corte)
    {
        if (pesoKg is not > 0) return null;
        var peso = pesoKg.Value;
        if (tipo == TipoObjetivo.GananciaMusculo)
        {
            return new ResultadoProteina(
                ACinco(peso * GPorKgPesoGananciaSugerido),
                peso * GPorKgPesoMin,
                peso * GPorKgPesoSugerido,
                peso, Composicion.MagraKg(peso, grasaPct), false, tipo);
        }

        var magra = Composicion.MagraKg(peso, grasaPct);
        if (magra is > 0)
        {
            return new ResultadoProteina(
                ACinco(magra.Value * GPorKgMagraSugerido),
                magra.Value * GPorKgMagraMin,
                magra.Value * GPorKgMagraMax,
                peso, magra, true, tipo);
        }

        return new ResultadoProteina(
            ACinco(peso * GPorKgPesoSugerido),
            peso * GPorKgPesoMin,
            peso * GPorKgPesoMax,
            peso, null, false, tipo);
    }

    public static double? Objetivo(double? guardada, double? pesoKg, double? grasaPct, TipoObjetivo tipo = TipoObjetivo.Corte)
        => guardada is > 0 ? guardada : Calcular(pesoKg, grasaPct, tipo)?.SugeridaG;

    private static double ACinco(double g)
        => Math.Round(g / 5, MidpointRounding.AwayFromZero) * 5;
}
