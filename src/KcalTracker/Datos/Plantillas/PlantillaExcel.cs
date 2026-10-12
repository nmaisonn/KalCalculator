namespace KcalTracker.Datos.Plantillas;

/// <summary>
/// Catálogo de partida derivado de Alimentación.xlsx, con macros de P-01
/// revisados contra USDA/Atwater y gasto de entrenamiento en MET/ACSM.
/// No se carga solo: hay que importarlo.
/// </summary>
public static class PlantillaExcel
{
    public const string Id = "excel";

    public static PlantillaInfo Info { get; } = new(
        Id,
        "Derivada de Alimentación.xlsx",
        "Alimentos con macros corregidos, combos, fútbol 5, rutina de gym y mediciones. Las kcal del gym van en cada día de la rutina, no como actividad de catálogo.");

    public static IReadOnlyList<string> TiposComida { get; } =
        ["Desayuno", "Media mañana", "Almuerzo", "Merienda", "Cena", "Extra"];

    public static IReadOnlyList<string> TiposActividad { get; } =
        ["Deporte", "Otro"];

    public static IReadOnlyList<string> CategoriasAlimento { get; } =
        ["Comida", "Acompañamiento", "Vegetal", "Fruta", "Extra", "De afuera"];

    public static IReadOnlyList<string> GruposMusculares { get; } =
        ["Pecho", "Espalda", "Pierna", "Hombro", "Bíceps", "Tríceps"];

    public static IReadOnlyList<AlimentoPlantilla> Alimentos { get; } =
    [
        new("Pan Los Sorchantes", "2 rebanadas (50 g)", 127, 4.3, 25, 1.1, "Comida", false),
        new("Requeson light", "18 g", 14, 2.2, 0.6, 0.3, "Comida", false),
        new("Leche descremada", "200 ml", 68, 7, 10, 0.1, "Comida", false),
        new("Queso magro", "29 g", 58, 8, 0, 2.9, "Comida", false),
        new("Manzana verde", "1 (180 g)", 94, 0.5, 22, 0.3, "Fruta", false),
        new("Mandarina", "1 (90 g)", 48, 0.6, 11, 0.2, "Fruta", false),
        new("Yogur Viva Light", "1 vaso (200 g)", 100, 9.9, 15, 0, "Comida", false),
        new("Pechuga s/piel cruda", "250 g", 285, 57, 0, 6.5, "Comida", false),
        new("Cuadril magro crudo", "250 g", 375, 55, 0, 17.5, "Comida", false),
        new("Hígado de vaca", "250 g", 340, 51, 9.8, 9.1, "Comida", false),
        new("Arroz Saman crudo", "50 g", 179, 3.8, 39, 0.3, "Acompañamiento", false),
        new("Arroz Saman crudo", "60 g", 215, 4.5, 47, 0.4, "Acompañamiento", false),
        new("Arroz Saman crudo", "70 g", 251, 5.3, 55, 0.4, "Acompañamiento", false),
        new("Zanahoria hervida", "1 (70 g)", 25, 0.6, 6, 0.2, "Vegetal", false),
        new("Huevo", "1", 72, 6.3, 0.4, 4.8, "Comida", false),
        new("Aceite", "1 cucharadita (5 ml)", 40, 0, 0, 4.4, "Extra", false),

        new("Muzza Requete + 3 fainás", "1", 2250, null, null, null, "De afuera", true),
        new("Muzza Requete + 1 fainá", "1", 1600, null, null, null, "De afuera", true),
        new("Mila napo del club + fritas", "1", 1250, null, null, null, "De afuera", true),
        new("Rudy doble con fritas", "1", 1400, null, null, null, "De afuera", true),
        new("Doble cuarto con fritas", "1", 1120, null, null, null, "De afuera", true),
        new("Doble cuarto con papas grandes", "1", 1280, null, null, null, "De afuera", true),
        new("Mila napo del club sin fritas", "1", 1000, null, null, null, "De afuera", true),
        new("Rudy doble sin fritas", "1", 950, null, null, null, "De afuera", true),
        new("Doble cuarto sin fritas", "1", 780, null, null, null, "De afuera", true),
        new("Tira + medio chorizo", "1", 740, null, null, null, "De afuera", true),
        new("Mila al horno de carne", "1", 700, null, null, null, "De afuera", true),
        new("Mila al horno de pollo", "1", 600, null, null, null, "De afuera", true),
        new("2 empanadas", "2", 490, null, null, null, "De afuera", true),
        new("Sándwich caliente (Sorchantes + jamón + muzza)", "1", 255, null, null, null, "De afuera", true),
        new("Coca 250 ml", "250 ml", 105, null, null, null, "De afuera", true),
        new("Coca McD grande", "700 ml", 295, null, null, null, "De afuera", true),
        new("Palichips", "100 g", 485, null, null, null, "De afuera", true),
        new("Pop Movie mediano salado", "1 balde", 800, null, null, null, "De afuera", true),
        new("Pop Movie chico dulce", "1", 500, null, null, null, "De afuera", true),
        new("Muzza + jamón", "1", 150, null, null, null, "De afuera", true),
        new("Milka Oreo, 2 cuadraditos", "2", 92, null, null, null, "De afuera", true),
    ];

