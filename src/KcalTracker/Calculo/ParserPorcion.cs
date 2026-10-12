using System.Globalization;
using System.Text.RegularExpressions;

namespace KcalTracker.Calculo;

public sealed record ConsultaPorcion(string Nombre, string TextoPorcion, double? Gramos);

public static partial class ParserPorcion
{
    [GeneratedRegex(@"(?<n>\d+(?:[.,]\d+)?)\s*(?<u>kilos?|kg|gramos?|grs?|g|ml|cc)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RxCantidad();

    public static ConsultaPorcion Parse(string texto)
    {
        texto = (texto ?? "").Trim();
        if (texto.Length == 0)
            return new ConsultaPorcion("", "1 porción", null);

        var m = RxCantidad().Match(texto);
        if (!m.Success)
            return new ConsultaPorcion(LimpiarNombre(texto), "1 porción", null);

        var n = double.Parse(m.Groups["n"].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
        var u = m.Groups["u"].Value.ToLowerInvariant();
        var gramos = u.StartsWith('k') ? n * 1000 : n;
        var nombre = LimpiarNombre(texto[..m.Index] + " " + texto[(m.Index + m.Length)..]);
        if (string.IsNullOrWhiteSpace(nombre))
            nombre = LimpiarNombre(texto);

        var porcion = gramos >= 1000
            ? (gramos / 1000).ToString("0.##", CultureInfo.GetCultureInfo("es-UY")) + " kg"
            : gramos.ToString("0.#", CultureInfo.GetCultureInfo("es-UY")) + " g";

        return new ConsultaPorcion(nombre, porcion, gramos);
    }

    private static string LimpiarNombre(string bruto)
    {
        var t = Regex.Replace(bruto, @"[\(\)\[\]:,;]+", " ");
        t = Regex.Replace(t, @"\s+", " ").Trim(" -–—".ToCharArray());
        t = Regex.Replace(t, @"^(de|del|con)\s+", "", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"\s+(de|del)$", "", RegexOptions.IgnoreCase);
        return t.Trim();
    }
}

public static class EscaladoNutricion
{
    public static (double Kcal, double? Proteina, double? Carbos, double? Grasa) AGramos(
        double kcal, double? proteina, double? carbos, double? grasa, double gramosOrigen, double gramosDestino)
    {
        if (gramosOrigen <= 0 || gramosDestino <= 0)
            return (kcal, proteina, carbos, grasa);
        var f = gramosDestino / gramosOrigen;
        return (kcal * f, proteina * f, carbos * f, grasa * f);
    }
}
