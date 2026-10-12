namespace KcalTracker.Dominio;

public class Combo : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public string Nombre { get; set; } = "";
    public int? TipoComidaId { get; set; }
    public TipoComida? TipoComida { get; set; }
    public bool Activo { get; set; } = true;

    public List<ComboItem> Items { get; set; } = [];
}

/// <summary>
/// A diferencia de <see cref="ComidaItem"/>, acá no se copian macros: un combo es
/// una plantilla y debe reflejar el catálogo vigente cuando se usa.
/// </summary>
public class ComboItem
{
    public int Id { get; set; }

    public int ComboId { get; set; }
    public Combo? Combo { get; set; }

    public int AlimentoId { get; set; }
    public Alimento? Alimento { get; set; }

    public double Cantidad { get; set; }
}