    public static IReadOnlyList<ComboPlantilla> Combos { get; } =
    [
        new("Desayuno completo", "Desayuno",
        [
            ("Pan Los Sorchantes", "2 rebanadas (50 g)"),
            ("Requeson light", "18 g"),
            ("Leche descremada", "200 ml"),
            ("Queso magro", "29 g"),
            ("Manzana verde", "1 (180 g)"),
        ]),
        new("Merienda completa", "Merienda",
        [
            ("Pan Los Sorchantes", "2 rebanadas (50 g)"),
            ("Requeson light", "18 g"),
            ("Leche descremada", "200 ml"),
            ("Yogur Viva Light", "1 vaso (200 g)"),
            ("Mandarina", "1 (90 g)"),
        ]),
        new("Almuerzo/cena pollo", "Almuerzo",
        [
            ("Pechuga s/piel cruda", "250 g"),
            ("Arroz Saman crudo", "60 g"),
            ("Zanahoria hervida", "1 (70 g)"),
            ("Aceite", "1 cucharadita (5 ml)"),
        ]),
        new("Almuerzo/cena cuadril", "Cena",
        [
            ("Cuadril magro crudo", "250 g"),
            ("Arroz Saman crudo", "60 g"),
            ("Zanahoria hervida", "1 (70 g)"),
            ("Aceite", "1 cucharadita (5 ml)"),
        ]),
    ];

    public static IReadOnlyList<ActividadPlantilla> Actividades { get; } =
    [
        new("Fútbol 5", "Deporte", 550, 60),
    ];

    public static IReadOnlyList<EjercicioPlantilla> Ejercicios { get; } =
    [
        new("Jalón", "Espalda"),
        new("Remo con barra T", "Espalda"),
        new("Remo bajo / máquina", "Espalda"),
        new("Deltoides polea", "Hombro"),
        new("Curl barra", "Bíceps"),
        new("Curl martillo", "Bíceps"),
        new("Curl inclinado", "Bíceps"),
        new("Press banca", "Pecho"),
        new("Press inclinado", "Pecho"),
        new("Aperturas", "Pecho"),
        new("Extensión tríceps polea", "Tríceps"),
        new("Trasnuca polea", "Tríceps"),
        new("Sentadilla", "Pierna"),
        new("Peso muerto rumano", "Pierna"),
        new("Prensa", "Pierna"),
        new("Curl femoral", "Pierna"),
        new("Cuádriceps", "Pierna"),
        new("Aductores", "Pierna"),
        new("Vuelos laterales", "Hombro"),
        new("Press militar", "Hombro"),
    ];

