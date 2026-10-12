namespace KcalTracker.Dominio;

/// <summary>Lo que realmente se levantó ese día. Editar la rutina no cambia esto (CA-34).</summary>
public class SesionGym : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public DateOnly Fecha { get; set; }

    /// <summary>Nulo si fue una sesión libre, sin rutina de origen.</summary>
    public int? RutinaDiaId { get; set; }
    public RutinaDia? RutinaDia { get; set; }

    /// <summary>Copia del nombre del día de rutina al momento de entrenar.</summary>
    public string Nombre { get; set; } = "";

    /// <summary>Se propone desde <see cref="RutinaDia.KcalEstimadas"/> y queda editable para este día.</summary>
    public double? Kcal { get; set; }

    /// <summary>Porciones de la sesión: 0,5 es media rutina. Las kcal y las series se ajustan.</summary>
    public double Cantidad { get; set; } = 1;

    public string? Nota { get; set; }

    public List<SesionEjercicio> Ejercicios { get; set; } = [];
}

public class SesionEjercicio
{
    public int Id { get; set; }

    public int SesionGymId { get; set; }
    public SesionGym? SesionGym { get; set; }

    public int? EjercicioId { get; set; }
    public Ejercicio? Ejercicio { get; set; }

    public int Orden { get; set; }
    public string Nombre { get; set; } = "";
    public string? GrupoMuscular { get; set; }
    public int Series { get; set; }
    public int? Reps { get; set; }
    public double? Peso { get; set; }
    public bool PuedeMas { get; set; }
    public string? Observacion { get; set; }

    public List<SerieRealizada> SeriesDetalle { get; set; } = [];
}

/// <summary>Desglose opcional cuando las series no fueron iguales entre sí (RF-K4).</summary>
public class SerieRealizada
{
    public int Id { get; set; }

    public int SesionEjercicioId { get; set; }
    public SesionEjercicio? SesionEjercicio { get; set; }

    public int Numero { get; set; }
    public int Reps { get; set; }
    public double? Peso { get; set; }
}
