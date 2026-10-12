using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

public class ServicioAlimentos(KcalDbContext db)
{
    public Task<Alimento?> PorIdAsync(int id, CancellationToken ct = default)
        => db.Alimentos.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(bool Ok, string? Error, string? Aviso)> GuardarAsync(Alimento entrada, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entrada.Nombre))
            return (false, "Falta el nombre.", null);
        if (entrada.Kcal < 0)
            return (false, "Las kcal no pueden ser negativas.", null);

        string? aviso = null;
        if (entrada.Proteina is not null && entrada.Carbos is not null && entrada.Grasa is not null)
        {
            var v = ValidadorSugerencia.Validar(entrada.Kcal, entrada.Proteina, entrada.Carbos, entrada.Grasa);
            if (v.Nivel is NivelSugerencia.Advertencia or NivelSugerencia.NoConfiable)
                aviso = v.Detalle;
        }

        if (entrada.Id == 0)
        {
            if (entrada.Origen == OrigenDato.Plantilla)
                entrada.Origen = OrigenDato.Manual;
            if (entrada.Origen is OrigenDato.OpenFoodFacts or OrigenDato.Usda or OrigenDato.ModeloLenguaje)
                entrada.ObtenidoEl = DateTime.UtcNow;
            entrada.EsAfuera = await EsAfueraDeCategoriaAsync(entrada.CategoriaAlimentoId, entrada.EsAfuera, ct);
            db.Alimentos.Add(entrada);
        }
        else
        {
            var a = await db.Alimentos.FirstOrDefaultAsync(x => x.Id == entrada.Id, ct);
            if (a is null) return (false, "No está.", null);
            if (db.EsDelCatalogo(a.UsuarioId))
                return (false, "El catálogo de la app no se edita. Si querés cambiarlo, cargá una copia en los tuyos.", null);
            a.Nombre = entrada.Nombre.Trim();
            a.Porcion = entrada.Porcion.Trim();
            a.Kcal = entrada.Kcal;
            a.Proteina = entrada.Proteina;
            a.Carbos = entrada.Carbos;
            a.Grasa = entrada.Grasa;
            a.EsAfuera = await EsAfueraDeCategoriaAsync(entrada.CategoriaAlimentoId, entrada.EsAfuera, ct);
            a.CategoriaAlimentoId = entrada.CategoriaAlimentoId;
            a.TipoComidaId = entrada.TipoComidaId;
            a.Activo = entrada.Activo;
            var (dias, enCombo) = await PropagarAUsosAsync(a, ct);
            var extra = AvisoDePropagacion(dias, enCombo);
            if (extra is not null)
                aviso = aviso is null ? extra : aviso + " " + extra;
        }

        await db.SaveChangesAsync(ct);
        return (true, null, aviso);
    }

    /// <summary>
    /// Reescribe las líneas de días que apuntan a este alimento. Los combos no
    /// guardan kcal propias: al usarse o mostrarse leen el catálogo vigente.
    /// </summary>
    private async Task<(int Dias, bool EnCombo)> PropagarAUsosAsync(Alimento a, CancellationToken ct)
    {
        var items = await db.ComidaItems.Include(i => i.Comida)
            .Where(i => i.AlimentoId == a.Id)
            .ToListAsync(ct);
        foreach (var i in items)
        {
            i.Nombre = a.Nombre;
            i.Porcion = a.Porcion;
            i.Kcal = a.Kcal;
            i.Proteina = a.Proteina;
            i.Carbos = a.Carbos;
            i.Grasa = a.Grasa;
            i.EsAfuera = a.EsAfuera;
        }

        var dias = items.Select(i => i.Comida!.Fecha).Distinct().Count();
        var enCombo = await db.ComboItems.AnyAsync(i => i.AlimentoId == a.Id, ct);
        return (dias, enCombo);
    }

    private static string? AvisoDePropagacion(int dias, bool enCombo)
    {
        if (dias == 0 && !enCombo) return null;
        var partes = new List<string>();
        if (dias == 1) partes.Add("1 día");
        else if (dias > 1) partes.Add($"{dias} días");
        if (enCombo) partes.Add("combos");
        return "Se actualizó en " + string.Join(" y ", partes) + ".";
    }

    private async Task<bool> EsAfueraDeCategoriaAsync(int? categoriaId, bool marcado, CancellationToken ct)
    {
        if (categoriaId is not > 0) return marcado;
        var cat = await db.CategoriasAlimento.FirstOrDefaultAsync(c => c.Id == categoriaId, ct);
        if (cat is null) return marcado;
        return CategoriaAlimento.EsAfuera(cat.Nombre) || marcado;
    }

    public async Task<(bool Ok, string? Error)> GuardarComboAsync(Combo entrada, IReadOnlyList<(int AlimentoId, double Cantidad)> items, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entrada.Nombre))
            return (false, "Falta el nombre.");
        if (entrada.Id == 0)
        {
            entrada.Items = items.Select(i => new ComboItem { AlimentoId = i.AlimentoId, Cantidad = i.Cantidad }).ToList();
            db.Combos.Add(entrada);
        }
        else
        {
            var c = await db.Combos.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == entrada.Id, ct);
            if (c is null) return (false, "No está.");
            c.Nombre = entrada.Nombre.Trim();
            c.TipoComidaId = entrada.TipoComidaId;
            c.Activo = entrada.Activo;
            db.ComboItems.RemoveRange(c.Items);
            c.Items = items.Select(i => new ComboItem { AlimentoId = i.AlimentoId, Cantidad = i.Cantidad }).ToList();
        }
        await db.SaveChangesAsync(ct);
        return (true, null);
    }
}

