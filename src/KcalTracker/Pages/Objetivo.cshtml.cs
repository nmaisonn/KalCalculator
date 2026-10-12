using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class ObjetivoModel(ServicioDia dias, ServicioPeriodo periodo, KcalDbContext db) : PageModel
{
    [BindProperty] public double GrasaObjetivoPct { get; set; } = 15;
    [BindProperty] public int PlazoSemanas { get; set; } = 8;
    [BindProperty] public DateOnly? FechaInicioObjetivo { get; set; }
    [BindProperty] public double? ProteinaObjetivoG { get; set; }
    [BindProperty] public bool UsarSugerida { get; set; }
    [BindProperty] public TipoObjetivo TipoObjetivo { get; set; } = TipoObjetivo.Corte;
    [BindProperty] public double? DeficitDiarioKcal { get; set; }

    public ResultadoPlan? Plan { get; private set; }
    public ResultadoDeficit? Deficit { get; private set; }
    public ResultadoCalibracion? Calibrado { get; private set; }
    public ResultadoProteina? Proteina { get; private set; }
    public RitmoObjetivo? Ritmo { get; private set; }
    public Medicion? Medicion { get; private set; }
    public DateOnly? FechaMedicionMasNueva { get; private set; }
    public ResultadoBmr? Bmr { get; private set; }
    public double? GastoSinDeporte { get; private set; }
    public double FactorActividad { get; private set; }
    public ResultadoMacros? Macros { get; private set; }
    public ResultadoAlcance? Alcance { get; private set; }
    public LecturaSeguimiento? Seguimiento { get; private set; }

    public VistaCalculoObjetivo VistaCalculo => new()
    {
        Deficit = Deficit,
        Plan = Plan,
        Ritmo = Ritmo,
        TipoObjetivo = TipoObjetivo,
        GrasaObjetivoPct = GrasaObjetivoPct,
        Macros = Macros,
        Bmr = Bmr,
        GastoSinDeporte = GastoSinDeporte,
        FactorActividad = FactorActividad,
        MasaMagraKg = Medicion is { } m ? Composicion.MagraKg(m.PesoKg, m.GrasaPct) : null,
        Alcance = Alcance,
        Seguimiento = Seguimiento,
        GrasaActualPct = Medicion?.GrasaPct,
        Handler = "Recalcular"
    };

    public async Task OnGetAsync()
    {
        var perfil = await db.Perfiles.SingleAsync();
        GrasaObjetivoPct = perfil.GrasaObjetivoPct ?? 15;
        PlazoSemanas = perfil.PlazoSemanas;
        FechaInicioObjetivo = perfil.FechaInicioObjetivo;
        ProteinaObjetivoG = perfil.ProteinaObjetivoG;
        TipoObjetivo = perfil.TipoObjetivo;
        DeficitDiarioKcal = perfil.DeficitDiarioKcal;
        await CalcularAsync();
        ProteinaObjetivoG ??= Proteina?.SugeridaG;
    }

    public async Task<IActionResult> OnPostRecalcularAsync()
    {
        if (PlazoSemanas < 1) PlazoSemanas = 1;
        await CalcularAsync(calibrar: false);
        return Partial("_CalculoObjetivo", VistaCalculo);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await CalcularAsync();
        if (UsarSugerida && Proteina is { } calc)
            ProteinaObjetivoG = calc.SugeridaG;

        var perfil = await db.Perfiles.SingleAsync();
        perfil.GrasaObjetivoPct = GrasaObjetivoPct;
        perfil.PlazoSemanas = PlazoSemanas;
        perfil.FechaInicioObjetivo = FechaInicioObjetivo is { } fi && fi != default ? fi : null;
        perfil.ProteinaObjetivoG = ProteinaObjetivoG is > 0 ? ProteinaObjetivoG : null;
        perfil.TipoObjetivo = TipoObjetivo;
        perfil.DeficitDiarioKcal = DeficitDiarioKcal is > 0 ? DeficitDiarioKcal : null;
        await db.SaveChangesAsync();
        await CalcularAsync();
        ProteinaObjetivoG = perfil.ProteinaObjetivoG ?? Proteina?.SugeridaG;
        return Page();
    }

    private async Task CalcularAsync(bool calibrar = true)
    {
        var (med, bmr, gasto, perfil, cfg) = await dias.BaseAsync();
        Medicion = med;
        Bmr = bmr;
        GastoSinDeporte = gasto;
        FactorActividad = perfil.FactorActividad;
        FechaMedicionMasNueva = med is null
            ? null
            : await db.Mediciones.MaxAsync(m => (DateOnly?)m.Fecha);
        Proteina = ProteinaNecesaria.Calcular(med?.PesoKg, med?.GrasaPct, TipoObjetivo);
        if (med is not null && gasto is { } g)
            Plan = PlanObjetivo.De(med.PesoKg, med.GrasaPct, GrasaObjetivoPct, PlazoSemanas,
                cfg.KcalPorKgGrasa, g, cfg, FechaInicioObjetivo);
        Deficit = DeficitObjetivo.De(Plan, gasto, med?.GrasaPct, TipoObjetivo, DeficitDiarioKcal);
        Calibrado = calibrar ? await periodo.CalibrarAsync() : null;
        var corrido = Plan?.Plazo ?? PlazoCorrido.De(PlazoSemanas, FechaInicioObjetivo);
        Ritmo = RitmoObjetivo.De(corrido, Plan, Calibrado, Deficit);
        Alcance = AlcanceObjetivo.De(Plan, Deficit, med?.GrasaPct, GrasaObjetivoPct, TipoObjetivo);
        Seguimiento = await CargarSeguimientoAsync(corrido);
        var protG = ProteinaObjetivoG is > 0 ? ProteinaObjetivoG : Proteina?.SugeridaG;
        Macros = MacrosNecesarios.De(
            gasto, Deficit?.Diario ?? 0, Deficit?.EsSuperavit == true,
            protG, med?.PesoKg, TipoObjetivo);
    }

    private async Task<LecturaSeguimiento?> CargarSeguimientoAsync(PlazoCorrido corrido)
    {
        if (Deficit is { EsSuperavit: true }) return null;
        var necesario = Plan is { UsaComposicion: true, DeficitDiario: > 0 }
            ? Plan.DeficitDiario
            : Deficit?.Necesario ?? 0;
        if (corrido.Inicio is null || necesario <= 0)
            return SeguimientoObjetivo.De(corrido, null, necesario, false);

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var desde = corrido.Inicio.Value;
        if (desde > hoy)
            return SeguimientoObjetivo.De(corrido, null, necesario, false);

        var metricas = await periodo.CalcularAsync(desde, hoy);
        return SeguimientoObjetivo.De(corrido, metricas.Serie, necesario, false);
    }
}
