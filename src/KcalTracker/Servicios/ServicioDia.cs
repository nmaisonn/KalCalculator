using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

public sealed record GrupoComida(TipoComida Tipo, Comida Comida, double Subtotal);

public sealed record MarcaEntreno(int Id, double Kcal, double Cantidad);

public sealed record EntrenoSuelto(int Id, string Nombre, double Kcal);

public sealed record VistaHoy(
    DateOnly Fecha,
    ResultadoDia Balance,
    ResultadoBmr? Bmr,
    double? GastoSinDeporte,
    Medicion? Medicion,
    IReadOnlyList<GrupoComida> Comidas,
    IReadOnlyList<TipoComida> Tipos,
    IReadOnlyList<Combo> Combos,
    IReadOnlyList<Alimento> Frecuentes,
    IReadOnlyList<Alimento> Afuera,
    IReadOnlyList<Alimento> Busqueda,
    IReadOnlyList<Actividad> Actividades,
    IReadOnlyList<int> ActividadesDelDia,
    IReadOnlyList<RutinaDia> DiasRutina,
    IReadOnlyList<int> SesionesGymDelDia,
    IReadOnlyList<ProtocoloCardio> Protocolos,
    IReadOnlyList<int> CardioDelDia,
    ResultadoPlan? Plan,
    ResultadoDeficit? Deficit,
    IReadOnlyList<MarcaEntreno> GymMarcas,
    IReadOnlyList<MarcaEntreno> CardioMarcas,
    IReadOnlyList<MarcaEntreno> DeporteMarcas,
    IReadOnlyList<EntrenoSuelto> GymSueltos,
    IReadOnlyList<EntrenoSuelto> CardioSueltos,
    IReadOnlyList<EntrenoSuelto> DeporteSueltos,
    bool FechaFutura,
    IReadOnlyList<Alimento> Alimentos,
    double FactorActividad);

public class ServicioDia(KcalDbContext db)
{
    public async Task<(Medicion? Medicion, ResultadoBmr? Bmr, double? GastoSinDeporte, Perfil Perfil, Configuracion Cfg)> BaseAsync(CancellationToken ct = default)
    {
        var cfg = await db.Configuraciones.SingleAsync(ct);
        var perfil = await db.Perfiles.SingleAsync(ct);
        var medicion = await ServicioMediciones.ObtenerReferenciaAsync(db, ct);
        if (medicion is null)
            return (null, null, null, perfil, cfg);

        var edad = perfil.FechaNacimiento is { } nac ? Metabolismo.Edad(nac, medicion.Fecha) : null;
        var bmr = Metabolismo.Calcular(cfg.MetodoBmr, cfg.BmrMedido, medicion.PesoKg, medicion.GrasaPct,
            perfil.AlturaCm, edad, perfil.Sexo);
        var gasto = Metabolismo.GastoSinDeporte(bmr, perfil.FactorActividad);
        return (medicion, bmr, gasto, perfil, cfg);
    }

