namespace KcalTracker.Dominio;

/// <summary>Protocolo de intervalos, como el HIIT del jueves de la planilla.</summary>
public class ProtocoloCardio : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    /// <summary>kcal del catálogo. Si está, Hoy las usa al marcar. Si no, se estima por tramos.</summary>
    public double? Kcal { get; set; }
    public bool Activo { get; set; } = true;

    public List<BloqueCardio> Bloques { get; set; } = [];
}

public class BloqueCardio
{
    public int Id { get; set; }

    public int ProtocoloId { get; set; }
    public ProtocoloCardio? Protocolo { get; set; }

    public int Orden { get; set; }
    public double Minutos { get; set; }
    public double? Velocidad { get; set; }
    public double? Inclinacion { get; set; }
    public int Repeticiones { get; set; } = 1;
}

public class SesionCardio : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public DateOnly Fecha { get; set; }

    public int? ProtocoloId { get; set; }
    public ProtocoloCardio? Protocolo { get; set; }

    public double Minutos { get; set; }
    public double? Velocidad { get; set; }
    public double? Inclinacion { get; set; }

    /// <summary>El valor que entra al gasto del día. Si se cargó un rango, es el punto medio (RN-14).</summary>
    public double Kcal { get; set; }
    public double? KcalMin { get; set; }
    public double? KcalMax { get; set; }
    /// <summary>0,5 = medio protocolo. Las kcal y los minutos se ajustan.</summary>
    public double Cantidad { get; set; } = 1;

    public string? Nota { get; set; }
}
