namespace KcalTracker.Dominio;

/// <summary>
/// Marca una entidad que pertenece a una cuenta concreta. Todo tipo que la implemente
/// recibe automáticamente el filtro global de usuario y la asignación de UsuarioId al
/// guardar (D-13, RN-12). Las tablas hijas no la implementan: heredan el usuario de su
/// padre por cascada.
/// </summary>
public interface EntidadDeUsuario
{
    int UsuarioId { get; set; }
}
