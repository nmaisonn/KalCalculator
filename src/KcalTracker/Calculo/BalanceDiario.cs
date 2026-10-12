namespace KcalTracker.Calculo;

public sealed record ItemIngesta(
    double Cantidad,
    double Kcal,
    double? Proteina,
    double? Carbos,
    double? Grasa);

public sealed record ResumenIngesta(
    double Kcal,
    double? Proteina,
    double? Carbos,
    double? Grasa,
    double KcalSinMacros,
    int Items);

public sealed record GastoDelDia(
    double SinDeporte,
    double Actividades,
    double Gym,
    double Cardio,
    double Total,
    bool PosibleDobleConteo);

public sealed record ResultadoDia(
    bool HayRegistro,
    ResumenIngesta? Ingesta,
    GastoDelDia Gasto,
    double? Deficit);

/// <summary>
/// RN-02 (gasto por capas) y RN-03 (déficit). Un día sin comidas no tiene déficit (RN-07).
/// </summary>
public static class BalanceDiario
{
    public static GastoDelDia CalcularGasto(
        double gastoSinDeporte,
        IEnumerable<double>? kcalActividades = null,
        IEnumerable<double>? kcalGym = null,
        IEnumerable<double>? kcalCardio = null)
    {
        var actividades = kcalActividades?.ToList() ?? [];
        var gym = kcalGym?.ToList() ?? [];
        var cardio = kcalCardio?.ToList() ?? [];

        var sumaAct = actividades.Sum();
        var sumaGym = gym.Sum();
        var sumaCardio = cardio.Sum();

        // El catálogo de actividades no debería representar gym ni cardio (D-17).
        // Si igual coinciden en el mismo día, se suma todo y se avisa (RN-02, CA-38).
        var dobleConteo = actividades.Count > 0 && (gym.Count > 0 || cardio.Count > 0);

        return new GastoDelDia(
            gastoSinDeporte,
            sumaAct,
            sumaGym,
            sumaCardio,
            gastoSinDeporte + sumaAct + sumaGym + sumaCardio,
            dobleConteo);
    }

    /// <summary>
    /// RN-09: las kcal de un item sin macros entran al total y al renglón "sin macros conocidos".
    /// Un macro desconocido no se cuenta como cero.
    /// </summary>
    public static ResumenIngesta SumarIngesta(IEnumerable<ItemIngesta> items)
    {
        double kcal = 0;
        double kcalSinMacros = 0;
        double proteina = 0, carbos = 0, grasa = 0;
        var hayProteina = false;
        var hayCarbos = false;
        var hayGrasa = false;
        var n = 0;

        foreach (var item in items)
        {
            n++;
            var aporte = item.Kcal * item.Cantidad;
            kcal += aporte;

            var sinMacros = item.Proteina is null && item.Carbos is null && item.Grasa is null;
            if (sinMacros)
            {
                kcalSinMacros += aporte;
                continue;
            }

            if (item.Proteina is { } p)
            {
                proteina += p * item.Cantidad;
                hayProteina = true;
            }

            if (item.Carbos is { } c)
            {
                carbos += c * item.Cantidad;
                hayCarbos = true;
            }

            if (item.Grasa is { } g)
            {
                grasa += g * item.Cantidad;
                hayGrasa = true;
            }
        }

        return new ResumenIngesta(
            kcal,
            hayProteina ? proteina : null,
            hayCarbos ? carbos : null,
            hayGrasa ? grasa : null,
            kcalSinMacros,
            n);
    }

    /// <summary>
    /// Positivo es déficit; negativo, superávit. Null si el día no tiene comidas (CA-11).
    /// </summary>
    public static double? Deficit(double gastoDelDia, bool hayRegistroDeComida, double ingesta)
        => hayRegistroDeComida ? gastoDelDia - ingesta : null;

    public static ResultadoDia DelDia(
        double gastoSinDeporte,
        IEnumerable<ItemIngesta>? comidas,
        IEnumerable<double>? kcalActividades = null,
        IEnumerable<double>? kcalGym = null,
        IEnumerable<double>? kcalCardio = null)
    {
        var items = comidas?.ToList() ?? [];
        var hayRegistro = items.Count > 0;
        var ingesta = hayRegistro ? SumarIngesta(items) : null;
        var gasto = CalcularGasto(gastoSinDeporte, kcalActividades, kcalGym, kcalCardio);
        var deficit = Deficit(gasto.Total, hayRegistro, ingesta?.Kcal ?? 0);

        return new ResultadoDia(hayRegistro, ingesta, gasto, deficit);
    }
}
