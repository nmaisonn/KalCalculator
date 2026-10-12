using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

public sealed record ResultadoBmr(
    double? Kcal,
    MetodoBmr Pedido,
    MetodoBmr? Aplicado,
    bool UsoRespaldo,
    string? Aviso);

/// <summary>RN-01. El método y el valor medido salen de la configuración del usuario, no del código.</summary>
public static class Metabolismo
{
    public static double KatchMcArdle(double pesoKg, double grasaPct)
    {
        var masaMagra = pesoKg * (1 - grasaPct / 100);
        return 370 + 21.6 * masaMagra;
    }

    public static double MifflinStJeor(double pesoKg, double alturaCm, int edad, Sexo sexo)
    {
        var ajuste = sexo == Sexo.Masculino ? 5 : -161;
        return 10 * pesoKg + 6.25 * alturaCm - 5 * edad + ajuste;
    }

    public static int? Edad(DateOnly nacimiento, DateOnly en)
    {
        var edad = en.Year - nacimiento.Year;
        if (en < nacimiento.AddYears(edad)) edad--;
        return edad < 0 ? null : edad;
    }

    public static ResultadoBmr Calcular(
        MetodoBmr pedido,
        double? bmrMedido,
        double pesoKg,
        double? grasaPct,
        double? alturaCm,
        int? edad,
        Sexo? sexo)
    {
        if (pedido == MetodoBmr.Medido)
        {
            if (bmrMedido is null)
            {
                return new ResultadoBmr(null, pedido, null, false,
                    "Falta el valor medido del metabolismo basal.");
            }

            return new ResultadoBmr(bmrMedido, pedido, MetodoBmr.Medido, false, null);
        }

        if (pedido == MetodoBmr.KatchMcArdle && grasaPct is not null)
        {
            return new ResultadoBmr(
                KatchMcArdle(pesoKg, grasaPct.Value),
                pedido, MetodoBmr.KatchMcArdle, false, null);
        }

        var mifflin = IntentarMifflin(pesoKg, alturaCm, edad, sexo);
        if (mifflin is not null)
        {
            var aviso = pedido == MetodoBmr.KatchMcArdle
                ? "Se usó Mifflin-St Jeor porque la medición no trae porcentaje de grasa."
                : null;

            return new ResultadoBmr(
                mifflin, pedido, MetodoBmr.Mifflin,
                pedido == MetodoBmr.KatchMcArdle, aviso);
        }

        if (pedido == MetodoBmr.KatchMcArdle)
        {
            return new ResultadoBmr(null, pedido, null, true,
                "La medición no trae porcentaje de grasa y faltan altura, edad o sexo para usar Mifflin-St Jeor.");
        }

        return new ResultadoBmr(null, pedido, null, false,
            "Faltan altura, fecha de nacimiento o sexo para Mifflin-St Jeor.");
    }

    public static string Etiqueta(MetodoBmr? metodo) => metodo switch
    {
        MetodoBmr.KatchMcArdle => "Katch-McArdle",
        MetodoBmr.Mifflin => "Mifflin-St Jeor",
        MetodoBmr.Medido => "GER medido",
        _ => "basal"
    };

    public static string Formula(MetodoBmr? metodo, double? magraKg = null)
    {
        if (metodo == MetodoBmr.KatchMcArdle)
        {
            return magraKg is { } magra
                ? $"370 + 21,6 × {Redondeo.Kg(magra)} kg magros"
                : "370 + 21,6 × masa magra";
        }

        if (metodo == MetodoBmr.Mifflin)
            return "10 × peso + 6,25 × altura − 5 × edad + 5 (hombres) o −161 (mujeres)";
        if (metodo == MetodoBmr.Medido)
            return "Valor de la báscula, acostado. No es el gasto del día.";
        return "";
    }

    /// <summary>GER de báscula si está; si no, Katch con % de grasa; si no, Mifflin.</summary>
    public static MetodoBmr Elegir(double? bmrMedido, double? grasaPct)
    {
        if (bmrMedido is > 0) return MetodoBmr.Medido;
        if (grasaPct is > 0 and < 100) return MetodoBmr.KatchMcArdle;
        return MetodoBmr.Mifflin;
    }

    /// <summary>RN-02, capa cotidiana: BMR × factor de actividad, sin deporte.</summary>
    public static double? GastoSinDeporte(ResultadoBmr bmr, double factorActividad)
        => bmr.Kcal is null ? null : bmr.Kcal.Value * factorActividad;

    private static double? IntentarMifflin(double pesoKg, double? alturaCm, int? edad, Sexo? sexo)
    {
        if (alturaCm is null || edad is null || sexo is null) return null;
        return MifflinStJeor(pesoKg, alturaCm.Value, edad.Value, sexo.Value);
    }
}
