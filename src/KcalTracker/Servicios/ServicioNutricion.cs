using KcalTracker.Calculo;

namespace KcalTracker.Servicios;

public class ServicioNutricion(
    OpenFoodFactsProveedor off,
    UsdaProveedor usda,
    ModeloLenguajeProveedor modelo)
{
    public async Task<(IReadOnlyList<SugerenciaNutricional> Resultados, string? Estado, string? Error)> BuscarAsync(
        string consulta, CancellationToken ct = default)
        => await CalcularAsync(consulta, incluirModelo: false, ct);

    public async Task<(IReadOnlyList<SugerenciaNutricional> Resultados, string? Estado, string? Error)> CalcularAsync(
        string consulta, bool incluirModelo, CancellationToken ct = default)
    {
        consulta = (consulta ?? "").Trim();
        if (consulta.Length == 0)
            return ([], null, "Escribí el alimento y la cantidad, por ejemplo «puré 250 g».");

        var parsed = ParserPorcion.Parse(consulta);
        if (string.IsNullOrWhiteSpace(parsed.Nombre))
            return ([], null, "No se entendió el nombre del alimento.");

        var resultados = new List<SugerenciaNutricional>();
        var fuentes = new List<string>();

        try
        {
            await ConsultarAsync(off, parsed, resultados, fuentes, ct);
            if (incluirModelo)
            {
                await ConsultarAsync(usda, parsed, resultados, fuentes, ct);
                await ConsultarAsync(modelo, parsed, resultados, fuentes, ct);
            }
        }
        catch (HttpRequestException)
        {
            return (resultados, EstadoDe(fuentes), resultados.Count == 0
                ? "No hay conexión. La carga manual sigue funcionando."
                : "Se cortó la conexión a mitad de camino. Revisá lo que llegó.");
        }
        catch (TaskCanceledException)
        {
            return (resultados, EstadoDe(fuentes), "La consulta tardó demasiado. Probá de nuevo o cargalo a mano.");
        }

        var estado = EstadoDe(fuentes);
        if (resultados.Count == 0)
        {
            var extra = incluirModelo && !modelo.Disponible
                ? " Para estimar platos caseros configurá OPENAI_API_KEY (dotnet user-secrets set \"Nutricion:OpenAiApiKey\" …)."
                : "";
            return ([], estado, "No hubo resultados con kcal." + extra);
        }

        return (resultados, estado, null);
    }

    private static string EstadoDe(IReadOnlyList<string> fuentes)
        => fuentes.Count == 0 ? "" : "Consultado: " + string.Join(" · ", fuentes);

    private static async Task ConsultarAsync(
        IProveedorNutricional proveedor,
        ConsultaPorcion consulta,
        List<SugerenciaNutricional> destino,
        List<string> fuentes,
        CancellationToken ct)
    {
        if (!proveedor.Disponible)
        {
            fuentes.Add($"{proveedor.Nombre}: no configurado");
            return;
        }

        try
        {
            var items = await proveedor.BuscarAsync(consulta, ct);
            fuentes.Add(items.Count == 0
                ? $"{proveedor.Nombre}: sin datos"
                : $"{proveedor.Nombre}: {items.Count}");
            destino.AddRange(items);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            fuentes.Add($"{proveedor.Nombre}: error");
        }
    }
}
