using System.Globalization;
using KcalTracker.Calculo;

namespace KcalTracker;

public static class Formato
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-UY");

    public static string Kcal(double n) => Redondeo.Kcal(n).ToString("N0", Es);
    public static string G(double n) => n.ToString("0.#", Es);
    public static string Kg(double n, int d = 2) => Redondeo.Kg(n, d).ToString(d == 1 ? "0.#" : "0.00", Es);
    public static string Factor(double n) => n.ToString("0.##", Es);

    public static string Pct(double fraccion)
        => (fraccion * 100).ToString("0.#", Es) + " %";

    /// <summary>Ancho CSS: siempre con punto, si no <c>53,5%</c> no pinta en el navegador.</summary>
    public static string PctCss(double n)
        => Math.Clamp(n, 0, 100).ToString("0.##", CultureInfo.InvariantCulture);

    public static string FechaCorta(DateOnly f) => f.ToString("d/M/yyyy", Es);

    public static string FechaLarga(DateOnly f)
        => f.ToString("dddd d 'de' MMMM 'de' yyyy", Es);

    public static string FechaDia(DateOnly f)
        => f.ToString("dddd d 'de' MMMM", Es);

    public static string DiaCorto(DateOnly f)
    {
        var t = f.ToString("ddd d", Es);
        return string.IsNullOrEmpty(t) ? t : char.ToUpper(t[0], Es) + t[1..];
    }

    public static string Rango(DateOnly desde, DateOnly hasta)
    {
        if (desde.Month == hasta.Month && desde.Year == hasta.Year)
            return $"{desde.Day}–{hasta.ToString("d 'de' MMMM", Es)}";
        return $"{desde.ToString("d MMM", Es)} – {hasta.ToString("d MMM yyyy", Es)}";
    }
}
