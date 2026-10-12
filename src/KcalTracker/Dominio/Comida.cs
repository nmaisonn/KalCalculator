namespace KcalTracker.Dominio;

public class Comida : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    /// <summary>Fecha sin hora. La ausencia de fila es lo que representa un día sin registro (RN-07).</summary>
    public DateOnly Fecha { get; set; }

    public int TipoComidaId { get; set; }
    public TipoComida? TipoComida { get; set; }

    public string? Nota { get; set; }

    public List<ComidaItem> Items { get; set; } = [];
}

public class ComidaItem
{
    public int Id { get; set; }

    public int ComidaId { get; set; }
    public Comida? Comida { get; set; }

    /// <summary>Referencia al alimento. Si se corrige el catálogo, las copias de esta línea se actualizan.</summary>
    public int? AlimentoId { get; set; }
    public Alimento? Alimento { get; set; }

    /// <summary>Porciones de receta al cargar. La fracción 1 / 50 % se aplica sobre esta base.</summary>
    public double CantidadBase { get; set; } = 1;

    /// <summary>Número de porciones de esta línea (base × fracción del ítem × fracción del plato).</summary>
    public double Cantidad { get; set; }

    /// <summary>Ítems cargados juntos (un combo). Permite recortar el plato entero.</summary>
    public string? GrupoId { get; set; }
    public string? GrupoNombre { get; set; }
    public double GrupoFactor { get; set; } = 1;

    // Copia de trabajo del alimento (nombre, porción, kcal, macros). Al cargar se toma
    // del catálogo; si después se edita el alimento y hay AlimentoId, se vuelve a copiar.
    public string Nombre { get; set; } = "";
    public string Porcion { get; set; } = "";
    public double Kcal { get; set; }
    public double? Proteina { get; set; }
    public double? Carbos { get; set; }
    public double? Grasa { get; set; }
    public bool EsAfuera { get; set; }
}
