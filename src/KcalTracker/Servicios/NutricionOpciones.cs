namespace KcalTracker.Servicios;

public sealed class NutricionOpciones
{
    public const string Seccion = "Nutricion";

    public string? UsdaApiKey { get; set; }
    public string? OpenAiApiKey { get; set; }
    public string? OpenAiBaseUrl { get; set; }
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
}
