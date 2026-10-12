namespace KcalTracker.Dominio;

/// <summary>El movimiento como concepto, independiente de cualquier rutina.</summary>
public class Ejercicio : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public string Nombre { get; set; } = "";
    public int? GrupoMuscularId { get; set; }
    public GrupoMuscular? GrupoMuscular { get; set; }
    public bool Activo { get; set; } = true;
}
