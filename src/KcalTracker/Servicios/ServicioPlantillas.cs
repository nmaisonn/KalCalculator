using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Datos.Plantillas;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

public sealed record ResultadoImportacion(int Creados, int OmitidosPorYaExistir);

/// <summary>
/// Copia una plantilla a la cuenta actual. Lo copiado queda como dato propio,
/// con Origen = Plantilla y sin vínculo con el origen (D-14, RF-G2b).
/// </summary>
public class ServicioPlantillas(KcalDbContext db)
{
    public IReadOnlyList<PlantillaInfo> Listar() => [PlantillaExcel.Info];

    public IReadOnlyList<OpcionParte> Partes() =>
    [
        new(PartePlantilla.TiposComida, "Tipos de comida",
            "Desayuno, media mañana, almuerzo, merienda, cena, extra", true),
        new(PartePlantilla.CategoriasYGrupos, "Categorías y grupos",
            "Vegetales, frutas · pecho, espalda · deporte", true),
        new(PartePlantilla.Alimentos, "Alimentos",
            $"{CatalogoApp.Alimentos.Count} típicos ya están en el catálogo de la app. Importá esta parte solo si querés copias editables de la planilla original.", false),
        new(PartePlantilla.Combos, "Combos",
            "4 combinaciones habituales. Traen los alimentos que usan.", false),
        new(PartePlantilla.Actividades, "Actividades y cardio",
            "Fútbol 5 (~70 min, 550 kcal netas) y los protocolos de cinta. Las kcal del gym van en la rutina.", false),
        new(PartePlantilla.EjerciciosYRutinas, "Ejercicios y rutinas",
            $"{PlantillaExcel.Ejercicios.Count} ejercicios y una rutina de 4 días, con kcal por día", false),
        new(PartePlantilla.Mediciones, "Mediciones",
            $"{PlantillaExcel.Mediciones.Count} mediciones de composición, para calibrar desde el primer día", false),
    ];

    public async Task<ResultadoImportacion> ImportarAsync(
        string plantillaId, IEnumerable<PartePlantilla> partes, CancellationToken ct = default)
    {
        if (plantillaId != PlantillaExcel.Id)
            throw new ArgumentOutOfRangeException(nameof(plantillaId), "No hay una plantilla con ese identificador.");

        if (db.UsuarioIdActual == 0)
            throw new InvalidOperationException("No hay cuenta en sesión para copiar la plantilla.");

        var elegidas = Expandir(partes);
        var creados = 0;
        var omitidos = 0;

        if (elegidas.Contains(PartePlantilla.TiposComida))
            (creados, omitidos) = Sumar(creados, omitidos, await CopiarTiposComidaAsync(ct));

        if (elegidas.Contains(PartePlantilla.CategoriasYGrupos))
            (creados, omitidos) = Sumar(creados, omitidos, await CopiarCategoriasYGruposAsync(ct));

        if (elegidas.Contains(PartePlantilla.Alimentos))
            (creados, omitidos) = Sumar(creados, omitidos, await CopiarAlimentosAsync(ct));

        if (elegidas.Contains(PartePlantilla.Combos))
            (creados, omitidos) = Sumar(creados, omitidos, await CopiarCombosAsync(ct));

        if (elegidas.Contains(PartePlantilla.Actividades))
            (creados, omitidos) = Sumar(creados, omitidos, await CopiarActividadesYCardioAsync(ct));

        if (elegidas.Contains(PartePlantilla.EjerciciosYRutinas))
            (creados, omitidos) = Sumar(creados, omitidos, await CopiarEjerciciosYRutinasAsync(ct));

        if (elegidas.Contains(PartePlantilla.Mediciones))
            (creados, omitidos) = Sumar(creados, omitidos, await CopiarMedicionesAsync(ct));

        return new ResultadoImportacion(creados, omitidos);
    }

