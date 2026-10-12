using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

public sealed record ReferenciaMedicion(
    Medicion Medicion,
    int? BmrKcal,
    string? Aviso);

public sealed record ResultadoMedicion(
    bool Ok,
    string? Error,
    string? Aviso);

/// <summary>
/// Alta, edición y baja de mediciones. El BMR no se guarda: se recalcula
/// sobre la medición más reciente (RF-D8, CA-19).
/// </summary>
public class ServicioMediciones(KcalDbContext db)
{
    public async Task<IReadOnlyList<Medicion>> ListarAsync(CancellationToken ct = default)
        => await db.Mediciones.OrderByDescending(m => m.Fecha).ToListAsync(ct);

    public Task<Medicion?> PorIdAsync(int id, CancellationToken ct = default)
        => db.Mediciones.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<ReferenciaMedicion?> ReferenciaAsync(CancellationToken ct = default)
    {
        var medicion = await ObtenerReferenciaAsync(db, ct);
        if (medicion is null) return null;
        var bmr = await BmrDeAsync(medicion, ct);
        return new ReferenciaMedicion(medicion, bmr.Kcal is { } k ? Redondeo.Kcal(k) : null, bmr.Aviso);
    }

    public static async Task<Medicion?> ObtenerReferenciaAsync(KcalDbContext db, CancellationToken ct = default)
    {
        var marcada = await db.Mediciones.Where(m => m.EsReferencia).OrderByDescending(m => m.Fecha).FirstOrDefaultAsync(ct);
        if (marcada is not null) return marcada;
        return await db.Mediciones.OrderByDescending(m => m.Fecha).FirstOrDefaultAsync(ct);
    }

    public async Task MarcarReferenciaAsync(int id, CancellationToken ct = default)
    {
        var mediciones = await db.Mediciones.ToListAsync(ct);
        foreach (var m in mediciones)
            m.EsReferencia = m.Id == id;
        await db.SaveChangesAsync(ct);
    }

    public async Task MarcarComparableAsync(int id, bool on, CancellationToken ct = default)
    {
        var medicion = await db.Mediciones.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (medicion is null) return;
        medicion.EsComparable = on;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ResultadoMedicion> GuardarAsync(Medicion entrada, CancellationToken ct = default)
    {
        if (entrada.PesoKg <= 0)
            return new ResultadoMedicion(false, "El peso tiene que ser mayor que cero.", null);

        var otra = await db.Mediciones.FirstOrDefaultAsync(
            m => m.Fecha == entrada.Fecha && m.Id != entrada.Id, ct);
        if (otra is not null)
            return new ResultadoMedicion(false, "Ya hay una medición en esa fecha. Editá esa.", null);

        var referenciaAntes = await ReferenciaAsync(ct);

        Medicion guardada;
        if (entrada.Id == 0)
        {
            guardada = new Medicion
            {
                Fecha = entrada.Fecha,
                PesoKg = entrada.PesoKg,
                GrasaPct = entrada.GrasaPct,
                MusculoPct = entrada.MusculoPct,
                Visceral = entrada.Visceral,
                EsComparable = entrada.EsComparable,
                Nota = entrada.Nota
            };
            db.Mediciones.Add(guardada);
        }
        else
        {
            var existente = await db.Mediciones.FirstOrDefaultAsync(m => m.Id == entrada.Id, ct);
            if (existente is null)
                return new ResultadoMedicion(false, "No está.", null);

            existente.Fecha = entrada.Fecha;
            existente.PesoKg = entrada.PesoKg;
            existente.GrasaPct = entrada.GrasaPct;
            existente.MusculoPct = entrada.MusculoPct;
            existente.Visceral = entrada.Visceral;
            existente.EsComparable = entrada.EsComparable;
            existente.Nota = entrada.Nota;
            guardada = existente;
        }

        await db.SaveChangesAsync(ct);
        var aviso = await AvisoSiCambioLaReferenciaAsync(referenciaAntes, ct);
        return new ResultadoMedicion(true, null, aviso);
    }

    public async Task<ResultadoMedicion> EliminarAsync(int id, CancellationToken ct = default)
    {
        var medicion = await db.Mediciones.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (medicion is null)
            return new ResultadoMedicion(false, "No está.", null);

        var referenciaAntes = await ReferenciaAsync(ct);
        db.Mediciones.Remove(medicion);
        await db.SaveChangesAsync(ct);
        var aviso = await AvisoSiCambioLaReferenciaAsync(referenciaAntes, ct);
        return new ResultadoMedicion(true, null, aviso);
    }

    public async Task<ResultadoBmr> BmrDeAsync(Medicion medicion, CancellationToken ct = default)
    {
        var cfg = await db.Configuraciones.SingleAsync(ct);
        var perfil = await db.Perfiles.SingleAsync(ct);
        var edad = perfil.FechaNacimiento is { } nac
            ? Metabolismo.Edad(nac, medicion.Fecha)
            : null;
        return Metabolismo.Calcular(
            cfg.MetodoBmr, cfg.BmrMedido, medicion.PesoKg, medicion.GrasaPct,
            perfil.AlturaCm, edad, perfil.Sexo);
    }

    private async Task<string?> AvisoSiCambioLaReferenciaAsync(ReferenciaMedicion? antes, CancellationToken ct)
    {
        var despues = await ReferenciaAsync(ct);
        if (antes is null && despues is null) return null;
        if (antes is not null && despues is not null && antes.Medicion.Id == despues.Medicion.Id
            && antes.BmrKcal == despues.BmrKcal)
            return null;

        if (despues is null)
            return "No queda ninguna medición. El gasto basal no se puede calcular hasta que cargues otra.";

        if (antes is null)
            return $"La referencia pasó a ser la del {despues.Medicion.Fecha:yyyy-MM-dd}.";

        var bmrAntes = antes.BmrKcal?.ToString() ?? "—";
        var bmrDespues = despues.BmrKcal?.ToString() ?? "—";
        var m = despues.Medicion;
        var grasa = m.GrasaPct is { } g ? $", {g.ToString("0.#")} %" : "";
        return $"La referencia pasó a ser la del {m.Fecha:yyyy-MM-dd} ({m.PesoKg.ToString("0.#")} kg{grasa}), y el gasto basal pasó de {bmrAntes} a {bmrDespues} kcal.";
    }
}