    public async Task<VistaHoy> CargarAsync(DateOnly fecha, string? busqueda = null, CancellationToken ct = default)
    {
        var (medicion, bmr, gastoSin, perfil, cfg) = await BaseAsync(ct);
        var tipos = await db.TiposComida.Where(t => t.Activo).OrderBy(t => t.Orden).ToListAsync(ct);
        var comidas = await db.Comidas.Include(c => c.TipoComida).Include(c => c.Items)
            .Where(c => c.Fecha == fecha).ToListAsync(ct);
        var grupos = tipos
            .Select(t =>
            {
                var c = comidas.FirstOrDefault(x => x.TipoComidaId == t.Id);
                return c is null ? null : new GrupoComida(t, c, c.Items.Sum(i => i.Kcal * i.Cantidad));
            })
            .Where(g => g is not null)
            .Cast<GrupoComida>()
            .ToList();

        var items = comidas.SelectMany(c => c.Items).Select(i => new ItemIngesta(i.Cantidad, i.Kcal, i.Proteina, i.Carbos, i.Grasa));
        var logs = await db.ActividadLogs.Where(l => l.Fecha == fecha).ToListAsync(ct);
        var gym = await db.SesionesGym.Where(s => s.Fecha == fecha).ToListAsync(ct);
        var cardio = await db.SesionesCardio.Where(s => s.Fecha == fecha).ToListAsync(ct);

        var gastoBase = gastoSin ?? 0;
        var balance = BalanceDiario.DelDia(
            gastoBase, items,
            logs.Select(l => l.Kcal),
            gym.Where(s => s.Kcal is not null).Select(s => s.Kcal!.Value),
            cardio.Select(s => s.Kcal));

        ResultadoPlan? plan = null;
        ResultadoDeficit? deficit = null;
        if (medicion is not null && gastoSin is { } g)
        {
            plan = PlanObjetivo.De(medicion.PesoKg, medicion.GrasaPct, perfil.GrasaObjetivoPct,
                perfil.PlazoSemanas, cfg.KcalPorKgGrasa, g, cfg,
                perfil.FechaInicioObjetivo);
        }
        deficit = DeficitObjetivo.De(plan, gastoSin, medicion?.GrasaPct, perfil.TipoObjetivo, perfil.DeficitDiarioKcal);

        var busquedaQ = busqueda?.Trim() ?? "";
        await new ServicioCatalogos(db).AsegurarAfueraAsync(ct);
        var alimentos = await CatalogoAlimentos.CargarAsync(
            db.Alimentos.Include(a => a.CategoriaAlimento).Where(a => a.Activo),
            db.UsuarioIdActual, db.UsuarioIdCatalogo, perfil.FuenteAlimentos, ct);
        var frecuentesIds = await db.ComidaItems
            .Where(i => i.AlimentoId != null)
            .GroupBy(i => i.AlimentoId)
            .OrderByDescending(g => g.Count())
            .Take(8)
            .Select(g => g.Key)
            .ToListAsync(ct);

        var frecuentes = frecuentesIds.Count == 0
            ? alimentos.Where(a => !a.EsAfuera).OrderBy(a => a.Nombre).Take(8).ToList()
            : alimentos.Where(a => frecuentesIds.Contains(a.Id)).ToList();

        var rutina = await db.Rutinas.Include(r => r.Dias).FirstOrDefaultAsync(r => r.EsActiva, ct);

        return new VistaHoy(
            fecha, balance, bmr, gastoSin, medicion, grupos, tipos,
            await db.Combos.Include(c => c.Items).ThenInclude(i => i.Alimento).Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(ct),
            frecuentes,
            alimentos.Where(a => a.EsAfuera).OrderBy(a => a.Nombre).ToList(),
            busquedaQ.Length == 0 ? [] : alimentos.Where(a => a.Nombre.Contains(busquedaQ)).OrderBy(a => a.Nombre).Take(20).ToList(),
            await db.Actividades.Include(a => a.TipoActividad).Where(a => a.Activo).OrderBy(a => a.Nombre).ToListAsync(ct),
            logs.Where(l => l.ActividadId is not null).Select(l => l.ActividadId!.Value).ToList(),
            rutina?.Dias.OrderBy(d => d.Orden).ToList() ?? [],
            gym.Where(s => s.RutinaDiaId is not null).Select(s => s.RutinaDiaId!.Value).ToList(),
            await db.ProtocolosCardio.Include(p => p.Bloques).Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync(ct),
            cardio.Where(s => s.ProtocoloId is not null).Select(s => s.ProtocoloId!.Value).ToList(),
            plan,
            deficit,
            gym.Where(s => s.RutinaDiaId is not null)
                .Select(s => new MarcaEntreno(s.RutinaDiaId!.Value, s.Kcal ?? 0, s.Cantidad <= 0 ? 1 : s.Cantidad)).ToList(),
            cardio.Where(s => s.ProtocoloId is not null)
                .Select(s => new MarcaEntreno(s.ProtocoloId!.Value, s.Kcal, s.Cantidad <= 0 ? 1 : s.Cantidad)).ToList(),
            logs.Where(l => l.ActividadId is not null)
                .Select(l => new MarcaEntreno(l.ActividadId!.Value, l.Kcal, l.Cantidad <= 0 ? 1 : l.Cantidad)).ToList(),
            gym.Where(s => s.RutinaDiaId is null)
                .Select(s => new EntrenoSuelto(s.Id, string.IsNullOrWhiteSpace(s.Nombre) ? "Gym" : s.Nombre, s.Kcal ?? 0)).ToList(),
            cardio.Where(s => s.ProtocoloId is null)
                .Select(s => new EntrenoSuelto(s.Id, string.IsNullOrWhiteSpace(s.Nota) ? "Cardio" : s.Nota, s.Kcal)).ToList(),
            logs.Where(l => l.ActividadId is null)
                .Select(l => new EntrenoSuelto(l.Id, string.IsNullOrWhiteSpace(l.Nombre) ? "Deporte" : l.Nombre, l.Kcal)).ToList(),
            fecha > DateOnly.FromDateTime(DateTime.Today),
            alimentos.OrderBy(a => a.Nombre).ToList(),
            perfil.FactorActividad);
    }

