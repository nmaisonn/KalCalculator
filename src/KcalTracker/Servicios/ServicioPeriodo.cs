using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

public class ServicioPeriodo(KcalDbContext db, ServicioDia dias)
{
    public async Task<MetricasPeriodo> CalcularAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
    {
        var (_, _, gastoSin, perfil, cfg) = await dias.BaseAsync(ct);
        var objetivo = 0d;
        var esSuperavit = false;
        var medicion = await ServicioMediciones.ObtenerReferenciaAsync(db, ct);
        if (medicion is not null && gastoSin is { } g)
        {
            var plan = PlanObjetivo.De(medicion.PesoKg, medicion.GrasaPct, perfil.GrasaObjetivoPct,
                perfil.PlazoSemanas, cfg.KcalPorKgGrasa, g, cfg,
                perfil.FechaInicioObjetivo);
            var meta = DeficitObjetivo.De(plan, g, medicion.GrasaPct, perfil.TipoObjetivo, perfil.DeficitDiarioKcal);
            objetivo = meta.Diario;
            esSuperavit = meta.EsSuperavit;
        }

        var serie = new List<DiaDePeriodo>();
        for (var f = desde; f <= hasta; f = f.AddDays(1))
            serie.Add(await dias.ResumenFechaAsync(f, ct));

        var meds = await db.Mediciones.OrderBy(m => m.Fecha).ToListAsync(ct);
        IReadOnlyList<DiaDePeriodo>? ventana = null;
        double? grasaMedida = null;
        var par = ContrasteComposicion.Par(meds, desde, hasta);
        if (par is { } p)
        {
            grasaMedida = ContrasteComposicion.GrasaPerdidaKg(p.Inicial, p.Ultima);
            var extra = new List<DiaDePeriodo>();
            for (var f = p.Inicial.Fecha; f <= p.Ultima.Fecha; f = f.AddDays(1))
            {
                var ya = serie.Find(d => d.Fecha == f);
                extra.Add(ya ?? await dias.ResumenFechaAsync(f, ct));
            }
            ventana = extra;
        }

        return Periodo.Calcular(serie, objetivo, cfg.KcalPorKgGrasa, cfg.CobMinContraste, grasaMedida, esSuperavit, ventana);
    }

    public async Task<ResultadoCalibracion?> CalibrarAsync(CancellationToken ct = default)
    {
        var meds = await db.Mediciones.Where(m => m.EsComparable && m.GrasaPct != null)
            .OrderBy(m => m.Fecha).ToListAsync(ct);
        if (meds.Count < 2) return null;
        var a = meds[0];
        var b = meds[^1];
        var cfg = await db.Configuraciones.SingleAsync(ct);
        var metricas = await CalcularAsync(a.Fecha, b.Fecha, ct);
        return Calibracion.Calcular(
            a.Fecha, b.Fecha, a.PesoKg, a.GrasaPct!.Value, a.MusculoPct,
            b.PesoKg, b.GrasaPct!.Value, b.MusculoPct,
            metricas.DiasConRegistro, metricas.IngestaMedia * metricas.DiasConRegistro,
            cfg.KcalPorKgGrasa, cfg.KcalPorKgMagro);
    }
}

public sealed record ResumenLateral(
    string Nombre,
    string Email,
    Medicion? Medicion,
    double? GrasaObjetivoPct,
    double? GrasaAPerderKg,
    TipoObjetivo TipoObjetivo);

public class ServicioContexto(KcalDbContext db, IUsuarioActual usuario, ServicioDia dias)
{
    public async Task<ResumenLateral?> CargarAsync(CancellationToken ct = default)
    {
        if (usuario.UsuarioId is null) return null;
        var cuenta = await db.Usuarios.FirstAsync(u => u.Id == usuario.UsuarioId, ct);
        var perfil = await db.Perfiles.SingleAsync(ct);
        var (medicion, _, gastoSin, _, cfg) = await dias.BaseAsync(ct);
        double? aPerder = null;
        if (medicion is not null && gastoSin is { } g)
        {
            var plan = PlanObjetivo.De(medicion.PesoKg, medicion.GrasaPct, perfil.GrasaObjetivoPct,
                perfil.PlazoSemanas, cfg.KcalPorKgGrasa, g, cfg,
                perfil.FechaInicioObjetivo);
            aPerder = plan is { UsaComposicion: true } ? plan.GrasaAPerderKg : null;
        }
        return new ResumenLateral(cuenta.Nombre ?? cuenta.Email, cuenta.Email, medicion, perfil.GrasaObjetivoPct, aPerder, perfil.TipoObjetivo);
    }
}
