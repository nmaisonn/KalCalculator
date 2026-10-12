using System.ComponentModel.DataAnnotations;
using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class MedicionesModel(ServicioMediciones mediciones, KcalDbContext db) : PageModel
{
    public IReadOnlyList<Medicion> Lista { get; private set; } = [];
    public ReferenciaMedicion? Referencia { get; private set; }
    public Medicion? Confirmacion { get; private set; }
    public string? AvisoEliminar { get; private set; }
    public VistaAvance? Historial { get; private set; }
    public VistaAvance? Avance { get; private set; }
    public DateOnly? FechaInicioObjetivo { get; private set; }

    [BindProperty(SupportsGet = true)] public string? Tab { get; set; }
    [BindProperty] public int Id { get; set; }
    [BindProperty, Required] public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty, Required] public double PesoKg { get; set; }
    [BindProperty] public double? GrasaPct { get; set; }
    [BindProperty] public double? MusculoPct { get; set; }
    [BindProperty] public double? Visceral { get; set; }
    [BindProperty] public string? Nota { get; set; }
    [BindProperty] public bool EsComparable { get; set; } = true;

    public string? Mensaje { get; private set; }
    public string? Error { get; private set; }
    public bool Editando => Id != 0;
    public bool EsLaReferencia { get; private set; }
    public bool EsAvance => string.Equals(Tab, "avance", StringComparison.OrdinalIgnoreCase);

    public async Task OnGetAsync(int? id, int? eliminar)
    {
        await CargarAsync();
        if (eliminar is > 0)
        {
            Confirmacion = Lista.FirstOrDefault(m => m.Id == eliminar);
            if (Confirmacion is not null)
                AvisoEliminar = await TextoCambioDeReferenciaAsync(Confirmacion);
            return;
        }

        if (id is > 0)
        {
            var m = Lista.FirstOrDefault(x => x.Id == id);
            if (m is not null) CargarFormulario(m);
        }
        else
        {
            EsComparable = true;
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        if (!ModelState.IsValid)
        {
            await CargarAsync();
            return Page();
        }

        var resultado = await mediciones.GuardarAsync(new Medicion
        {
            Id = Id,
            Fecha = Fecha,
            PesoKg = PesoKg,
            GrasaPct = GrasaPct,
            MusculoPct = MusculoPct,
            Visceral = Visceral,
            EsComparable = EsComparable,
            Nota = Nota
        });

        if (!resultado.Ok)
        {
            Error = resultado.Error;
            await CargarAsync();
            return Page();
        }

        TempData["Mensaje"] = resultado.Aviso ?? "Medición guardada.";
        return Volver();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var resultado = await mediciones.EliminarAsync(id);
        TempData["Mensaje"] = resultado.Ok
            ? resultado.Aviso ?? "Medición eliminada."
            : resultado.Error;
        return Volver();
    }

    public async Task<IActionResult> OnPostReferenciaAsync(int id)
    {
        await mediciones.MarcarReferenciaAsync(id);
        TempData["Mensaje"] = "Esa medición es ahora la referencia del gasto y del plan.";
        return Volver();
    }

    public async Task<IActionResult> OnPostComparableAsync(int id, bool on)
    {
        await mediciones.MarcarComparableAsync(id, on);
        TempData["Mensaje"] = on
            ? "Esa medición entra en la comparación con la báscula."
            : "Esa medición queda guardada, pero no entra en la comparación.";
        return Volver();
    }

    private IActionResult Volver()
        => EsAvance ? RedirectToPage(new { tab = "avance" }) : RedirectToPage();

    private async Task CargarAsync()
    {
        Lista = await mediciones.ListarAsync();
        Referencia = await mediciones.ReferenciaAsync();
        EsLaReferencia = Referencia is not null && Referencia.Medicion.Id == Id;
        if (TempData["Mensaje"] is string m) Mensaje = m;

        var perfil = await db.Perfiles.SingleAsync();
        FechaInicioObjetivo = perfil.FechaInicioObjetivo;
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var ancla = Referencia?.Medicion is { EsReferencia: true } r ? r : null;
        Historial = AvanceComposicion.De(Lista, ancla?.Fecha, hoy, perfil.GrasaObjetivoPct, ancla);
        Avance = AvanceComposicion.De(
            Lista, ancla?.Fecha ?? FechaInicioObjetivo, hoy, perfil.GrasaObjetivoPct, ancla);
    }

    private void CargarFormulario(Medicion m)
    {
        Id = m.Id;
        Fecha = m.Fecha;
        PesoKg = m.PesoKg;
        GrasaPct = m.GrasaPct;
        MusculoPct = m.MusculoPct;
        Visceral = m.Visceral;
        Nota = m.Nota;
        EsComparable = m.EsComparable;
        EsLaReferencia = Referencia?.Medicion.Id == m.Id;
    }

    private async Task<string?> TextoCambioDeReferenciaAsync(Medicion actual)
    {
        if (Referencia?.Medicion.Id != actual.Id)
            return null;

        var siguiente = Lista.FirstOrDefault(m => m.Id != actual.Id);
        if (siguiente is null)
            return "Es la única medición. Si la eliminás, no va a haber referencia para el gasto basal.";

        var bmrAhora = (await mediciones.BmrDeAsync(actual)).Kcal is { } k ? Redondeo.Kcal(k) : (int?)null;
        var bmrNuevo = (await mediciones.BmrDeAsync(siguiente)).Kcal is { } n ? Redondeo.Kcal(n) : (int?)null;
        var grasa = siguiente.GrasaPct is { } g ? $", {g.ToString("0.#")} %" : "";
        return $"Es la más reciente. La referencia pasará a ser la del {siguiente.Fecha:yyyy-MM-dd} ({siguiente.PesoKg.ToString("0.#")} kg{grasa}), y el gasto basal pasará de {bmrAhora?.ToString() ?? "—"} a {bmrNuevo?.ToString() ?? "—"} kcal.";
    }
}
