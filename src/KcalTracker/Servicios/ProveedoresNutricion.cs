using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KcalTracker.Calculo;
using KcalTracker.Dominio;
using Microsoft.Extensions.Options;

namespace KcalTracker.Servicios;

public interface IProveedorNutricional
{
    string Nombre { get; }
    bool Disponible { get; }
    string? MotivoNoDisponible { get; }
    Task<IReadOnlyList<SugerenciaNutricional>> BuscarAsync(ConsultaPorcion consulta, CancellationToken ct = default);
}

public sealed class OpenFoodFactsProveedor(IHttpClientFactory http) : IProveedorNutricional
{
    public string Nombre => "Open Food Facts";
    public bool Disponible => true;
    public string? MotivoNoDisponible => null;

    public async Task<IReadOnlyList<SugerenciaNutricional>> BuscarAsync(ConsultaPorcion consulta, CancellationToken ct = default)
    {
        var cliente = http.CreateClient("off");
        var url = $"cgi/search.pl?search_terms={Uri.EscapeDataString(consulta.Nombre)}&search_simple=1&action=process&json=1&page_size=5";
        using var resp = await cliente.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return [];

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.TryGetProperty("products", out var products))
            return [];

        var lista = new List<SugerenciaNutricional>();
        foreach (var p in products.EnumerateArray())
        {
            var nombre = p.TryGetProperty("product_name", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(nombre)) continue;
            var nutr = p.TryGetProperty("nutriments", out var nu) ? nu : default;
            var s = Armar(nombre, nutr, p.TryGetProperty("serving_size", out var sz) ? sz.GetString() : null, consulta);
            if (s is not null) lista.Add(s);
        }
        return lista;
    }

    private static SugerenciaNutricional? Armar(string nombre, JsonElement nutr, string? servingSize, ConsultaPorcion consulta)
    {
        var kcal100 = Leer(nutr, "energy-kcal_100g");
        var p100 = Leer(nutr, "proteins_100g");
        var c100 = Leer(nutr, "carbohydrates_100g");
        var g100 = Leer(nutr, "fat_100g");
        var kcalServ = Leer(nutr, "energy-kcal_serving");
        var pServ = Leer(nutr, "proteins_serving");
        var cServ = Leer(nutr, "carbohydrates_serving");
        var gServ = Leer(nutr, "fat_serving");

        double kcal;
        double? prot, carbos, grasa;
        string porcion;

        if (consulta.Gramos is { } dest && kcal100 is { } k100)
        {
            (kcal, prot, carbos, grasa) = EscaladoNutricion.AGramos(k100, p100, c100, g100, 100, dest);
            porcion = consulta.TextoPorcion;
        }
        else if (kcalServ is { } ks)
        {
            kcal = ks; prot = pServ; carbos = cServ; grasa = gServ;
            porcion = string.IsNullOrWhiteSpace(servingSize) ? "1 porción" : servingSize;
        }
        else if (kcal100 is { } k)
        {
            kcal = k; prot = p100; carbos = c100; grasa = g100;
            porcion = "100 g";
        }
        else return null;

        var valid = ValidadorSugerencia.Validar(kcal, prot, carbos, grasa, consulta.Gramos);
        return new SugerenciaNutricional(nombre, porcion, kcal, prot, carbos, grasa,
            "Open Food Facts", valid, OrigenDato.OpenFoodFacts);
    }

    private static double? Leer(JsonElement nutr, string clave)
    {
        if (nutr.ValueKind != JsonValueKind.Object) return null;
        if (!nutr.TryGetProperty(clave, out var v)) return null;
        return v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
    }
}

public sealed class UsdaProveedor(IHttpClientFactory http, IOptions<NutricionOpciones> opciones) : IProveedorNutricional
{
    private readonly string? _clave = PrimeraNoVacia(
        opciones.Value.UsdaApiKey,
        Environment.GetEnvironmentVariable("USDA_API_KEY"));

    public string Nombre => "USDA";
    public bool Disponible => !string.IsNullOrWhiteSpace(_clave);
    public string? MotivoNoDisponible => Disponible ? null : "Falta USDA_API_KEY (gratis en fdc.nal.usda.gov).";

