using System.Globalization;

namespace KcalTracker.Calculo;

public sealed record PuntoSerie(double X, double Y, DateOnly Fecha, double Valor);

public sealed record GraficoLinea(
    double Ancho,
    double Alto,
    string Path,
    string PathArea,
    IReadOnlyList<PuntoSerie> Puntos,
    double YMaxValor,
    double YMinValor,
    double YMaxY,
    double YMinY,
    double YBase,
    double XIni,
    double XFin,
    double? ObjetivoY);

/// <summary>Línea en el tiempo para peso, % o visceral. Hacen falta al menos dos puntos.</summary>
public static class GraficoSerie
{
    public const double Ancho = 620;
    public const double Alto = 188;

    public static GraficoLinea? De(
        IReadOnlyList<(DateOnly Fecha, double Valor)> puntos,
        double? objetivo = null)
    {
        if (puntos.Count < 2) return null;

        const double padIzq = 36;
        const double padDer = 16;
        const double padArr = 22;
        const double padAb = 26;
        var interior = Ancho - padIzq - padDer;
        var altoUtil = Alto - padArr - padAb;

        var loDato = puntos.Min(p => p.Valor);
        var hiDato = puntos.Max(p => p.Valor);
        var lo = objetivo is { } obj ? Math.Min(loDato, obj) : loDato;
        var hi = objetivo is { } obj2 ? Math.Max(hiDato, obj2) : hiDato;
        if (Math.Abs(hi - lo) < 0.05)
        {
            lo -= 3;
            hi += 3;
        }
        else
        {
            var margen = Math.Max(0.4, (hi - lo) * 0.12);
            lo -= margen;
            hi += margen;
        }

        var t0 = puntos[0].Fecha.DayNumber;
        var t1 = puntos[^1].Fecha.DayNumber;
        var porFecha = t1 > t0;
        var span = Math.Max(1, t1 - t0);

        double X(int i, DateOnly fecha)
        {
            if (!porFecha)
                return padIzq + i / (double)(puntos.Count - 1) * interior;
            return padIzq + (fecha.DayNumber - t0) / (double)span * interior;
        }

        double Y(double valor)
            => Alto - padAb - (valor - lo) / (hi - lo) * altoUtil;

        var dibujo = puntos.Select((p, i) => new PuntoSerie(X(i, p.Fecha), Y(p.Valor), p.Fecha, p.Valor)).ToList();
        var inv = CultureInfo.InvariantCulture;
        string N(double v) => v.ToString("0.##", inv);
        var path = string.Join(" ", dibujo.Select((p, i) => $"{(i == 0 ? "M" : "L")}{N(p.X)},{N(p.Y)}"));
        var yBase = Alto - padAb;
        var area = $"{path} L{N(dibujo[^1].X)},{N(yBase)} L{N(dibujo[0].X)},{N(yBase)} Z";

        return new GraficoLinea(
            Ancho, Alto, path, area, dibujo,
            hiDato, loDato, Y(hiDato), Y(loDato), yBase,
            dibujo[0].X, dibujo[^1].X,
            objetivo is { } o ? Y(o) : null);
    }
}
