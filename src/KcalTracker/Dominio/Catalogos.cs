namespace KcalTracker.Dominio;

public class TipoComida : ITipoDeCatalogo
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}

public class TipoActividad : ITipoDeCatalogo
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}

public class CategoriaAlimento : ITipoDeCatalogo
{
    public const string NombreAfuera = "De afuera";

    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;

    public static bool EsAfuera(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return false;
        var n = nombre.Trim().ToLowerInvariant();
        return n is "de afuera" or "afuera" or "restaurante";
    }
}

public class GrupoMuscular : ITipoDeCatalogo
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}
