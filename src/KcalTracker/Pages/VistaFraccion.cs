namespace KcalTracker.Pages;

public sealed class VistaFraccion
{
    public string Handler { get; init; } = "";
    public IReadOnlyList<(string Name, string Value)> Hidden { get; init; } = [];
    public double Valor { get; init; } = 1;
    public bool Htmx { get; init; }
    public string HtmxTarget { get; init; } = "#hoy-comidas";
    public string Titulo { get; init; } = "100 % es completo. 50 % deja la mitad de las kcal.";

    public static VistaFraccion Item(string fechaIso, int itemId, double valor) => new()
    {
        Handler = "Cantidad",
        Hidden = [("Fecha", fechaIso), ("itemId", itemId.ToString())],
        Valor = valor,
        Htmx = true
    };

    public static VistaFraccion Grupo(string fechaIso, string grupoId, double valor) => new()
    {
        Handler = "CantidadGrupo",
        Hidden = [("Fecha", fechaIso), ("grupoId", grupoId)],
        Valor = valor,
        Htmx = true,
        Titulo = "Recorta todo el combo. Después podés bajar un ingrediente."
    };

    public static VistaFraccion Gym(string fechaIso, int rutinaDiaId, double valor) => new()
    {
        Handler = "CantidadGym",
        Hidden = [("Fecha", fechaIso), ("rutinaDiaId", rutinaDiaId.ToString())],
        Valor = valor,
        Htmx = true,
        HtmxTarget = "#entreno"
    };

    public static VistaFraccion Cardio(string fechaIso, int protocoloId, double valor) => new()
    {
        Handler = "CantidadCardio",
        Hidden = [("Fecha", fechaIso), ("protocoloId", protocoloId.ToString())],
        Valor = valor,
        Htmx = true,
        HtmxTarget = "#entreno"
    };

    public static VistaFraccion Actividad(string fechaIso, int actividadId, double valor) => new()
    {
        Handler = "CantidadActividad",
        Hidden = [("Fecha", fechaIso), ("actividadId", actividadId.ToString())],
        Valor = valor,
        Htmx = true,
        HtmxTarget = "#entreno"
    };
}

public sealed record VistaHoyLinea(KcalTracker.Dominio.ComidaItem Item, string FechaIso);
