namespace KcalTracker.Dominio;

public class Rutina : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }

    /// <summary>A lo sumo una rutina activa por cuenta (RF-J4).</summary>
    public bool EsActiva { get; set; }

    public List<RutinaDia> Dias { get; set; } = [];
}

public class RutinaDia
{
    public int Id { get; set; }

    public int RutinaId { get; set; }
    public Rutina? Rutina { get; set; }

    public string Nombre { get; set; } = "";
    public int Orden { get; set; }

    /// <summary>
    /// Gasto estimado del entrenamiento, que se propone al abrir cada sesión (D-17).
    /// Nulo si el usuario prefiere no estimarlo.
    /// </summary>
    public double? KcalEstimadas { get; set; }

    public List<RutinaEjercicio> Ejercicios { get; set; } = [];
}

/// <summary>Lo que la rutina prescribe. Series y reps van separados, nunca como "4x6/10".</summary>
public class RutinaEjercicio
{
    public int Id { get; set; }

    public int RutinaDiaId { get; set; }
    public RutinaDia? RutinaDia { get; set; }

    public int EjercicioId { get; set; }
    public Ejercicio? Ejercicio { get; set; }

    public int Orden { get; set; }
    public int Series { get; set; }
    public int RepsMin { get; set; }
    public int? RepsMax { get; set; }

    /// <summary>kg. Nulo cuando no aplica un número (RN-13).</summary>
    public double? PesoRef { get; set; }
    public bool PuedeMas { get; set; }
    public string? Observacion { get; set; }
}
