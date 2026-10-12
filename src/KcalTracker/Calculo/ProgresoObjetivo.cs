using KcalTracker;

namespace KcalTracker.Calculo;

/// <summary>
/// Cómo va el recorte (o el superávit) del día y del período frente al objetivo.
/// Los días sin comida cuentan 0 en el acumulado del período: no se inventa el déficit.
/// </summary>
public sealed record LecturaProgreso(
    string TextoDia,
    string TextoPeriodo,
    string ClaseDia,
    string ClasePeriodo);

public static class ProgresoObjetivo
{
    public static LecturaProgreso De(
        IReadOnlyList<DiaDePeriodo> serie,
        DateOnly hoy,
        double objDia,
        bool superavit,
        string etiquetaPeriodo)
    {
        var verbo = superavit ? "superávit" : "déficit";

        if (objDia <= 0)
        {
            return new LecturaProgreso(
                "Todavía no hay un recorte diario definido. Armalo en Objetivo.",
                "Sin objetivo no se puede decir si la semana está completa.",
                "",
                "");
        }

        var (textoDia, claseDia) = Dia(serie, hoy, objDia, superavit, verbo);
        var (textoPeriodo, clasePeriodo) = PeriodoTxt(serie, objDia, superavit, verbo, etiquetaPeriodo);
        return new LecturaProgreso(textoDia, textoPeriodo, claseDia, clasePeriodo);
    }

    private static (string Texto, string Clase) Dia(
        IReadOnlyList<DiaDePeriodo> serie,
        DateOnly hoy,
        double objDia,
        bool superavit,
        string verbo)
    {
        var dia = serie.FirstOrDefault(d => d.Fecha == hoy);
        var esHoy = dia is not null;
        dia ??= serie.LastOrDefault();
        if (dia is null)
            return ("No hay días en este período.", "");

        var cuando = esHoy ? "Hoy" : $"El {Formato.FechaDia(dia.Fecha)}";
        if (!dia.HayRegistro)
        {
            return (
                $"{cuando} todavía no hay comida cargada. El {verbo} del día son {Formato.Kcal(objDia)} kcal.",
                "warn");
        }

        var hecho = Valor(dia, superavit);
        var falta = objDia - hecho;
        if (hecho < -0.5 && !superavit)
        {
            return (
                $"{cuando} comiste de más: superávit de {Formato.Kcal(Math.Abs(hecho))} kcal. Te faltan {Formato.Kcal(objDia - hecho)} para el recorte del día.",
                "bad");
        }

        if (falta <= 0.5)
        {
            var extra = -falta > 10
                ? $" Te pasaste {Formato.Kcal(-falta)} kcal."
                : "";
            return (
                $"{cuando} completaste el {verbo} del día: {Formato.Kcal(hecho)} de {Formato.Kcal(objDia)} kcal.{extra}",
                "ok");
        }

        return (
            $"{cuando} llevás {Formato.Kcal(Math.Max(0, hecho))} de {Formato.Kcal(objDia)} kcal de {verbo}. Te faltan {Formato.Kcal(falta)} para completar el día.",
            "warn");
    }

    private static (string Texto, string Clase) PeriodoTxt(
        IReadOnlyList<DiaDePeriodo> serie,
        double objDia,
        bool superavit,
        string verbo,
        string etiquetaPeriodo)
    {
        var n = serie.Count;
        if (n == 0)
            return ("No hay días en este período.", "");

        var meta = objDia * n;
        var hecho = serie.Where(d => d.HayRegistro).Sum(d => Valor(d, superavit));
        var vacios = serie.Count(d => !d.HayRegistro);
        var falta = meta - hecho;
        var vaciosTxt = vacios == 0
            ? ""
            : vacios == 1
                ? " Hay 1 día sin comida (cuenta 0)."
                : $" Hay {vacios} días sin comida (cuentan 0).";

        if (hecho < -0.5 && !superavit)
        {
            return (
                $"En {etiquetaPeriodo} comiste de más en neto: superávit de {Formato.Kcal(Math.Abs(hecho))} kcal. Te faltan {Formato.Kcal(meta - hecho)} para el recorte.{vaciosTxt}",
                "bad");
        }

        if (falta <= 0.5)
        {
            return (
                $"Completaste el {verbo} de {etiquetaPeriodo}: {Formato.Kcal(hecho)} de {Formato.Kcal(meta)} kcal.",
                "ok");
        }

        return (
            $"En {etiquetaPeriodo} llevás {Formato.Kcal(Math.Max(0, hecho))} de {Formato.Kcal(meta)} kcal de {verbo}. Te faltan {Formato.Kcal(falta)}.{vaciosTxt}",
            "warn");
    }

    private static double Valor(DiaDePeriodo dia, bool superavit)
    {
        var d = dia.Deficit ?? 0;
        return superavit ? -d : d;
    }
}
