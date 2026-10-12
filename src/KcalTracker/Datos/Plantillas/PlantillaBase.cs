namespace KcalTracker.Datos.Plantillas;

/// <summary>Qué se puede copiar de una plantilla. La elección es granular (P16, RF-I8).</summary>
public enum PartePlantilla
{
    TiposComida,
    CategoriasYGrupos,
    Alimentos,
    Combos,
    Actividades,
    EjerciciosYRutinas,
    Mediciones
}

public sealed record PlantillaInfo(string Id, string Nombre, string Descripcion);

public sealed record OpcionParte(
    PartePlantilla Parte,
    string Titulo,
    string Detalle,
    bool MarcadaPorDefecto);

public sealed record AlimentoPlantilla(
    string Nombre,
    string Porcion,
    double Kcal,
    double? Proteina,
    double? Carbos,
    double? Grasa,
    string Categoria,
    bool EsAfuera);

public sealed record ComboPlantilla(
    string Nombre,
    string TipoComida,
    IReadOnlyList<(string Nombre, string Porcion)> Items);

public sealed record ActividadPlantilla(string Nombre, string Tipo, double Kcal, double? Minutos);

public sealed record EjercicioPlantilla(string Nombre, string Grupo);

public sealed record PrescripcionPlantilla(
    string Ejercicio,
    int Series,
    int RepsMin,
    int? RepsMax,
    double? Peso,
    bool PuedeMas,
    string? Observacion);

public sealed record DiaPlantilla(
    string Nombre,
    double? KcalEstimadas,
    IReadOnlyList<PrescripcionPlantilla> Ejercicios);

public sealed record RutinaPlantilla(
    string Nombre,
    string? Descripcion,
    bool EsActiva,
    IReadOnlyList<DiaPlantilla> Dias);

public sealed record BloquePlantilla(
    double Minutos,
    double? Velocidad,
    double? Inclinacion,
    int Repeticiones);

public sealed record ProtocoloPlantilla(
    string Nombre,
    string? Descripcion,
    IReadOnlyList<BloquePlantilla> Bloques);

public sealed record MedicionPlantilla(
    DateOnly Fecha,
    double PesoKg,
    double? GrasaPct,
    double? MusculoPct,
    double? Visceral,
    string? Nota = null);
