namespace KcalTracker.Dominio;

public enum Sexo
{
    Masculino = 0,
    Femenino = 1
}

/// <summary>
/// Qué se busca con la energía del día. Cambia el signo (déficit vs superávit)
/// y la proteína sugerida.
/// </summary>
public enum TipoObjetivo
{
    Corte = 0,
    Recomposicion = 1,
    GananciaMusculo = 2
}

/// <summary>
/// De dónde sale el desplegable de alimentos. El catálogo de la app es de solo lectura.
/// </summary>
public enum FuenteAlimentos
{
    Propios = 0,
    Catalogo = 1,
    Ambos = 2
}

public static class FuenteAlimentosNombres
{
    public static string De(FuenteAlimentos fuente) => fuente switch
    {
        FuenteAlimentos.Catalogo => "Catálogo de la app",
        FuenteAlimentos.Ambos => "Los míos y el catálogo",
        _ => "Solo los míos"
    };
}

public static class TipoObjetivoNombres
{
    public static string De(TipoObjetivo tipo) => tipo switch
    {
        TipoObjetivo.Recomposicion => "Recomposición",
        TipoObjetivo.GananciaMusculo => "Ganancia de músculo",
        _ => "Corte"
    };

    public static string Detalle(TipoObjetivo tipo) => tipo switch
    {
        TipoObjetivo.Recomposicion => "Bajar grasa y sumar músculo con un recorte chico del gasto; el % entra si ese ritmo alcanza",
        TipoObjetivo.GananciaMusculo => "Superávit para hipertrofia; el peso no baja",
        _ => "Bajar de peso con un recorte del gasto del día, según tu composición"
    };
}

/// <summary>
/// Una fila por cuenta. Solo el factor de actividad y el plazo son obligatorios,
/// para poder usar el sistema sin completar el resto.
/// </summary>
public class Perfil : EntidadDeUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    public double? AlturaCm { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public Sexo? Sexo { get; set; }

    /// <summary>Solo actividad no deportiva. El ejercicio se suma aparte (RN-02).</summary>
    public double FactorActividad { get; set; } = ValoresDeFabrica.FactorActividad;

    public double? GrasaObjetivoPct { get; set; }
    public int PlazoSemanas { get; set; } = ValoresDeFabrica.PlazoSemanas;
    /// <summary>Cuándo arrancó este plazo. Si es null, las semanas se cuentan completas.</summary>
    public DateOnly? FechaInicioObjetivo { get; set; }
    public double? ProteinaObjetivoG { get; set; }

    public TipoObjetivo TipoObjetivo { get; set; } = TipoObjetivo.Corte;
    /// <summary>Si hay un número, pisa el déficit o superávit calculado. Siempre positivo.</summary>
    public double? DeficitDiarioKcal { get; set; }

    /// <summary>
    /// Qué alimentos aparecen en Hoy y en el catálogo. El histórico no cambia:
    /// una comida ya cargada sigue visible aunque dejes de ver el catálogo.
    /// </summary>
    public FuenteAlimentos FuenteAlimentos { get; set; } = FuenteAlimentos.Propios;
}
