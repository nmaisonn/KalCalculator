namespace KcalTracker.Calculo;

public enum NivelSugerencia
{
    Consistente,
    Advertencia,
    NoConfiable,
    Invalida
}

public sealed record ResultadoSugerencia(
    NivelSugerencia Nivel,
    double? Desvio,
    double KcalCalculadas,
    string Detalle);

/// <summary>RN-10. Aritmética pura, sin depender de la fuente.</summary>
public static class ValidadorSugerencia
{
    public static ResultadoSugerencia Validar(
        double kcalDeclaradas, double? proteina, double? carbos, double? grasa, double? gramosPorcion = null)
    {
        if (kcalDeclaradas < 0 || proteina is < 0 || carbos is < 0 || grasa is < 0)
            return new ResultadoSugerencia(NivelSugerencia.Invalida, null, 0, "Hay un valor negativo.");

        if (proteina is null || carbos is null || grasa is null)
            return new ResultadoSugerencia(NivelSugerencia.Advertencia, null, 0, "Faltan macros: no se puede validar la coherencia.");

        if (gramosPorcion is > 0 && proteina + carbos + grasa > gramosPorcion + 0.5)
            return new ResultadoSugerencia(NivelSugerencia.Invalida, null, 0, "Los macros suman más que el peso de la porción.");

        var calculadas = proteina.Value * 4 + carbos.Value * 4 + grasa.Value * 9;
        if (kcalDeclaradas == 0)
            return new ResultadoSugerencia(NivelSugerencia.Invalida, null, calculadas, "Las kcal declaradas son 0.");

        var desvio = Math.Abs(calculadas - kcalDeclaradas) / kcalDeclaradas;
        var nivel = desvio <= 0.05 ? NivelSugerencia.Consistente
            : desvio <= 0.15 ? NivelSugerencia.Advertencia
            : NivelSugerencia.NoConfiable;

        var detalle = nivel switch
        {
            NivelSugerencia.Consistente => $"Coherente: los macros dan {Redondeo.Kcal(calculadas)} kcal.",
            NivelSugerencia.Advertencia => $"Desvío {desvio * 100:0.#} %: los macros dan {Redondeo.Kcal(calculadas)} kcal y declara {Redondeo.Kcal(kcalDeclaradas)}.",
            _ => $"No confiable: {Redondeo.Kcal(calculadas)} contra {Redondeo.Kcal(kcalDeclaradas)} es {desvio * 100:0} % de desvío. Revisalo antes de aceptar."
        };

        return new ResultadoSugerencia(nivel, desvio, calculadas, detalle);
    }
}
