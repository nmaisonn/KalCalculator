using System.Globalization;

namespace KcalTracker.Calculo;

public sealed record PuntoProgresion(DateOnly Fecha, double Peso, int? Reps);

public sealed record ResumenProgresion(
    double DesdeKg,
    double HastaKg,
    double MinKg,
    double MaxKg,
    double DeltaKg,
    double DeltaFraccion,
    int Sesiones);

public sealed record PuntoGrafico(double X, double Y, DateOnly Fecha, double Peso, int? Reps);

public sealed record GraficoProgresion(
    double Ancho,
    double Alto,
    string Path,
    IReadOnlyList<PuntoGrafico> Puntos,
    double YMaxKg,
    double YMinKg,
    double YMaxY,
    double YMinY,
    double XIni,
    double XFin);

/// <summary>RF-K5, CA-35. Solo entran puntos con peso numérico (RN-13).</summary>
public static class Progresion
{
    public const double Ancho = 620;
    public const double Alto = 168;

    public static IReadOnlyList<PuntoProgresion> De(IEnumerable<(DateOnly Fecha, double? Peso, int? Reps)> sesiones)
        => sesiones
            .Where(s => s.Peso is not null)
            .OrderBy(s => s.Fecha)
            .Select(s => new PuntoProgresion(s.Fecha, s.Peso!.Value, s.Reps))
            .ToList();

    public static ResumenProgresion? Resumir(IReadOnlyList<PuntoProgresion> puntos)
    {
        if (puntos.Count == 0) return null;
        var desde = puntos[0].Peso;
        var hasta = puntos[^1].Peso;
        var min = puntos.Min(p => p.Peso);
        var max = puntos.Max(p => p.Peso);
        var delta = hasta - desde;
        var frac = desde == 0 ? 0 : delta / desde;
        return new ResumenProgresion(desde, hasta, min, max, delta, frac, puntos.Count);
    }

    public static GraficoProgresion? Grafico(IReadOnlyList<PuntoProgresion> puntos)
    {
        if (puntos.Count < 2) return null;

        const double padIzq = 36;
        const double padDer = 16;
        const double padArr = 14;
        const double padAb = 24;
        var interior = Ancho - padIzq - padDer;
        var altoUtil = Alto - padArr - padAb;

        var loDato = puntos.Min(p => p.Peso);
        var hiDato = puntos.Max(p => p.Peso);
        var lo = loDato;
        var hi = hiDato;
        if (Math.Abs(hi - lo) < 0.05)
        {
            lo -= 3;
            hi += 3;
        }
        else
        {
            var margen = Math.Max(1, (hi - lo) * 0.12);
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

        double Y(double kg)
            => Alto - padAb - (kg - lo) / (hi - lo) * altoUtil;

        var dibujo = puntos.Select((p, i) => new PuntoGrafico(X(i, p.Fecha), Y(p.Peso), p.Fecha, p.Peso, p.Reps)).ToList();
        var inv = CultureInfo.InvariantCulture;
        string N(double v) => v.ToString("0.##", inv);
        var path = string.Join(" ", dibujo.Select((p, i) => $"{(i == 0 ? "M" : "L")}{N(p.X)},{N(p.Y)}"));

        return new GraficoProgresion(
            Ancho, Alto, path, dibujo,
            hiDato, loDato, Y(hiDato), Y(loDato),
            dibujo[0].X, dibujo[^1].X);
    }
}