    /// <summary>
    /// Actualiza kcal/macros de filas que vinieron de la plantilla, sin tocar
    /// comidas ya registradas ni duplicar catálogo.
    /// </summary>
    public async Task SincronizarValoresAsync(CancellationToken ct = default)
    {
        var alimentos = await db.Alimentos
            .Where(a => a.Origen == OrigenDato.Plantilla && a.UsuarioId == db.UsuarioIdActual)
            .ToListAsync(ct);
        foreach (var a in alimentos)
        {
            var p = PlantillaExcel.Alimentos.FirstOrDefault(x => Mismo(x.Nombre, a.Nombre) && Mismo(x.Porcion, a.Porcion));
            if (p is null) continue;
            a.Kcal = p.Kcal;
            a.Proteina = p.Proteina;
            a.Carbos = p.Carbos;
            a.Grasa = p.Grasa;
            a.EsAfuera = p.EsAfuera;
        }

        var actividades = await db.Actividades.ToListAsync(ct);
        foreach (var a in actividades)
        {
            var p = PlantillaExcel.Actividades.FirstOrDefault(x => Mismo(x.Nombre, a.Nombre));
            if (p is null) continue;
            a.Kcal = p.Kcal;
            a.Minutos = p.Minutos;
        }

        var dias = await db.RutinaDias.ToListAsync(ct);
        foreach (var d in dias)
        {
            var p = PlantillaExcel.Rutinas.SelectMany(r => r.Dias).FirstOrDefault(x => Mismo(x.Nombre, d.Nombre));
            if (p is null) continue;
            d.KcalEstimadas = p.KcalEstimadas;
        }

        var protocolos = await db.ProtocolosCardio.Include(p => p.Bloques).ToListAsync(ct);
        foreach (var proto in protocolos)
        {
            var p = PlantillaExcel.ProtocolosCardio.FirstOrDefault(x => Mismo(x.Nombre, proto.Nombre));
            if (p is null) continue;
            proto.Descripcion = p.Descripcion;
            if (proto.Bloques.Count != p.Bloques.Count) continue;
            foreach (var (bloque, i) in proto.Bloques.OrderBy(b => b.Orden).Select((b, i) => (b, i)))
            {
                var origen = p.Bloques[i];
                bloque.Minutos = origen.Minutos;
                bloque.Velocidad = origen.Velocidad;
                bloque.Inclinacion = origen.Inclinacion;
                bloque.Repeticiones = origen.Repeticiones;
            }
        }

        var anterioresGym = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Día 1 — Espalda + bíceps"] = [330, 300],
            ["Día 2 — Pecho + tríceps"] = [310, 280],
            ["Día 3 — Pierna"] = [390, 410],
            ["Día 4 — Hombro + inclinada + brazos"] = [280, 250]
        };
        var sesionesGym = await db.SesionesGym.ToListAsync(ct);
        foreach (var s in sesionesGym)
        {
            var destino = PlantillaExcel.Rutinas.SelectMany(r => r.Dias)
                .FirstOrDefault(x => Mismo(x.Nombre, s.Nombre));
            if (destino?.KcalEstimadas is null) continue;
            if (s.Kcal is { } k && anterioresGym.TryGetValue(s.Nombre, out var viejos) && viejos.Any(v => Math.Abs(k - v) < 0.5))
                s.Kcal = destino.KcalEstimadas;
        }

        var medicion = await ServicioMediciones.ObtenerReferenciaAsync(db, ct);
        var peso = medicion?.PesoKg ?? 70;

        var cardioAuto = await db.SesionesCardio.Include(s => s.Protocolo).ThenInclude(p => p!.Bloques)
            .Where(s => s.ProtocoloId != null && s.KcalMin == null && s.KcalMax == null)
            .ToListAsync(ct);
        foreach (var s in cardioAuto)
        {
            s.Kcal = s.Protocolo is { } proto
                ? Redondeo.Kcal(EstimacionCardio.DeProtocolo(proto, peso))
                : Redondeo.Kcal(EstimacionCardio.DeSesion(
                    s.Minutos, s.Velocidad, s.Inclinacion, null, peso, null));
        }

