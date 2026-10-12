namespace KcalTracker.Calculo;

/// <summary>
/// RN-10: las kcal declaradas contra proteína × 4 + carbos × 4 + grasa × 9.
/// Nulo si faltan macros (RN-09) o si las kcal son 0.
/// </summary>
public static class CoherenciaMacros
{
    public static double? Desvio(double kcalDeclaradas, double? proteina, double? carbos, double? grasa)
    {
        if (proteina is null || carbos is null || grasa is null) return null;
        if (kcalDeclaradas == 0) return null;
        var calculadas = proteina.Value * 4 + carbos.Value * 4 + grasa.Value * 9;
        return Math.Abs(calculadas - kcalDeclaradas) / kcalDeclaradas;
    }
}