    public async Task<string?> AgregarAlimentoAsync(DateOnly fecha, int tipoComidaId, int alimentoId, double cantidad = 1, CancellationToken ct = default)
    {
        var alimento = await db.Alimentos.FirstOrDefaultAsync(a => a.Id == alimentoId && a.Activo, ct);
        if (alimento is null) return "Ese alimento no está.";
        var comida = await ObtenerOCrearComidaAsync(fecha, tipoComidaId, ct);
        comida.Items.Add(Copiar(alimento, cantidad));
        await db.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> AgregarComboAsync(DateOnly fecha, int comboId, int? tipoComidaId = null, CancellationToken ct = default)
    {
        var combo = await db.Combos.Include(c => c.Items).ThenInclude(i => i.Alimento)
            .FirstOrDefaultAsync(c => c.Id == comboId, ct);
        if (combo is null) return "Ese combo no está.";
        var tipos = await db.TiposComida.Where(t => t.Activo).OrderBy(t => t.Orden).ToListAsync(ct);
        var tipoId = tipoComidaId ?? combo.TipoComidaId ?? tipos.FirstOrDefault()?.Id;
        if (tipoId is null) return "Definí un tipo de comida en Ajustes antes de cargar.";
        if (tipos.All(t => t.Id != tipoId.Value))
            return "Ese tipo de comida no está.";
        var comida = await ObtenerOCrearComidaAsync(fecha, tipoId.Value, ct);
        var grupo = Guid.NewGuid().ToString("N");
        foreach (var item in combo.Items)
        {
            if (item.Alimento is null) continue;
            comida.Items.Add(Copiar(item.Alimento, item.Cantidad, grupo, combo.Nombre));
        }
        await db.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> AgregarEleccionAsync(DateOnly fecha, int tipoComidaId, string? eleccion, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eleccion)) return null;
        var partes = eleccion.Split(':', 2);
        if (partes.Length != 2 || !int.TryParse(partes[1], out var id) || id <= 0)
            return "No se reconoció esa opción.";
        return partes[0] switch
        {
            "c" => await AgregarComboAsync(fecha, id, tipoComidaId, ct),
            "a" => await AgregarAlimentoAsync(fecha, tipoComidaId, id, 1, ct),
            _ => "No se reconoció esa opción."
        };
    }

    /// <summary>
    /// El modal de puntual manda nombre/kcal. Si el desplegable del tramo viajó
    /// en el mismo POST, no tiene que robarse el alta: el extra gana.
    /// </summary>
    public Task<string?> AgregarSlotAsync(
        DateOnly fecha, int tipoComidaId, string? eleccion, double kcal = 0, string? nombre = null,
        CancellationToken ct = default)
        => kcal >= 1 || !string.IsNullOrWhiteSpace(nombre)
            ? AgregarKcalComidaAsync(fecha, tipoComidaId, kcal, nombre, ct)
            : AgregarEleccionAsync(fecha, tipoComidaId, eleccion, ct);