    public static IReadOnlyList<RutinaPlantilla> Rutinas { get; } =
    [
        new("Mi rutina", "La de la hoja Ejercicio", true,
        [
            new("Día 1 — Espalda + bíceps", 330,
            [
                new("Jalón", 4, 6, 10, 70, true, null),
                new("Remo con barra T", 3, 8, 10, 45, true, "Duele el trapecio: sustituir por jalón/pullover"),
                new("Remo bajo / máquina", 3, 10, 12, 50, false, "El cable tira los hombros; evitar si el trapecio duele"),
                new("Deltoides polea", 3, 12, 15, 30, false, null),
                new("Curl barra", 3, 8, 10, 10, false, "De pie cuelga del trapecio; predicador/sentado si duele"),
                new("Curl martillo", 2, 10, 12, 10, false, "De pie duele; hacer sentado"),
                new("Curl inclinado", 2, 10, 12, 25, false, "12.5 mancuerna"),
            ]),
            new("Día 2 — Pecho + tríceps", 310,
            [
                new("Press banca", 4, 6, 10, 27.5, true, null),
                new("Press inclinado", 3, 8, 10, 42.5, false, null),
                new("Aperturas", 3, 10, 12, 60, false, null),
                new("Extensión tríceps polea", 3, 8, 12, 45, false, null),
                new("Trasnuca polea", 3, 10, 12, 35, false, null),
            ]),
            new("Día 3 — Pierna", 390,
            [
                new("Sentadilla", 4, 6, 10, 95, false, "Hack/prensa si el cuello duele"),
                new("Peso muerto rumano", 3, 8, 10, 27.5, true, "Saltar si el trapecio duele; femoral sentado/acostado"),
                new("Prensa", 3, 10, 12, 120, false, null),
                new("Curl femoral", 3, 10, 12, 66, true, null),
                new("Cuádriceps", 3, 12, 15, 66, false, null),
                new("Aductores", 2, 12, 12, null, false, "Máximo máquina"),
            ]),
            new("Día 4 — Hombro + inclinada + brazos", 280,
            [
                new("Press inclinado", 3, 8, 10, 32.5, true, null),
                new("Remo con barra T", 3, 8, 10, 40, false, "Saltar si duele el trapecio; usar jalón"),
                new("Jalón", 2, 10, 12, 50, false, null),
                new("Vuelos laterales", 3, 12, 15, 15, false, "4×12 a 15 kg; sentado/polea si el trapecio duele"),
                new("Press militar", 3, 8, 10, 35, false, "Saltar mientras duela el trapecio"),
                new("Extensión tríceps polea", 2, 8, 12, 40, false, null),
                new("Curl barra", 2, 8, 12, 7.5, false, null),
            ]),
        ]),
    ];

    public static IReadOnlyList<ProtocoloPlantilla> ProtocolosCardio { get; } =
    [
        new("Cinta 30 min, 5 km/h al 8 %", "Caminata inclinada. ACSM bruto ~265 kcal a 74 kg.",
        [
            new(30, 5.0, 8, 1),
        ]),
        new("Cinta 30 min, 5 km/h al 10 %", "Caminata inclinada. ACSM bruto ~290 kcal a 74 kg.",
        [
            new(30, 5.0, 10, 1),
        ]),
        new("HIIT jueves", "Intervalos en cinta: trote 9 km/h y caminata de recuperación. El gasto se estima con ACSM al marcar el día.",
        [
            new(5, 5.0, 5, 1),
            new(0.5, 9.0, 5, 6),
            new(1, 4.8, 5, 6),
            new(5, 5.0, 5, 1),
        ]),
    ];

    public static IReadOnlyList<MedicionPlantilla> Mediciones { get; } =
    [
        new(new DateOnly(2022, 11, 4), 71.8, 15.8, 43.3, 6, "Más magro. Referencia vieja."),
        new(new DateOnly(2026, 3, 20), 73.9, 21.8, 39.4, 7, "Inicio dieta + gym."),
        new(new DateOnly(2026, 4, 22), 74.0, 20.6, 40.2, 7),
        new(new DateOnly(2026, 8, 16), 74.1, 18.8, 41.2, 7, "Baseline sano. Usar esta para proyectar (13,9 kg de grasa)."),
        new(new DateOnly(2026, 8, 19), 73.1, 18.0, 41.7, 7, "Tres días después; probable agua."),
        new(new DateOnly(2026, 9, 6), 72.9, 18.4, 41.4, 7, "Post all-inclusive + enfermedad. No baseline."),
        new(new DateOnly(2026, 9, 15), 72.2, 17.2, 42.2, 7, "Tarde, post semana de −4.000 kcal. No comparable 1:1."),
    ];
}
