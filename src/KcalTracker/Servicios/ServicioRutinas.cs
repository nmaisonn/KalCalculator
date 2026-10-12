using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

public class ServicioRutinas(KcalDbContext db)
{
    public async Task<(bool Ok, string? Error, Rutina? Rutina)> CrearAsync(
        string nombre, string? descripcion, bool activar, CancellationToken ct = default)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre.Length == 0)
            return (false, "Falta el nombre.", null);

        var hayActiva = await db.Rutinas.AnyAsync(r => r.EsActiva, ct);
        var r = new Rutina
        {
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
            EsActiva = activar || !hayActiva
        };
        if (r.EsActiva)
            await ApagarLasDemasAsync(null, ct);
        db.Rutinas.Add(r);
        await db.SaveChangesAsync(ct);
        return (true, null, r);
    }

    public async Task<(bool Ok, string? Error)> GuardarDatosAsync(
        int id, string nombre, string? descripcion, CancellationToken ct = default)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre.Length == 0)
            return (false, "Falta el nombre.");
        var r = await db.Rutinas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return (false, "No está.");
        r.Nombre = nombre;
        r.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ActivarAsync(int id, CancellationToken ct = default)
    {
        var r = await db.Rutinas.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return (false, "No está.");
        await ApagarLasDemasAsync(id, ct);
        await db.SaveChangesAsync(ct);
        r.EsActiva = true;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error, Rutina? Copia)> DuplicarAsync(int id, CancellationToken ct = default)
    {
        var r = await db.Rutinas
            .Include(x => x.Dias).ThenInclude(d => d.Ejercicios)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return (false, "No está.", null);

        var copia = new Rutina
        {
            Nombre = r.Nombre + " (copia)",
            Descripcion = r.Descripcion,
            EsActiva = false,
            Dias = r.Dias.OrderBy(d => d.Orden).Select(d => new RutinaDia
            {
                Nombre = d.Nombre,
                Orden = d.Orden,
                KcalEstimadas = d.KcalEstimadas,
                Ejercicios = d.Ejercicios.OrderBy(e => e.Orden).Select(e => new RutinaEjercicio
                {
                    EjercicioId = e.EjercicioId,
                    Orden = e.Orden,
                    Series = e.Series,
                    RepsMin = e.RepsMin,
                    RepsMax = e.RepsMax,
                    PesoRef = e.PesoRef,
                    PuedeMas = e.PuedeMas,
                    Observacion = e.Observacion
                }).ToList()
            }).ToList()
        };
        db.Rutinas.Add(copia);
        await db.SaveChangesAsync(ct);
        return (true, null, copia);
    }

    public async Task<(bool Ok, string? Error)> EliminarAsync(int id, CancellationToken ct = default)
    {
        var r = await db.Rutinas.Include(x => x.Dias).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return (false, "No está.");
        db.Rutinas.Remove(r);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> AgregarDiaAsync(
        int rutinaId, string nombre, double? kcal, CancellationToken ct = default)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre.Length == 0)
            return (false, "Falta el nombre del día.");
        var r = await db.Rutinas.Include(x => x.Dias).FirstOrDefaultAsync(x => x.Id == rutinaId, ct);
        if (r is null) return (false, "No está.");
        var orden = r.Dias.Count == 0 ? 1 : r.Dias.Max(d => d.Orden) + 1;
        r.Dias.Add(new RutinaDia { Nombre = nombre, Orden = orden, KcalEstimadas = kcal is > 0 ? kcal : null });
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> GuardarDiaAsync(
        int diaId, string nombre, double? kcal, CancellationToken ct = default)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre.Length == 0)
            return (false, "Falta el nombre del día.");
        var d = await db.RutinaDias.FirstOrDefaultAsync(x => x.Id == diaId, ct);
        if (d is null) return (false, "No está.");
        d.Nombre = nombre;
        d.KcalEstimadas = kcal is > 0 ? kcal : null;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> QuitarDiaAsync(int diaId, CancellationToken ct = default)
    {
        var d = await db.RutinaDias.FirstOrDefaultAsync(x => x.Id == diaId, ct);
        if (d is null) return (false, "No está.");
        db.RutinaDias.Remove(d);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> AgregarEjercicioAsync(
        int diaId, int ejercicioId, int series, int repsMin, int? repsMax,
        double? peso, bool puedeMas, string? observacion, CancellationToken ct = default)
    {
        if (ejercicioId <= 0)
            return (false, "Elegí un ejercicio del catálogo.");
        var d = await db.RutinaDias.Include(x => x.Ejercicios).FirstOrDefaultAsync(x => x.Id == diaId, ct);
        if (d is null) return (false, "No está.");
        if (!await db.Ejercicios.AnyAsync(e => e.Id == ejercicioId && e.Activo, ct))
            return (false, "Ese ejercicio no está en el catálogo.");
        if (series < 1) series = 1;
        if (repsMin < 1) repsMin = 1;
        if (repsMax is < 1) repsMax = null;
        if (repsMax is { } max && max < repsMin) repsMax = repsMin;
        var orden = d.Ejercicios.Count == 0 ? 1 : d.Ejercicios.Max(e => e.Orden) + 1;
        d.Ejercicios.Add(new RutinaEjercicio
        {
            EjercicioId = ejercicioId,
            Orden = orden,
            Series = series,
            RepsMin = repsMin,
            RepsMax = repsMax,
            PesoRef = peso is > 0 ? peso : null,
            PuedeMas = puedeMas,
            Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim()
        });
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> GuardarEjercicioAsync(
        int id, int series, int repsMin, int? repsMax,
        double? peso, bool puedeMas, string? observacion, CancellationToken ct = default)
    {
        var e = await db.RutinaEjercicios.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return (false, "No está.");
        if (series < 1) series = 1;
        if (repsMin < 1) repsMin = 1;
        if (repsMax is < 1) repsMax = null;
        if (repsMax is { } max && max < repsMin) repsMax = repsMin;
        e.Series = series;
        e.RepsMin = repsMin;
        e.RepsMax = repsMax;
        e.PesoRef = peso is > 0 ? peso : null;
        e.PuedeMas = puedeMas;
        e.Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> QuitarEjercicioAsync(int id, CancellationToken ct = default)
    {
        var e = await db.RutinaEjercicios.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return (false, "No está.");
        db.RutinaEjercicios.Remove(e);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ReordenarEjerciciosAsync(
        int diaId, IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return (false, "No hay un orden nuevo.");
        var d = await db.RutinaDias.Include(x => x.Ejercicios).FirstOrDefaultAsync(x => x.Id == diaId, ct);
        if (d is null) return (false, "No está.");
        var actuales = d.Ejercicios.Select(e => e.Id).ToHashSet();
        if (ids.Count != actuales.Count || ids.Any(i => !actuales.Contains(i)))
            return (false, "El orden no coincide con los ejercicios de este día.");
        for (var i = 0; i < ids.Count; i++)
            d.Ejercicios.First(e => e.Id == ids[i]).Orden = i + 1;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    private async Task ApagarLasDemasAsync(int? salvoId, CancellationToken ct)
    {
        var activas = await db.Rutinas.Where(r => r.EsActiva && r.Id != salvoId).ToListAsync(ct);
        foreach (var x in activas)
            x.EsActiva = false;
    }
}