    public async Task<IReadOnlyList<SugerenciaNutricional>> BuscarAsync(ConsultaPorcion consulta, CancellationToken ct = default)
    {
        if (!Disponible) return [];
        var cliente = http.CreateClient("usda");
        var url = $"foods/search?query={Uri.EscapeDataString(consulta.Nombre)}&pageSize=5&api_key={Uri.EscapeDataString(_clave!)}";
        using var resp = await cliente.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return [];

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.TryGetProperty("foods", out var foods)) return [];

        var lista = new List<SugerenciaNutricional>();
        foreach (var f in foods.EnumerateArray())
        {
            var nombre = f.TryGetProperty("description", out var d) ? d.GetString() : null;
            if (string.IsNullOrWhiteSpace(nombre)) continue;
            var (kcal, prot, carbos, grasa, origenG) = LeerNutrientes(f);
            if (kcal is null) continue;

            double kcalF = kcal.Value;
            double? p = prot, c = carbos, g = grasa;
            var porcion = origenG.ToString("0.#") + " g";
            if (consulta.Gramos is { } dest)
            {
                (kcalF, p, c, g) = EscaladoNutricion.AGramos(kcalF, p, c, g, origenG, dest);
                porcion = consulta.TextoPorcion;
            }

            var valid = ValidadorSugerencia.Validar(kcalF, p, c, g, consulta.Gramos);
            lista.Add(new SugerenciaNutricional(nombre, porcion, kcalF, p, c, g,
                "USDA FoodData Central", valid, OrigenDato.Usda));
        }
        return lista;
    }

    private static (double? Kcal, double? Proteina, double? Carbos, double? Grasa, double Gramos) LeerNutrientes(JsonElement food)
    {
        double? kcal = null, prot = null, carbos = null, grasa = null;
        if (food.TryGetProperty("foodNutrients", out var nuts))
        {
            foreach (var n in nuts.EnumerateArray())
            {
                var name = n.TryGetProperty("nutrientName", out var nn) ? nn.GetString() ?? "" : "";
                var unit = n.TryGetProperty("unitName", out var u) ? u.GetString() ?? "" : "";
                var val = n.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : (double?)null;
                if (val is null) continue;
                if (name.Equals("Energy", StringComparison.OrdinalIgnoreCase) && unit.Equals("KCAL", StringComparison.OrdinalIgnoreCase))
                    kcal = val;
                else if (name.Equals("Protein", StringComparison.OrdinalIgnoreCase))
                    prot = val;
                else if (name.StartsWith("Carbohydrate", StringComparison.OrdinalIgnoreCase))
                    carbos ??= val;
                else if (name.Equals("Total lipid (fat)", StringComparison.OrdinalIgnoreCase) || name.Equals("Fat", StringComparison.OrdinalIgnoreCase))
                    grasa = val;
            }
        }

        var gramos = 100.0;
        var tipo = food.TryGetProperty("dataType", out var dt) ? dt.GetString() : null;
        if (string.Equals(tipo, "Branded", StringComparison.OrdinalIgnoreCase)
            && food.TryGetProperty("servingSize", out var ss) && ss.ValueKind == JsonValueKind.Number)
        {
            var unidad = food.TryGetProperty("servingSizeUnit", out var su) ? su.GetString() ?? "g" : "g";
            if (unidad.Equals("g", StringComparison.OrdinalIgnoreCase) || unidad.Equals("ml", StringComparison.OrdinalIgnoreCase))
                gramos = ss.GetDouble();
        }
        return (kcal, prot, carbos, grasa, gramos);
    }

    private static string? PrimeraNoVacia(params string?[] xs)
        => xs.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
}

