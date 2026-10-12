namespace KcalTracker.Datos;

/// <summary>
/// Quién está usando el sistema en esta petición. El contexto de datos depende de esta
/// abstracción y no de HttpContext, para que las pruebas puedan sustituirla y verificar
/// el aislamiento con dos cuentas sobre la misma base (CA-23, CA-24).
/// </summary>
public interface IUsuarioActual
{
    /// <summary>Identificador de la cuenta, o null si nadie ingresó.</summary>
    int? UsuarioId { get; }
}
