using KcalTracker.Calculo;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KcalTracker.Pages;

public class DashboardModel(ServicioPeriodo periodo, ServicioDia dias, ServicioExportacionPeriodo exportacion) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Periodo { get; set; } = "semana";

    [BindProperty(SupportsGet = true)]
    public DateOnly? Desde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? Hasta { get; set; }

    public DateOnly DesdeEfectivo { get; private set; }
    public DateOnly HastaEfectivo { get; private set; }
    public MetricasPeriodo Metricas { get; private set; } = null!;
    public double ObjetivoDiario { get; private set; }
    public bool EsSuperavit { get; private set; }
    public string NombreObjetivo { get; private set; } = TipoObjetivoNombres.De(TipoObjetivo.Corte);
    public double CobMinContraste { get; private set; }
    public double? ProteinaObjetivo { get; private set; }
    public ResultadoMacros? Macros { get; private set; }
    public LecturaProgreso Progreso { get; private set; } = null!;
    public string EtiquetaPeriodo { get; private set; } = "la semana";

    public async Task<IActionResult> OnGetExportarAsync()
    {
        AplicarRango();
        var bytes = await exportacion.ArmarAsync(DesdeEfectivo, HastaEfectivo);
        return File(bytes, ServicioImportacionAlimentos.TipoContenidoXlsx,
            $"kcal-analisis-{DesdeEfectivo:yyyyMMdd}-{HastaEfectivo:yyyyMMdd}.xlsx");
    }

    public async Task OnGetAsync()
    {
        AplicarRango();

        var (med, _, gasto, perfil, cfg) = await dias.BaseAsync();
        CobMinContraste = cfg.CobMinContraste;
        ResultadoPlan? plan = null;
        if (med is not null && gasto is { } g)
            plan = PlanObjetivo.De(med.PesoKg, med.GrasaPct, perfil.GrasaObjetivoPct, perfil.PlazoSemanas,
                cfg.KcalPorKgGrasa, g, cfg, perfil.FechaInicioObjetivo);
        var meta = DeficitObjetivo.De(plan, gasto, med?.GrasaPct, perfil.TipoObjetivo, perfil.DeficitDiarioKcal);
        ObjetivoDiario = meta.Diario;
        EsSuperavit = meta.EsSuperavit;
        NombreObjetivo = meta.Nombre;
        ProteinaObjetivo = ProteinaNecesaria.Objetivo(perfil.ProteinaObjetivoG, med?.PesoKg, med?.GrasaPct, perfil.TipoObjetivo);
        Macros = MacrosNecesarios.De(
            gasto, meta.Diario, meta.EsSuperavit, ProteinaObjetivo, med?.PesoKg, perfil.TipoObjetivo);

        Metricas = await periodo.CalcularAsync(DesdeEfectivo, HastaEfectivo);
        EtiquetaPeriodo = (Desde, Hasta, Periodo) switch
        {
            ({ } a, { } b, _) when a <= b => "el período",
            (_, _, "mes") => "el mes",
            (_, _, "10d") => "estos 10 días",
            _ => "la semana"
        };
        Progreso = ProgresoObjetivo.De(
            Metricas.Serie, DateOnly.FromDateTime(DateTime.Today),
            ObjetivoDiario, EsSuperavit, EtiquetaPeriodo);
    }

    private void AplicarRango()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        (DesdeEfectivo, HastaEfectivo) = Periodo switch
        {
            "mes" => (new DateOnly(hoy.Year, hoy.Month, 1), hoy),
            "10d" => (hoy.AddDays(-9), hoy),
            _ => (hoy.AddDays(-((int)hoy.DayOfWeek + 6) % 7), hoy)
        };
        if (Desde is { } d && Hasta is { } h && d <= h)
        {
            DesdeEfectivo = d;
            HastaEfectivo = h;
        }
    }
}
