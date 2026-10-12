using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

/// <summary>
/// Proteína fija (RF-E4). Grasa en g/kg según el objetivo, con piso de 0,5 g/kg.
/// Carbos: las kcal que quedan. 4 / 4 / 9 kcal por gramo.
/// </summary>
public sealed record ResultadoMacros(
    double KcalAComer,
    double ProteinaG,
    double CarbosG,
    double GrasaG,
    double KcalProteina,
    double KcalCarbos,
    double KcalGrasa,
    double GrasaGPorKg,
    bool AjustoGrasaAlPiso,
    bool NoAlcanza,
    string Texto);

public static class MacrosNecesarios
{
    public const double GrasaGPorKgCorte = 0.80;
    public const double GrasaGPorKgRecomp = 0.90;
    public const double GrasaGPorKgGanancia = 1.00;
    public const double GrasaGPorKgPiso = 0.50;

    public static double GrasaGPorKgDe(TipoObjetivo tipo) => tipo switch
    {
        TipoObjetivo.GananciaMusculo => GrasaGPorKgGanancia,
        TipoObjetivo.Recomposicion => GrasaGPorKgRecomp,
        _ => GrasaGPorKgCorte
    };

    public static ResultadoMacros? De(
        double? gastoDelDia,
        double diario,
        bool esSuperavit,
        double? proteinaG,
        double? pesoKg,
        TipoObjetivo tipo,
        bool incluyeEntreno = false)
    {
        if (gastoDelDia is not > 0 || proteinaG is not > 0 || pesoKg is not > 0)
            return null;

        var kcalComer = esSuperavit
            ? gastoDelDia.Value + Math.Max(0, diario)
            : Math.Max(0, gastoDelDia.Value - Math.Max(0, diario));
        var prot = proteinaG.Value;
        var kcalP = prot * 4;
        var resto = kcalComer - kcalP;
        var gPorKg = GrasaGPorKgDe(tipo);
        var piso = pesoKg.Value * GrasaGPorKgPiso;
        var ideal = pesoKg.Value * gPorKg;

        var ajusto = false;
        var noAlcanza = resto < piso * 9 - 0.5;
        double grasaBruta;
        if (resto <= 0)
        {
            grasaBruta = 0;
            noAlcanza = true;
        }
        else if (ideal * 9 <= resto + 0.5)
        {
            grasaBruta = ideal;
        }
        else if (piso * 9 <= resto + 0.5)
        {
            grasaBruta = piso;
            ajusto = true;
        }
        else
        {
            grasaBruta = resto / 9;
            ajusto = true;
        }

        var grasa = ACinco(grasaBruta);
        var kcalG = grasa * 9;
        var kcalC = Math.Max(0, resto - kcalG);
        var carbos = ACinco(kcalC / 4);
        kcalC = carbos * 4;

        var cuando = incluyeEntreno
            ? "Hoy, con el gasto de este día"
            : "En un día sentado (sin gym ni deporte)";
        string texto;
        if (resto <= 0)
        {
            texto = $"{cuando} habría que comer {Redondeo.Kcal(kcalComer)} kcal, y la proteína ya se las lleva. Bajá el recorte o la proteína.";
        }
        else if (noAlcanza)
        {
            texto = $"{cuando}: {Redondeo.Kcal(kcalComer)} kcal. El recorte no deja margen: grasa al piso y casi sin carbos.";
        }
        else if (ajusto)
        {
            texto = $"{cuando}: {Redondeo.Kcal(kcalComer)} kcal. Grasa al piso de {GrasaGPorKgPiso.ToString("0.#")} g/kg para dejar carbos.";
        }
        else
        {
            texto = $"{cuando}: {Redondeo.Kcal(kcalComer)} kcal. Proteína fija, grasa {gPorKg.ToString("0.#")} g/kg, carbos el resto.";
        }

        return new ResultadoMacros(
            kcalComer, prot, carbos, grasa,
            kcalP, kcalC, kcalG,
            gPorKg, ajusto, noAlcanza, texto);
    }

    private static double ACinco(double g)
        => Math.Max(0, Math.Round(g / 5, MidpointRounding.AwayFromZero) * 5);
}
