namespace KcalTracker.Dominio;

public interface ITipoDeCatalogo : EntidadDeUsuario
{
    int Id { get; set; }
    string Nombre { get; set; }
    int Orden { get; set; }
    bool Activo { get; set; }
}
