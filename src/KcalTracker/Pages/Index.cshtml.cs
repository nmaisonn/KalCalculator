using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class IndexModel(ServicioDia dias, KcalDbContext db, IUsuarioActual usuarioActual) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public int TipoComidaId { get; set; }

    [BindProperty]
    public DateOnly CopiarDesde { get; set; }

    [BindProperty]
    public string? Eleccion { get; set; }

    public VistaHoy Vista { get; set; } = null!;
    public string? Error { get; set; }
    public string? Mensaje { get; set; }
    public double? ProteinaObjetivo { get; set; }
    public ResultadoMacros? Macros { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var cuenta = await db.Usuarios.FirstAsync(u => u.Id == usuarioActual.UsuarioId);
        if (cuenta.BienvenidaPendiente)
            return RedirectToPage("/Bienvenida");

        if (TempData["Mensaje"] is string msg)
            Mensaje = msg;
        if (TempData["Error"] is string err)
            Error = err;

        await CargarVistaAsync();
        return Page();
    }

    public Task<IActionResult> OnPostAgregarSlotAsync(int tipoComidaId, double kcal = 0, string? nombre = null)
        => Ejecutar(() => dias.AgregarSlotAsync(Fecha, tipoComidaId, Eleccion, kcal, nombre));

    public Task<IActionResult> OnPostComboAsync(int comboId)
        => Ejecutar(() => dias.AgregarComboAsync(Fecha, comboId, TipoComidaId == 0 ? null : TipoComidaId));

    public Task<IActionResult> OnPostAlimentoAsync(int alimentoId)
        => Ejecutar(() => dias.AgregarAlimentoAsync(Fecha, TipoComidaId, alimentoId));

    public async Task<IActionResult> OnPostQuitarAsync(int itemId)
    {
        await dias.QuitarItemAsync(itemId, Fecha);
        return await ResponderHoyAsync();
    }

    public Task<IActionResult> OnPostCantidadAsync(int itemId, double cantidad = 0, double porcentaje = 0)
        => Ejecutar(() =>
        {
            var v = FraccionHecha.De(cantidad, porcentaje);
            return v is null
                ? Task.FromResult<string?>("La fracción tiene que estar entre 10 % y 2000 %.")
                : dias.CambiarCantidadItemAsync(itemId, v.Value);
        });

    public Task<IActionResult> OnPostCantidadGrupoAsync(string grupoId, double cantidad = 0, double porcentaje = 0)
        => Ejecutar(() =>
        {
            var v = FraccionHecha.De(cantidad, porcentaje);
            return v is null
                ? Task.FromResult<string?>("La fracción tiene que estar entre 10 % y 2000 %.")
                : dias.CambiarCantidadGrupoAsync(grupoId, v.Value, Fecha);
        });

    public async Task<IActionResult> OnPostQuitarGrupoAsync(string grupoId)
    {
        await dias.QuitarGrupoAsync(grupoId, Fecha);
        return await ResponderHoyAsync();
    }

    public async Task<IActionResult> OnPostCantidadGymAsync(int rutinaDiaId, double cantidad = 0, double porcentaje = 0)
    {
        if (FraccionHecha.De(cantidad, porcentaje) is { } v)
            await dias.CambiarCantidadGymAsync(Fecha, rutinaDiaId, v);
        return await ResponderHoyAsync();
    }

    public async Task<IActionResult> OnPostCantidadCardioAsync(int protocoloId, double cantidad = 0, double porcentaje = 0)
    {
        if (FraccionHecha.De(cantidad, porcentaje) is { } v)
            await dias.CambiarCantidadCardioAsync(Fecha, protocoloId, v);
        return await ResponderHoyAsync();
    }

    public async Task<IActionResult> OnPostCantidadActividadAsync(int actividadId, double cantidad = 0, double porcentaje = 0)
    {
        if (FraccionHecha.De(cantidad, porcentaje) is { } v)
            await dias.CambiarCantidadActividadAsync(Fecha, actividadId, v);
        return await ResponderHoyAsync();
    }

    public async Task<IActionResult> OnPostVaciarAsync()
    {
        await dias.VaciarDiaAsync(Fecha);
        return await ResponderHoyAsync();
    }

    public Task<IActionResult> OnPostCopiarAsync()
        => Ejecutar(() => dias.CopiarDiaAsync(CopiarDesde, Fecha));

    public async Task<IActionResult> OnPostActividadAsync(int actividadId)
    {
        await dias.AlternarActividadAsync(Fecha, actividadId);
        return await ResponderHoyAsync();
    }

    public async Task<IActionResult> OnPostRutinaAsync(int rutinaDiaId)
    {
        await dias.AlternarDiaRutinaAsync(Fecha, rutinaDiaId);
        return await ResponderHoyAsync();
    }

    public async Task<IActionResult> OnPostCardioAsync(int protocoloId)
    {
        await dias.AlternarProtocoloAsync(Fecha, protocoloId);
        return await ResponderHoyAsync();
    }

    public Task<IActionResult> OnPostKcalGymAsync(double kcal, string? nombre = null)
        => Ejecutar(() => dias.AgregarKcalGymAsync(Fecha, kcal, nombre));

    public Task<IActionResult> OnPostKcalCardioAsync(double kcal, string? nombre = null)
        => Ejecutar(() => dias.AgregarKcalCardioAsync(Fecha, kcal, nombre));

    public Task<IActionResult> OnPostKcalDeporteAsync(double kcal, string? nombre = null)
        => Ejecutar(() => dias.AgregarKcalDeporteAsync(Fecha, kcal, nombre));

    public async Task<IActionResult> OnPostQuitarGymSueltoAsync(int sesionId)
    {
        await dias.QuitarGymSueltoAsync(sesionId, Fecha);
        return await ResponderHoyAsync();
    }

    public async Task<IActionResult> OnPostQuitarCardioSueltoAsync(int sesionId)
    {
        await dias.QuitarCardioSueltoAsync(sesionId, Fecha);
        return await ResponderHoyAsync();
    }

    public async Task<IActionResult> OnPostQuitarDeporteSueltoAsync(int logId)
    {
        await dias.QuitarDeporteSueltoAsync(logId, Fecha);
        return await ResponderHoyAsync();
    }

    private async Task<IActionResult> Ejecutar(Func<Task<string?>> accion)
    {
        Error = await accion();
        if (Error is not null)
        {
            await CargarVistaAsync();
            if (EsHtmx)
                return Partial(PartialHoy(), this);
            return Page();
        }
        return await ResponderHoyAsync();
    }

    private async Task<IActionResult> ResponderHoyAsync()
    {
        if (EsHtmx)
        {
            await CargarVistaAsync();
            return Partial(PartialHoy(), this);
        }
        return RedirectHoy();
    }

    private string PartialHoy()
    {
        var destino = Request.Headers["HX-Target"].ToString();
        return destino.Contains("entreno", StringComparison.OrdinalIgnoreCase)
            ? "_HoyEntreno"
            : "_HoyComidas";
    }

    private async Task CargarVistaAsync()
    {
        Vista = await dias.CargarAsync(Fecha, Q);
        var perfil = await db.Perfiles.SingleAsync();
        ProteinaObjetivo = ProteinaNecesaria.Objetivo(
            perfil.ProteinaObjetivoG, Vista.Medicion?.PesoKg, Vista.Medicion?.GrasaPct, perfil.TipoObjetivo);
        Macros = MacrosNecesarios.De(
            Vista.GastoSinDeporte,
            Vista.Deficit?.Diario ?? 0,
            Vista.Deficit?.EsSuperavit == true,
            ProteinaObjetivo,
            Vista.Medicion?.PesoKg,
            perfil.TipoObjetivo);
        if (CopiarDesde == default)
            CopiarDesde = Fecha.AddDays(-1);
    }

    private bool EsHtmx => Request.Headers.ContainsKey("HX-Request");

    private IActionResult RedirectHoy()
        => RedirectToPage("/Index", new { Fecha = Fecha.ToString("yyyy-MM-dd"), TipoComidaId, Q });
}
