namespace KcalTracker.Dominio;

/// <summary>
/// Composición corporal en una fecha. GrasaKg y MagraKg no se guardan: se derivan
/// al leer, que es lo que evita la inconsistencia P-12 de la planilla.
/// </summary>
public class Medicion : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public DateOnly Fecha { get; set; }
    public double PesoKg { get; set; }
    public double? GrasaPct { get; set; }
    public double? MusculoPct { get; set; }
    public double? Visceral { get; set; }

    /// <summary>
    /// Si es true, esta medición manda para BMR y el plan, aunque haya otra más nueva
    /// (p. ej. una pesada de tarde o post-enfermedad).
    /// </summary>
    public bool EsReferencia { get; set; }

    /// <summary>
    /// Pesada en las mismas condiciones (ayunas, misma hora). Si es false se guarda
    /// pero no entra en la comparación con la báscula ni en la calibración.
    /// </summary>
    public bool EsComparable { get; set; } = true;

    public string? Nota { get; set; }
}