public class ServicioActividades(KcalDbContext db)
{
    public async Task<(bool Ok, string? Error, string? Aviso)> GuardarAsync(Actividad entrada, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entrada.Nombre))
            return (false, "Falta el nombre.", null);
        string? aviso = null;
        if (entrada.Id == 0)
            db.Actividades.Add(entrada);
        else
        {
            var a = await db.Actividades.FirstOrDefaultAsync(x => x.Id == entrada.Id, ct);
            if (a is null) return (false, "No está.", null);
            var usos = await db.ActividadLogs.CountAsync(l => l.ActividadId == a.Id, ct);
            a.Nombre = entrada.Nombre.Trim();
            a.TipoActividadId = entrada.TipoActividadId;
            a.Kcal = entrada.Kcal;
            a.Minutos = entrada.Minutos;
            a.Activo = entrada.Activo;
            if (usos > 0)
                aviso = $"Esta actividad se usó en {usos} sesiones. Cambiar las kcal no las modifica.";
        }
        await db.SaveChangesAsync(ct);
        return (true, null, aviso);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var a = await db.Actividades.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return;
        a.Activo = false;
        await db.SaveChangesAsync(ct);
    }
}

public class ServicioGym(KcalDbContext db)
{
    public Task<SesionGym?> PorIdAsync(int id, CancellationToken ct = default)
        => db.SesionesGym.Include(s => s.Ejercicios).ThenInclude(e => e.SeriesDetalle)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<List<SesionGym>> DeFechaAsync(DateOnly fecha, CancellationToken ct = default)
        => db.SesionesGym.Include(s => s.Ejercicios).Where(s => s.Fecha == fecha)
            .OrderBy(s => s.Id).ToListAsync(ct);