public sealed class ModeloLenguajeProveedor(IHttpClientFactory http, IOptions<NutricionOpciones> opciones) : IProveedorNutricional
{
    private readonly NutricionOpciones _opt = opciones.Value;
    private readonly string? _clave = PrimeraNoVacia(
        opciones.Value.OpenAiApiKey,
        Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

    public string Nombre => "modelo de lenguaje";
    public bool Disponible => !string.IsNullOrWhiteSpace(_clave);
    public string? MotivoNoDisponible => Disponible
        ? null
        : "Falta OPENAI_API_KEY. Sin ella no se pueden estimar platos caseros.";

    public async Task<IReadOnlyList<SugerenciaNutricional>> BuscarAsync(ConsultaPorcion consulta, CancellationToken ct = default)
    {
        if (!Disponible) return [];

        var cliente = http.CreateClient("openai");
        var baseUrl = string.IsNullOrWhiteSpace(_opt.OpenAiBaseUrl)
            ? "https://api.openai.com/v1/"
            : _opt.OpenAiBaseUrl.TrimEnd('/') + "/";
        var url = baseUrl + "chat/completions";
        var azure = baseUrl.Contains("openai.azure.com", StringComparison.OrdinalIgnoreCase);

        var pedido = new
        {
            model = string.IsNullOrWhiteSpace(_opt.OpenAiModel) ? "gpt-4o-mini" : _opt.OpenAiModel,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = consulta.Gramos is { } g
                    ? $"Alimento: {consulta.Nombre}. Cantidad: {g} g ({consulta.TextoPorcion})."
                    : $"Alimento y cantidad: {consulta.Nombre}." }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = new StringContent(JsonSerializer.Serialize(pedido), Encoding.UTF8, "application/json");
        if (azure) req.Headers.TryAddWithoutValidation("api-key", _clave);
        else req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _clave);

        using var resp = await cliente.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return [];

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content)) return [];
        content = content.Trim();
        if (content.StartsWith("```"))
        {
            var ini = content.IndexOf('{');
            var fin = content.LastIndexOf('}');
            if (ini >= 0 && fin > ini) content = content[ini..(fin + 1)];
        }

        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;
        var nombre = root.TryGetProperty("nombre", out var nom) ? nom.GetString() : consulta.Nombre;
        if (string.IsNullOrWhiteSpace(nombre)) nombre = consulta.Nombre;
        var porcion = root.TryGetProperty("porcion", out var por) ? por.GetString() : consulta.TextoPorcion;
        if (string.IsNullOrWhiteSpace(porcion)) porcion = consulta.TextoPorcion;
        var kcal = Num(root, "kcal");
        if (kcal is null or <= 0) return [];
        var prot = Num(root, "proteina");
        var carbos = Num(root, "carbos");
        var grasa = Num(root, "grasa");
        var notas = root.TryGetProperty("notas", out var nt) ? nt.GetString() : null;
        var valid = ValidadorSugerencia.Validar(kcal.Value, prot, carbos, grasa, consulta.Gramos);
        var detalle = string.IsNullOrWhiteSpace(notas)
            ? "Es una estimación de un modelo, no una etiqueta. Revisala antes de aceptar."
            : notas + " Es una estimación, no una etiqueta.";
        valid = valid with { Detalle = valid.Detalle + " " + detalle };

        return
        [
            new SugerenciaNutricional(nombre, porcion!, kcal.Value, prot, carbos, grasa,
                "estimación del modelo", valid, OrigenDato.ModeloLenguaje, true, notas)
        ];
    }

    private const string SystemPrompt = """
        Sos un asistente nutricional. Estimá kcal, proteína, carbohidratos y grasa
        de un alimento o plato casero para la cantidad pedida (no para 100 g salvo que pidan 100 g).
        Respondé SOLO un JSON con: nombre (string), porcion (string, ej. "250 g"),
        kcal (number), proteina (number, g), carbos (number, g), grasa (number, g),
        notas (string breve, supuestos de receta).
        Las kcal deben ser coherentes con 4*proteina + 4*carbos + 9*grasa (±5 %).
        Si el plato es ambiguo, asumí la preparación casera más habitual en el Río de la Plata.
        """;

    private static double? Num(JsonElement root, string clave)
    {
        if (!root.TryGetProperty(clave, out var v)) return null;
        return v.ValueKind == JsonValueKind.Number ? v.GetDouble()
            : double.TryParse(v.GetString()?.Replace(',', '.'), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static string? PrimeraNoVacia(params string?[] xs)
        => xs.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
}
