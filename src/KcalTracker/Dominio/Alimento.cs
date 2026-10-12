namespace KcalTracker.Dominio;

public enum OrigenDato
{
    Plantilla = 0,
    Manual = 1,
    OpenFoodFacts = 2,
    Usda = 3,
    ModeloLenguaje = 4
}

public class Alimento : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public string Nombre { get; set; } = "";

    /// <summary>Porción de referencia tal como la escribe el usuario: "2 rebanadas (50 g)".</summary>
    public string Porcion { get; set; } = "";

    /// <summary>Kcal por porción.</summary>
    public double Kcal { get; set; }

    // Nulo significa desconocido, no cero (RN-09). Un alimento sin proteína declarada
    // no aporta 0 g al total: aporta un total que no se puede afirmar.
    public double? Proteina { get; set; }
    public double? Carbos { get; set; }
    public double? Grasa { get; set; }

    public int? CategoriaAlimentoId { get; set; }
    public CategoriaAlimento? CategoriaAlimento { get; set; }

    /// <summary>
    /// Tramo habitual (almuerzo, cena…). Organiza el catálogo; no impide usarlo en otro slot.
    /// </summary>
    public int? TipoComidaId { get; set; }
    public TipoComida? TipoComida { get; set; }

    public bool EsAfuera { get; set; }
    public bool Activo { get; set; } = true;

    public OrigenDato Origen { get; set; } = OrigenDato.Manual;

    /// <summary>Cuándo se obtuvo el dato, cuando el origen es externo.</summary>
    public DateTime? ObtenidoEl { get; set; }
}