    public async Task<string?> AgregarKcalComidaAsync(
        DateOnly fecha, int tipoComidaId, double kcal, string? nombre = null, CancellationToken ct = default)
    {
        if (kcal is < 1 or > 20000)
            return "Poné las kcal (1 a 20.000) o elegí un alimento.";
        var comida = await ObtenerOCrearComidaAsync(fecha, tipoComidaId, ct);
        var n = Redondeo.Kcal(kcal);
        comida.Items.Add(new ComidaItem
        {
            Cantidad = 1,
            CantidadBase = 1,
            Nombre = LimpiarNombre(nombre, "De afuera"),
            Porcion = "",
            Kcal = n,
            EsAfuera = true
        });
        await db.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> AgregarKcalGymAsync(DateOnly fecha, double kcal, string? nombre = null, CancellationToken ct = default)
        => await AgregarKcalEntrenoAsync(kcal, async n =>
        {
            db.SesionesGym.Add(new SesionGym
            {
                Fecha = fecha,
                Nombre = LimpiarNombre(nombre, "Gym"),
                Kcal = n,
                Cantidad = 1
            });
            await db.SaveChangesAsync(ct);
        });

    public async Task<string?> AgregarKcalCardioAsync(DateOnly fecha, double kcal, string? nombre = null, CancellationToken ct = default)
        => await AgregarKcalEntrenoAsync(kcal, async n =>
        {
            db.SesionesCardio.Add(new SesionCardio
            {
                Fecha = fecha,
                Kcal = n,
                Cantidad = 1,
                Nota = LimpiarNombre(nombre, "Cardio")
            });
            await db.SaveChangesAsync(ct);
        });

    public async Task<string?> AgregarKcalDeporteAsync(DateOnly fecha, double kcal, string? nombre = null, CancellationToken ct = default)
        => await AgregarKcalEntrenoAsync(kcal, async n =>
        {
            db.ActividadLogs.Add(new ActividadLog
            {
                Fecha = fecha,
                Nombre = LimpiarNombre(nombre, "Deporte"),
                Tipo = "",
                Kcal = n,
                Cantidad = 1
            });
            await db.SaveChangesAsync(ct);
        });

    private static string LimpiarNombre(string? nombre, string respaldo)
    {
        var t = (nombre ?? "").Trim();
        if (t.Length == 0) return respaldo;
        return t.Length > 80 ? t[..80] : t;
    }

    private static async Task<string?> AgregarKcalEntrenoAsync(double kcal, Func<double, Task> guardar)
    {
        if (kcal is < 1 or > 20000)
            return "Poné las kcal (1 a 20.000).";
        await guardar(Redondeo.Kcal(kcal));
        return null;
    }

    public async Task QuitarGymSueltoAsync(int sesionId, DateOnly? fecha = null, CancellationToken ct = default)
    {
        var s = await db.SesionesGym.FirstOrDefaultAsync(x => x.Id == sesionId && x.RutinaDiaId == null, ct);
        if (s is null) return;
        if (fecha is { } f && s.Fecha != f) return;
        db.SesionesGym.Remove(s);
        await db.SaveChangesAsync(ct);
    }

    public async Task QuitarCardioSueltoAsync(int sesionId, DateOnly? fecha = null, CancellationToken ct = default)
    {
        var s = await db.SesionesCardio.FirstOrDefaultAsync(x => x.Id == sesionId && x.ProtocoloId == null, ct);
        if (s is null) return;
        if (fecha is { } f && s.Fecha != f) return;
        db.SesionesCardio.Remove(s);
        await db.SaveChangesAsync(ct);
    }

    public async Task QuitarDeporteSueltoAsync(int logId, DateOnly? fecha = null, CancellationToken ct = default)
    {
        var l = await db.ActividadLogs.FirstOrDefaultAsync(x => x.Id == logId && x.ActividadId == null, ct);
        if (l is null) return;
        if (fecha is { } f && l.Fecha != f) return;
        db.ActividadLogs.Remove(l);
        await db.SaveChangesAsync(ct);
    }

    public async Task QuitarItemAsync(int itemId, DateOnly? fecha = null, CancellationToken ct = default)
    {
        var item = await db.ComidaItems.Include(i => i.Comida).FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item?.Comida is null) return;
        if (fecha is { } f && item.Comida.Fecha != f) return;
        db.ComidaItems.Remove(item);
        await db.SaveChangesAsync(ct);
        var comida = await db.Comidas.Include(c => c.Items).FirstAsync(c => c.Id == item.ComidaId, ct);
        if (comida.Items.Count == 0)
        {
            db.Comidas.Remove(comida);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<string?> CambiarCantidadItemAsync(int itemId, double cantidad, CancellationToken ct = default)
    {
        if (cantidad is < FraccionHecha.Min or > FraccionHecha.Max)
            return "La fracción tiene que estar entre 10 % y 2000 %.";
        var item = await db.ComidaItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item is null) return "No está.";
        var g = item.GrupoFactor <= 0 ? 1 : item.GrupoFactor;
        var b = item.CantidadBase <= 0 ? 1 : item.CantidadBase;
        item.Cantidad = Math.Round(b * cantidad * g, 4);
        await db.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> CambiarCantidadGrupoAsync(string grupoId, double cantidad, DateOnly? fecha = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(grupoId)) return "No está.";
        if (cantidad is < FraccionHecha.Min or > FraccionHecha.Max)
            return "La fracción tiene que estar entre 10 % y 2000 %.";
        var items = await ItemsDeGrupoAsync(grupoId, fecha, ct);
        if (items.Count == 0) return "No está.";
        var actual = items[0].GrupoFactor <= 0 ? 1 : items[0].GrupoFactor;
        var f = FactorDe(actual, cantidad);
        foreach (var item in items)
        {
            item.Cantidad = Math.Round(item.Cantidad * f, 4);
            item.GrupoFactor = cantidad;
        }
        await db.SaveChangesAsync(ct);
        return null;
    }

    public async Task QuitarGrupoAsync(string grupoId, DateOnly? fecha = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(grupoId)) return;
        var items = await ItemsDeGrupoAsync(grupoId, fecha, ct);
        if (items.Count == 0) return;
        var comidaIds = items.Select(i => i.ComidaId).Distinct().ToList();
        db.ComidaItems.RemoveRange(items);
        await db.SaveChangesAsync(ct);
        foreach (var id in comidaIds)
        {
            var comida = await db.Comidas.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, ct);
            if (comida is { Items.Count: 0 })
                db.Comidas.Remove(comida);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<List<ComidaItem>> ItemsDeGrupoAsync(string grupoId, DateOnly? fecha, CancellationToken ct)
    {
        var q = db.ComidaItems.Include(i => i.Comida).Where(i => i.GrupoId == grupoId);
        if (fecha is { } f)
            q = q.Where(i => i.Comida!.Fecha == f);
        return await q.ToListAsync(ct);
    }

    public Task CambiarCantidadGymAsync(DateOnly fecha, int rutinaDiaId, double cantidad, CancellationToken ct = default)
        => EscalarGymAsync(fecha, rutinaDiaId, cantidad, ct);

    public async Task CambiarCantidadCardioAsync(DateOnly fecha, int protocoloId, double cantidad, CancellationToken ct = default)
    {
        if (cantidad is < FraccionHecha.Min or > FraccionHecha.Max) return;
        var s = await db.SesionesCardio.FirstOrDefaultAsync(x => x.Fecha == fecha && x.ProtocoloId == protocoloId, ct);
        if (s is null) return;
        var f = FactorDe(s.Cantidad, cantidad);
        s.Kcal = Redondeo.Kcal(s.Kcal * f);
        s.Minutos = Math.Round(s.Minutos * f, 1);
        s.Cantidad = cantidad;
        await db.SaveChangesAsync(ct);
    }

    public async Task CambiarCantidadActividadAsync(DateOnly fecha, int actividadId, double cantidad, CancellationToken ct = default)
    {
        if (cantidad is < FraccionHecha.Min or > FraccionHecha.Max) return;
        var l = await db.ActividadLogs.FirstOrDefaultAsync(x => x.Fecha == fecha && x.ActividadId == actividadId, ct);
        if (l is null) return;
        var f = FactorDe(l.Cantidad, cantidad);
        l.Kcal = Redondeo.Kcal(l.Kcal * f);
        if (l.Minutos is { } min) l.Minutos = Math.Round(min * f, 1);
        l.Cantidad = cantidad;
        await db.SaveChangesAsync(ct);
    }

    private async Task EscalarGymAsync(DateOnly fecha, int rutinaDiaId, double cantidad, CancellationToken ct)
    {
        if (cantidad is < FraccionHecha.Min or > FraccionHecha.Max) return;
        var s = await db.SesionesGym.Include(x => x.Ejercicios)
            .FirstOrDefaultAsync(x => x.Fecha == fecha && x.RutinaDiaId == rutinaDiaId, ct);
        if (s is null) return;
        var f = FactorDe(s.Cantidad, cantidad);
        if (s.Kcal is { } k) s.Kcal = Redondeo.Kcal(k * f);
        foreach (var e in s.Ejercicios)
            e.Series = Math.Max(1, (int)Math.Round(e.Series * f));
        s.Cantidad = cantidad;
        await db.SaveChangesAsync(ct);
    }

    private static double FactorDe(double actual, double nueva)
        => nueva / (actual <= 0 ? 1 : actual);

    public async Task VaciarDiaAsync(DateOnly fecha, CancellationToken ct = default)
    {
        db.Comidas.RemoveRange(await db.Comidas.Where(c => c.Fecha == fecha).ToListAsync(ct));
        db.SesionesGym.RemoveRange(await db.SesionesGym.Where(s => s.Fecha == fecha).ToListAsync(ct));
        db.SesionesCardio.RemoveRange(await db.SesionesCardio.Where(s => s.Fecha == fecha).ToListAsync(ct));
        db.ActividadLogs.RemoveRange(await db.ActividadLogs.Where(l => l.Fecha == fecha).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
    }

    public async Task<string?> CopiarDiaAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        if (desde == hasta)
            return "Elegí otro día.";

        var origen = await db.Comidas.AsNoTracking().Include(c => c.Items)
            .Where(c => c.Fecha == desde).ToListAsync(ct);
        var gym = await db.SesionesGym.AsNoTracking()
            .Include(s => s.Ejercicios).ThenInclude(e => e.SeriesDetalle)
            .Where(s => s.Fecha == desde).ToListAsync(ct);
        var cardio = await db.SesionesCardio.AsNoTracking().Where(s => s.Fecha == desde).ToListAsync(ct);
        var logs = await db.ActividadLogs.AsNoTracking().Where(l => l.Fecha == desde).ToListAsync(ct);
        if (origen.Count == 0 && gym.Count == 0 && cardio.Count == 0 && logs.Count == 0)
            return "Ese día no tiene nada para copiar.";

        await VaciarDiaAsync(hasta, ct);

        var grupos = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var c in origen)
        {
            db.Comidas.Add(new Comida
            {
                Fecha = hasta,
                TipoComidaId = c.TipoComidaId,
                Nota = c.Nota,
                Items = c.Items.Select(i => new ComidaItem
                {
                    AlimentoId = i.AlimentoId,
                    Cantidad = i.Cantidad,
                    CantidadBase = i.CantidadBase <= 0 ? i.Cantidad : i.CantidadBase,
                    GrupoId = GrupoNuevo(grupos, i.GrupoId),
                    GrupoNombre = i.GrupoNombre,
                    GrupoFactor = i.GrupoFactor <= 0 ? 1 : i.GrupoFactor,
                    Nombre = i.Nombre,
                    Porcion = i.Porcion,
                    Kcal = i.Kcal,
                    Proteina = i.Proteina,
                    Carbos = i.Carbos,
                    Grasa = i.Grasa,
                    EsAfuera = i.EsAfuera
                }).ToList()
            });
        }

        foreach (var s in gym)
        {
            db.SesionesGym.Add(new SesionGym
            {
                Fecha = hasta,
                RutinaDiaId = s.RutinaDiaId,
                Nombre = s.Nombre,
                Kcal = s.Kcal,
                Cantidad = s.Cantidad <= 0 ? 1 : s.Cantidad,
                Nota = s.Nota,
                Ejercicios = s.Ejercicios.Select(e => new SesionEjercicio
                {
                    EjercicioId = e.EjercicioId,
                    Orden = e.Orden,
                    Nombre = e.Nombre,
                    GrupoMuscular = e.GrupoMuscular,
                    Series = e.Series,
                    Reps = e.Reps,
                    Peso = e.Peso,
                    PuedeMas = e.PuedeMas,
                    Observacion = e.Observacion,
                    SeriesDetalle = e.SeriesDetalle.Select(d => new SerieRealizada
                    {
                        Numero = d.Numero,
                        Reps = d.Reps,
                        Peso = d.Peso
                    }).ToList()
                }).ToList()
            });
        }

        foreach (var s in cardio)
        {
            db.SesionesCardio.Add(new SesionCardio
            {
                Fecha = hasta,
                ProtocoloId = s.ProtocoloId,
                Minutos = s.Minutos,
                Velocidad = s.Velocidad,
                Inclinacion = s.Inclinacion,
                Kcal = s.Kcal,
                KcalMin = s.KcalMin,
                KcalMax = s.KcalMax,
                Cantidad = s.Cantidad <= 0 ? 1 : s.Cantidad,
                Nota = s.Nota
            });
        }

        foreach (var l in logs)
        {
            db.ActividadLogs.Add(new ActividadLog
            {
                Fecha = hasta,
                ActividadId = l.ActividadId,
                Nombre = l.Nombre,
                Tipo = l.Tipo,
                Kcal = l.Kcal,
                Minutos = l.Minutos,
                Cantidad = l.Cantidad <= 0 ? 1 : l.Cantidad
            });
        }

        await db.SaveChangesAsync(ct);
        return null;
    }