        await db.SaveChangesAsync(ct);
    }

    public static HashSet<PartePlantilla> Expandir(IEnumerable<PartePlantilla> partes)
    {
        var set = partes.ToHashSet();
        if (set.Contains(PartePlantilla.Combos))
        {
            set.Add(PartePlantilla.Alimentos);
            set.Add(PartePlantilla.TiposComida);
        }
        if (set.Contains(PartePlantilla.Alimentos))
            set.Add(PartePlantilla.CategoriasYGrupos);
        if (set.Contains(PartePlantilla.Actividades))
            set.Add(PartePlantilla.CategoriasYGrupos);
        if (set.Contains(PartePlantilla.EjerciciosYRutinas))
            set.Add(PartePlantilla.CategoriasYGrupos);
        return set;
    }

    private async Task<(int Creados, int Omitidos)> CopiarTiposComidaAsync(CancellationToken ct)
    {
        var existentes = await db.TiposComida.Select(t => t.Nombre).ToListAsync(ct);
        var creados = 0;
        var omitidos = 0;
        var orden = existentes.Count;
        foreach (var nombre in PlantillaExcel.TiposComida)
        {
            if (existentes.Any(n => Mismo(n, nombre)))
            {
                omitidos++;
                continue;
            }
            db.TiposComida.Add(new TipoComida { Nombre = nombre, Orden = orden++ });
            creados++;
        }
        await db.SaveChangesAsync(ct);
        return (creados, omitidos);
    }

    private async Task<(int Creados, int Omitidos)> CopiarCategoriasYGruposAsync(CancellationToken ct)
    {
        var creados = 0;
        var omitidos = 0;

        (creados, omitidos) = Sumar(creados, omitidos,
            await CopiarCatalogoAsync(db.CategoriasAlimento, PlantillaExcel.CategoriasAlimento,
                (n, o) => new CategoriaAlimento { Nombre = n, Orden = o }, ct));

        (creados, omitidos) = Sumar(creados, omitidos,
            await CopiarCatalogoAsync(db.GruposMusculares, PlantillaExcel.GruposMusculares,
                (n, o) => new GrupoMuscular { Nombre = n, Orden = o }, ct));

        (creados, omitidos) = Sumar(creados, omitidos,
            await CopiarCatalogoAsync(db.TiposActividad, PlantillaExcel.TiposActividad,
                (n, o) => new TipoActividad { Nombre = n, Orden = o }, ct));

        return (creados, omitidos);
    }

    private async Task<(int Creados, int Omitidos)> CopiarCatalogoAsync<T>(
        DbSet<T> set, IReadOnlyList<string> nombres, Func<string, int, T> crear, CancellationToken ct)
        where T : class, ITipoDeCatalogo
    {
        var existentes = await set.Select(t => t.Nombre).ToListAsync(ct);
        var creados = 0;
        var omitidos = 0;
        var orden = existentes.Count;
        foreach (var nombre in nombres)
        {
            if (existentes.Any(n => Mismo(n, nombre)))
            {
                omitidos++;
                continue;
            }
            set.Add(crear(nombre, orden++));
            creados++;
        }
        await db.SaveChangesAsync(ct);
        return (creados, omitidos);
    }

    private async Task<(int Creados, int Omitidos)> CopiarAlimentosAsync(CancellationToken ct)
    {
        var categorias = await db.CategoriasAlimento
            .Where(c => c.UsuarioId == db.UsuarioIdActual)
            .ToListAsync(ct);
        var existentes = await db.Alimentos
            .Where(a => a.UsuarioId == db.UsuarioIdActual)
            .Select(a => new { a.Nombre, a.Porcion }).ToListAsync(ct);
        var creados = 0;
        var omitidos = 0;

        foreach (var a in PlantillaExcel.Alimentos)
        {
            if (existentes.Any(e => Mismo(e.Nombre, a.Nombre) && Mismo(e.Porcion, a.Porcion)))
            {
                omitidos++;
                continue;
            }

            db.Alimentos.Add(new Alimento
            {
                Nombre = a.Nombre,
                Porcion = a.Porcion,
                Kcal = a.Kcal,
                Proteina = a.Proteina,
                Carbos = a.Carbos,
                Grasa = a.Grasa,
                CategoriaAlimentoId = categorias.FirstOrDefault(c => Mismo(c.Nombre, a.Categoria))?.Id,
                EsAfuera = a.EsAfuera,
                Origen = OrigenDato.Plantilla
            });
            creados++;
        }

        await db.SaveChangesAsync(ct);
        return (creados, omitidos);
    }

    private async Task<(int Creados, int Omitidos)> CopiarCombosAsync(CancellationToken ct)
    {
        var tipos = await db.TiposComida.ToListAsync(ct);
        var alimentos = await db.Alimentos.ToListAsync(ct);
        var existentes = await db.Combos.Select(c => c.Nombre).ToListAsync(ct);
        var creados = 0;
        var omitidos = 0;

        foreach (var combo in PlantillaExcel.Combos)
        {
            if (existentes.Any(n => Mismo(n, combo.Nombre)))
            {
                omitidos++;
                continue;
            }

            var items = new List<ComboItem>();
            var incompleto = false;
            foreach (var (nombre, porcion) in combo.Items)
            {
                var alimento = alimentos
                    .Where(a => Mismo(a.Nombre, nombre) && Mismo(a.Porcion, porcion))
                    .OrderBy(a => a.UsuarioId == db.UsuarioIdActual ? 0 : 1)
                    .FirstOrDefault();
                if (alimento is null)
                {
                    incompleto = true;
                    break;
                }
                items.Add(new ComboItem { AlimentoId = alimento.Id, Cantidad = 1 });
            }

            if (incompleto)
            {
                omitidos++;
                continue;
            }

            db.Combos.Add(new Combo
            {
                Nombre = combo.Nombre,
                TipoComidaId = tipos.FirstOrDefault(t => Mismo(t.Nombre, combo.TipoComida))?.Id,
                Items = items
            });
            creados++;
        }

        await db.SaveChangesAsync(ct);
        return (creados, omitidos);
    }

    private async Task<(int Creados, int Omitidos)> CopiarActividadesYCardioAsync(CancellationToken ct)
    {
        var tipos = await db.TiposActividad.ToListAsync(ct);
        var existentesAct = await db.Actividades.Select(a => a.Nombre).ToListAsync(ct);
        var creados = 0;
        var omitidos = 0;

        foreach (var a in PlantillaExcel.Actividades)
        {
            if (existentesAct.Any(n => Mismo(n, a.Nombre)))
            {
                omitidos++;
                continue;
            }

            var tipo = tipos.FirstOrDefault(t => Mismo(t.Nombre, a.Tipo));
            if (tipo is null)
            {
                omitidos++;
                continue;
            }

            db.Actividades.Add(new Actividad
            {
                Nombre = a.Nombre,
                TipoActividadId = tipo.Id,
                Kcal = a.Kcal,
                Minutos = a.Minutos
            });
            creados++;
        }

        var existentesProt = await db.ProtocolosCardio.Select(p => p.Nombre).ToListAsync(ct);
        foreach (var p in PlantillaExcel.ProtocolosCardio)
        {
            if (existentesProt.Any(n => Mismo(n, p.Nombre)))
            {
                omitidos++;
                continue;
            }

            db.ProtocolosCardio.Add(new ProtocoloCardio
            {
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Bloques = p.Bloques.Select((b, i) => new BloqueCardio
                {
                    Orden = i,
                    Minutos = b.Minutos,
                    Velocidad = b.Velocidad,
                    Inclinacion = b.Inclinacion,
                    Repeticiones = b.Repeticiones
                }).ToList()
            });
            creados++;
        }

        await db.SaveChangesAsync(ct);
        return (creados, omitidos);
    }

    private async Task<(int Creados, int Omitidos)> CopiarEjerciciosYRutinasAsync(CancellationToken ct)
    {
        var grupos = await db.GruposMusculares.ToListAsync(ct);
        var existentesEj = await db.Ejercicios.Select(x => x.Nombre).ToListAsync(ct);
        var creados = 0;
        var omitidos = 0;

        foreach (var x in PlantillaExcel.Ejercicios)
        {
            if (existentesEj.Any(n => Mismo(n, x.Nombre)))
            {
                omitidos++;
                continue;
            }

            db.Ejercicios.Add(new Ejercicio
            {
                Nombre = x.Nombre,
                GrupoMuscularId = grupos.FirstOrDefault(g => Mismo(g.Nombre, x.Grupo))?.Id
            });
            creados++;
        }
        await db.SaveChangesAsync(ct);

        var ejercicios = await db.Ejercicios.ToListAsync(ct);
        var existentesRut = await db.Rutinas.Select(r => r.Nombre).ToListAsync(ct);
        var yaHayActiva = await db.Rutinas.AnyAsync(r => r.EsActiva, ct);

        foreach (var rutina in PlantillaExcel.Rutinas)
        {
            if (existentesRut.Any(n => Mismo(n, rutina.Nombre)))
            {
                omitidos++;
                continue;
            }

            db.Rutinas.Add(new Rutina
            {
                Nombre = rutina.Nombre,
                Descripcion = rutina.Descripcion,
                EsActiva = rutina.EsActiva && !yaHayActiva,
                Dias = rutina.Dias.Select((d, iDia) => new RutinaDia
                {
                    Nombre = d.Nombre,
                    Orden = iDia,
                    KcalEstimadas = d.KcalEstimadas,
                    Ejercicios = d.Ejercicios.Select((e, iEj) => new RutinaEjercicio
                    {
                        EjercicioId = ejercicios.Single(x => Mismo(x.Nombre, e.Ejercicio)).Id,
                        Orden = iEj,
                        Series = e.Series,
                        RepsMin = e.RepsMin,
                        RepsMax = e.RepsMax,
                        PesoRef = e.Peso,
                        PuedeMas = e.PuedeMas,
                        Observacion = e.Observacion
                    }).ToList()
                }).ToList()
            });
            if (rutina.EsActiva) yaHayActiva = true;
            creados++;
        }

        await db.SaveChangesAsync(ct);
        return (creados, omitidos);
    }

    private async Task<(int Creados, int Omitidos)> CopiarMedicionesAsync(CancellationToken ct)
    {
        var existentes = await db.Mediciones.Select(m => m.Fecha).ToListAsync(ct);
        var creados = 0;
        var omitidos = 0;

        foreach (var m in PlantillaExcel.Mediciones)
        {
            if (existentes.Contains(m.Fecha))
            {
                omitidos++;
                continue;
            }

            db.Mediciones.Add(new Medicion
            {
                Fecha = m.Fecha,
                PesoKg = m.PesoKg,
                GrasaPct = m.GrasaPct,
                MusculoPct = m.MusculoPct,
                Visceral = m.Visceral,
                Nota = m.Nota
            });
            creados++;
        }

        await db.SaveChangesAsync(ct);
        return (creados, omitidos);
    }

    private static (int Creados, int Omitidos) Sumar(int c, int o, (int Creados, int Omitidos) d)
        => (c + d.Creados, o + d.Omitidos);

    private static bool Mismo(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
