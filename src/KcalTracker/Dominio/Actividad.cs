namespace KcalTracker.Dominio;

/// <summary>
/// Catálogo de actividades que no son gym ni cardio (D-17): un partido, una caminata,
/// andar en bicicleta. Las kcal del entrenamiento viven en la rutina y en la sesión.
/// </summary>
public class Actividad : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public string Nombre { get; set; } = "";
    public int TipoActividadId { get; set; }
    public TipoActividad? TipoActividad { get; set; }
    public double Kcal { get; set; }
    public double? Minutos { get; set; }
    public bool Activo { get; set; } = true;
}

public class ActividadLog : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public DateOnly Fecha { get; set; }

    /// <summary>Referencia informativa. Nula si el catálogo se borró.</summary>
    public int? ActividadId { get; set; }
    public Actividad? Actividad { get; set; }

    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public double Kcal { get; set; }
    public double? Minutos { get; set; }
    /// <summary>0,5 = media actividad. Las kcal se ajustan.</summary>
    public double Cantidad { get; set; } = 1;
}
