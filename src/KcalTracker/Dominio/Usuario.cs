namespace KcalTracker.Dominio;

public class Usuario
{
    public int Id { get; set; }

    /// <summary>Normalizado a minúsculas antes de guardar. Único.</summary>
    public string Email { get; set; } = "";

    /// <summary>Derivación PBKDF2 con sal. Nunca la contraseña.</summary>
    public string HashClave { get; set; } = "";

    public string? Nombre { get; set; }

    public DateTime CreadoEl { get; set; }

    /// <summary>
    /// Tras el registro se muestra P16 una sola vez. Queda en falso al omitir o importar.
    /// </summary>
    public bool BienvenidaPendiente { get; set; }
}
