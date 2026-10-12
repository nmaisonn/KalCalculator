namespace KcalTracker.Datos;

/// <summary>
/// Identificador de la cuenta que posee el catálogo de alimentos de la app.
/// Cero: todavía no se sembró, el filtro no incluye filas extra.
/// </summary>
public sealed class CatalogoCompartido
{
    public const string Email = "catalogo@kcal-tracker.internal";

    public int Id { get; set; }
}
