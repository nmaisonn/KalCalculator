using KcalTracker;
using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class ActividadModel(KcalDbContext db, ServicioDia dias, ServicioActividades actividades) : PageModel
{
    public double? Bmr { get; private set; }
    public double? GastoSin { get; private set; }
    public double Factor { get; private set; }
    public IReadOnlyList<Actividad> Lista { get; private set; } = [];
    public IReadOnlyList<TipoActividad> Tipos { get; private set; } = [];
    public Dictionary<int, int> Usos { get; private set; } = [];
    public Actividad? Editando { get; private set; }
    public string? Aviso { get; private set; }
    public string? Error { get; private set; }

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Nombre { get; set; } = "";
    [BindProperty] public int TipoActividadId { get; set; }
    [BindProperty] public double Kcal { get; set; }
    [BindProperty] public double? Minutos { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Vuelta { get; set; }

    public async Task OnGetAsync(int? id, bool nueva = false)
    {
        await CargarAsync();
        if (nueva) Editando = new Actividad();
        else if (id is { } i) Editando = Lista.FirstOrDefault(a => a.Id == i);
        if (Editando is { } e && e.Id != 0)
        {
            Id = e.Id; Nombre = e.Nombre; TipoActividadId = e.TipoActividadId; Kcal = e.Kcal; Minutos = e.Minutos;
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        var (ok, error, aviso) = await actividades.GuardarAsync(new Actividad
        {
            Id = Id, Nombre = Nombre, TipoActividadId = TipoActividadId, Kcal = Kcal, Minutos = Minutos, Activo = true
        });
        if (!ok)
        {
            Error = error;
            await CargarAsync();
            Editando = new Actividad { Id = Id };
            return Page();
        }
        TempData["Mensaje"] = aviso ?? "Actividad guardada.";
        return Volver.A(this, Vuelta);
    }

    public async Task<IActionResult> OnPostBorrarAsync(int id)
    {
        await actividades.EliminarAsync(id);
        return RedirectToPage();
    }

    private async Task CargarAsync()
    {
        var (_, bmr, gasto, perfil, _) = await dias.BaseAsync();
        Bmr = bmr?.Kcal is { } k ? Redondeo.Kcal(k) : null;
        GastoSin = gasto is { } g ? Redondeo.Kcal(g) : null;
        Factor = perfil.FactorActividad;
        Lista = await db.Actividades.Include(a => a.TipoActividad).Where(a => a.Activo).OrderBy(a => a.Nombre).ToListAsync();
        Tipos = await db.TiposActividad.Where(t => t.Activo).OrderBy(t => t.Orden).ToListAsync();
        Usos = await db.ActividadLogs.Where(l => l.ActividadId != null)
            .GroupBy(l => l.ActividadId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
        if (TempData["Mensaje"] is string m) Aviso = m;
    }
}