    private static string? GrupoNuevo(Dictionary<string, string> map, string? grupoId)
    {
        if (string.IsNullOrWhiteSpace(grupoId)) return grupoId;
        if (!map.TryGetValue(grupoId, out var n))
        {
            n = Guid.NewGuid().ToString("N");
            map[grupoId] = n;
        }
        return n;
    }

    public async Task AlternarActividadAsync(DateOnly fecha, int actividadId, CancellationToken ct = default)
    {
        var existente = await db.ActividadLogs.FirstOrDefaultAsync(l => l.Fecha == fecha && l.ActividadId == actividadId, ct);
        if (existente is not null)
        {
            db.ActividadLogs.Remove(existente);
            await db.SaveChangesAsync(ct);
            return;
        }

        var act = await db.Actividades.Include(a => a.TipoActividad).FirstAsync(a => a.Id == actividadId, ct);
        db.ActividadLogs.Add(new ActividadLog
        {
            Fecha = fecha,
            ActividadId = act.Id,
            Nombre = act.Nombre,
            Tipo = act.TipoActividad?.Nombre ?? "",
            Kcal = act.Kcal,
            Minutos = act.Minutos,
            Cantidad = 1
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task AlternarDiaRutinaAsync(DateOnly fecha, int rutinaDiaId, CancellationToken ct = default)
    {
        var existente = await db.SesionesGym.FirstOrDefaultAsync(s => s.Fecha == fecha && s.RutinaDiaId == rutinaDiaId, ct);
        if (existente is not null)
        {
            db.SesionesGym.Remove(existente);
            await db.SaveChangesAsync(ct);
            return;
        }

        await PrecargarSesionAsync(fecha, rutinaDiaId, ct);
    }

    public async Task<SesionGym> PrecargarSesionAsync(DateOnly fecha, int rutinaDiaId, CancellationToken ct = default)
    {
        var dia = await db.RutinaDias.Include(d => d.Ejercicios).ThenInclude(e => e.Ejercicio)
            .ThenInclude(x => x!.GrupoMuscular)
            .FirstAsync(d => d.Id == rutinaDiaId, ct);

        var sesion = new SesionGym
        {
            Fecha = fecha,
            RutinaDiaId = dia.Id,
            Nombre = dia.Nombre,
            Kcal = dia.KcalEstimadas,
            Cantidad = 1,
            Ejercicios = dia.Ejercicios.OrderBy(e => e.Orden).Select((e, i) => new SesionEjercicio
            {
                EjercicioId = e.EjercicioId,
                Orden = i,
                Nombre = e.Ejercicio?.Nombre ?? "",
                GrupoMuscular = e.Ejercicio?.GrupoMuscular?.Nombre,
                Series = e.Series,
                Reps = e.RepsMax ?? e.RepsMin,
                Peso = e.PesoRef,
                PuedeMas = e.PuedeMas,
                Observacion = e.Observacion
            }).ToList()
        };
        db.SesionesGym.Add(sesion);
        await db.SaveChangesAsync(ct);
        return sesion;
    }

    public async Task AlternarProtocoloAsync(DateOnly fecha, int protocoloId, CancellationToken ct = default)
    {
        var existente = await db.SesionesCardio.FirstOrDefaultAsync(s => s.Fecha == fecha && s.ProtocoloId == protocoloId, ct);
        if (existente is not null)
        {
            db.SesionesCardio.Remove(existente);
            await db.SaveChangesAsync(ct);
            return;
        }

        var proto = await db.ProtocolosCardio.Include(p => p.Bloques).FirstAsync(p => p.Id == protocoloId, ct);
        var min = DuracionProtocolo.Minutos(proto.Bloques);
        var medicion = await ServicioMediciones.ObtenerReferenciaAsync(db, ct);
        var peso = medicion?.PesoKg ?? 70;
        var primer = proto.Bloques.OrderBy(b => b.Orden).FirstOrDefault();
        db.SesionesCardio.Add(new SesionCardio
        {
            Fecha = fecha,
            ProtocoloId = proto.Id,
            Minutos = min,
            Velocidad = proto.Bloques.Count == 1 ? primer?.Velocidad : null,
            Inclinacion = proto.Bloques.Count == 1 ? primer?.Inclinacion : null,
            Kcal = Redondeo.Kcal(EstimacionCardio.DeProtocolo(proto, peso)),
            Cantidad = 1,
            Nota = proto.Nombre
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<DiaDePeriodo> ResumenFechaAsync(DateOnly fecha, CancellationToken ct = default)
    {
        var vista = await CargarAsync(fecha, null, ct);
        var b = vista.Balance;
        return new DiaDePeriodo(
            fecha, b.HayRegistro,
            b.Ingesta?.Kcal, b.Ingesta?.Proteina, b.Ingesta?.Carbos, b.Ingesta?.Grasa,
            b.Ingesta?.KcalSinMacros ?? 0,
            b.Gasto.Total, b.Deficit,
            b.Gasto.Gym, b.Gasto.Cardio, b.Gasto.Actividades);
    }

    private async Task<Comida> ObtenerOCrearComidaAsync(DateOnly fecha, int tipoComidaId, CancellationToken ct)
    {
        var comida = await db.Comidas.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Fecha == fecha && c.TipoComidaId == tipoComidaId, ct);
        if (comida is not null) return comida;
        comida = new Comida { Fecha = fecha, TipoComidaId = tipoComidaId };
        db.Comidas.Add(comida);
        return comida;
    }

    private static ComidaItem Copiar(Alimento a, double cantidad, string? grupoId = null, string? grupoNombre = null) => new()
    {
        AlimentoId = a.Id,
        Cantidad = cantidad,
        CantidadBase = cantidad,
        GrupoId = grupoId,
        GrupoNombre = grupoNombre,
        GrupoFactor = 1,
        Nombre = a.Nombre,
        Porcion = a.Porcion,
        Kcal = a.Kcal,
        Proteina = a.Proteina,
        Carbos = a.Carbos,
        Grasa = a.Grasa,
        EsAfuera = a.EsAfuera
    };
}