    public async Task GuardarAsync(SesionGym entrada, CancellationToken ct = default)
    {
        var s = await db.SesionesGym.Include(x => x.Ejercicios)
            .FirstAsync(x => x.Id == entrada.Id, ct);
        s.Fecha = entrada.Fecha;
        s.Kcal = entrada.Kcal;
        s.Cantidad = entrada.Cantidad <= 0 ? 1 : entrada.Cantidad;
        s.Nota = entrada.Nota;
        foreach (var e in entrada.Ejercicios)
        {
            var dest = s.Ejercicios.FirstOrDefault(x => x.Id == e.Id);
            if (dest is null) continue;
            dest.Series = e.Series;
            dest.Reps = e.Reps;
            dest.Peso = e.Peso;
            dest.PuedeMas = e.PuedeMas;
            dest.Observacion = e.Observacion;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var s = await db.SesionesGym.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return;
        db.SesionesGym.Remove(s);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PuntoProgresion>> ProgresionAsync(int ejercicioId, CancellationToken ct = default)
    {
        var puntos = await db.SesionEjercicios
            .Where(e => e.EjercicioId == ejercicioId)
            .Select(e => new { e.SesionGym!.Fecha, e.Peso, e.Reps })
            .ToListAsync(ct);
        return Progresion.De(puntos.Select(p => (p.Fecha, p.Peso, p.Reps)));
    }
}

public class ServicioCardio(KcalDbContext db)
{
    public static double KcalEfectivas(double? puntual, double? min, double? max)
    {
        if (puntual is > 0) return puntual.Value;
        if (min is not null && max is not null) return (min.Value + max.Value) / 2;
        return min ?? max ?? 0;
    }

    public async Task GuardarSesionAsync(SesionCardio entrada, CancellationToken ct = default)
    {
        entrada.Kcal = KcalEfectivas(entrada.Kcal == 0 ? null : entrada.Kcal, entrada.KcalMin, entrada.KcalMax);
        if (entrada.Kcal == 0)
            entrada.Kcal = await EstimarKcalAsync(entrada, ct);
        if (entrada.Id == 0)
            db.SesionesCardio.Add(entrada);
        else
        {
            var s = await db.SesionesCardio.FirstAsync(x => x.Id == entrada.Id, ct);
            s.Fecha = entrada.Fecha;
            s.ProtocoloId = entrada.ProtocoloId;
            s.Minutos = entrada.Minutos;
            s.Velocidad = entrada.Velocidad;
            s.Inclinacion = entrada.Inclinacion;
            s.KcalMin = entrada.KcalMin;
            s.KcalMax = entrada.KcalMax;
            s.Kcal = entrada.Kcal;
            s.Nota = entrada.Nota;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<double> EstimarKcalAsync(SesionCardio entrada, CancellationToken ct = default)
    {
        var medicion = await ServicioMediciones.ObtenerReferenciaAsync(db, ct);
        var peso = medicion?.PesoKg ?? 70;

        if (entrada.ProtocoloId is { } pid)
        {
            var proto = await db.ProtocolosCardio.Include(p => p.Bloques).FirstOrDefaultAsync(p => p.Id == pid, ct);
            if (proto is not null)
                return Redondeo.Kcal(EstimacionCardio.DeProtocolo(proto, peso));
        }

        return Redondeo.Kcal(EstimacionCardio.DeSesion(
            entrada.Minutos, entrada.Velocidad, entrada.Inclinacion, null, peso, gastoSinDeporteDiario: null));
    }

    public async Task EliminarSesionAsync(int id, CancellationToken ct = default)
    {
        var s = await db.SesionesCardio.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return;
        db.SesionesCardio.Remove(s);
        await db.SaveChangesAsync(ct);
    }

    public async Task GuardarProtocoloAsync(ProtocoloCardio entrada, IReadOnlyList<BloqueCardio> bloques, CancellationToken ct = default)
    {
        if (entrada.Id == 0)
        {
            entrada.Bloques = bloques.Select((b, i) => { b.Orden = i; return b; }).ToList();
            db.ProtocolosCardio.Add(entrada);
        }
        else
        {
            var p = await db.ProtocolosCardio.Include(x => x.Bloques).FirstAsync(x => x.Id == entrada.Id, ct);
            p.Nombre = entrada.Nombre;
            p.Descripcion = entrada.Descripcion;
            p.Kcal = entrada.Kcal is > 0 ? entrada.Kcal : null;
            p.Activo = entrada.Activo;
            db.BloquesCardio.RemoveRange(p.Bloques);
            p.Bloques = bloques.Select((b, i) => { b.Orden = i; b.Id = 0; return b; }).ToList();
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task DesactivarProtocoloAsync(int id, CancellationToken ct = default)
    {
        var p = await db.ProtocolosCardio.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return;
        p.Activo = false;
        await db.SaveChangesAsync(ct);
    }
}
