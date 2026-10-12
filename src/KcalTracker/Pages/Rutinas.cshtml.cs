using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class RutinasModel(KcalDbContext db, ServicioRutinas rutinas) : PageModel
{
    public IReadOnlyList<Rutina> Rutinas { get; private set; } = [];
    public IReadOnlyList<Ejercicio> Catalogo { get; private set; } = [];
    public Rutina? Elegida { get; private set; }
    public bool Nueva { get; private set; }
    public string? Mensaje { get; private set; }
    public string? Error { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Vuelta { get; set; }

    [BindProperty] public string Nombre { get; set; } = "";
    [BindProperty] public string? Descripcion { get; set; }
    [BindProperty] public string DiaNombre { get; set; } = "";
    [BindProperty] public double? DiaKcal { get; set; }
    [BindProperty] public int EjercicioId { get; set; }
    [BindProperty] public int Series { get; set; } = 3;
    [BindProperty] public int RepsMin { get; set; } = 8;
    [BindProperty] public int? RepsMax { get; set; } = 10;
    [BindProperty] public double? PesoRef { get; set; }
    [BindProperty] public bool PuedeMas { get; set; }
    [BindProperty] public string? Observacion { get; set; }

    public async Task OnGetAsync(int? id, bool nuevo = false)
    {
        Nueva = nuevo;
        await CargarAsync(id);
        if (Elegida is { } r && !nuevo)
        {
            Nombre = r.Nombre;
            Descripcion = r.Descripcion;
        }
    }

    public async Task<IActionResult> OnPostCrearAsync()
    {
        var (ok, error, creada) = await rutinas.CrearAsync(Nombre, Descripcion, activar: false);
        if (!ok)
        {
            Error = error;
            Nueva = true;
            await CargarAsync(null);
            return Page();
        }
        TempData["Mensaje"] = "Rutina creada. Agregá los días y los ejercicios.";
        return RedirectToPage(new { id = creada!.Id, Vuelta });
    }

    public async Task<IActionResult> OnPostGuardarDatosAsync(int id)
    {
        var (ok, error) = await rutinas.GuardarDatosAsync(id, Nombre, Descripcion);
        TempData[ok ? "Mensaje" : "Error"] = ok
            ? "Datos guardados. Las sesiones ya registradas no cambian."
            : error;
        return RedirectToPage(new { id, Vuelta });
    }

    public async Task<IActionResult> OnPostActivarAsync(int id)
    {
        var (ok, error) = await rutinas.ActivarAsync(id);
        TempData[ok ? "Mensaje" : "Error"] = ok ? "Esta es ahora la rutina activa." : error;
        return RedirectToPage(new { id, Vuelta });
    }

    public async Task<IActionResult> OnPostDuplicarAsync(int id)
    {
        var (ok, error, copia) = await rutinas.DuplicarAsync(id);
        if (!ok)
        {
            TempData["Error"] = error;
            return RedirectToPage(new { id, Vuelta });
        }
        TempData["Mensaje"] = "Copia lista. Podés editarla sin tocar la original.";
        return RedirectToPage(new { id = copia!.Id, Vuelta });
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var (ok, error) = await rutinas.EliminarAsync(id);
        TempData[ok ? "Mensaje" : "Error"] = ok
            ? "Rutina eliminada. Las sesiones ya hechas quedan en el histórico."
            : error;
        return RedirectToPage(new { Vuelta });
    }

    public async Task<IActionResult> OnPostAgregarDiaAsync(int id)
    {
        var (ok, error) = await rutinas.AgregarDiaAsync(id, DiaNombre, DiaKcal);
        TempData[ok ? "Mensaje" : "Error"] = ok ? "Día agregado." : error;
        return RedirectToPage(new { id, Vuelta });
    }

    public async Task<IActionResult> OnPostGuardarDiaAsync(int id, int diaId)
    {
        var (ok, error) = await rutinas.GuardarDiaAsync(diaId, DiaNombre, DiaKcal);
        TempData[ok ? "Mensaje" : "Error"] = ok ? "Día actualizado. Las sesiones ya registradas no cambian." : error;
        return RedirectToPage(new { id, Vuelta });
    }

    public async Task<IActionResult> OnPostQuitarDiaAsync(int id, int diaId)
    {
        var (ok, error) = await rutinas.QuitarDiaAsync(diaId);
        TempData[ok ? "Mensaje" : "Error"] = ok ? "Día quitado." : error;
        return RedirectToPage(new { id, Vuelta });
    }

    public async Task<IActionResult> OnPostAgregarEjercicioAsync(int id, int diaId)
    {
        var (ok, error) = await rutinas.AgregarEjercicioAsync(
            diaId, EjercicioId, Series, RepsMin, RepsMax, PesoRef, PuedeMas, Observacion);
        TempData[ok ? "Mensaje" : "Error"] = ok ? "Ejercicio agregado." : error;
        return RedirectToPage(new { id, Vuelta });
    }

    public async Task<IActionResult> OnPostGuardarEjercicioAsync(int id, int itemId)
    {
        var (ok, error) = await rutinas.GuardarEjercicioAsync(
            itemId, Series, RepsMin, RepsMax, PesoRef, PuedeMas, Observacion);
        TempData[ok ? "Mensaje" : "Error"] = ok ? "Ejercicio actualizado. Las sesiones ya registradas no cambian." : error;
        return RedirectToPage(new { id, Vuelta });
    }

    public async Task<IActionResult> OnPostQuitarEjercicioAsync(int id, int itemId)
    {
        var (ok, error) = await rutinas.QuitarEjercicioAsync(itemId);
        TempData[ok ? "Mensaje" : "Error"] = ok ? "Ejercicio quitado de este día." : error;
        return RedirectToPage(new { id, Vuelta });
    }

    public async Task<IActionResult> OnPostReordenarEjerciciosAsync(int id, int diaId, [FromForm] int[] ids)
    {
        var (ok, error) = await rutinas.ReordenarEjerciciosAsync(diaId, ids ?? []);
        if (!ok)
            return BadRequest(error ?? "No se pudo reordenar.");
        return new EmptyResult();
    }

    private async Task CargarAsync(int? id)
    {
        Rutinas = await db.Rutinas
            .Include(r => r.Dias)
                .ThenInclude(d => d.Ejercicios)
                    .ThenInclude(e => e.Ejercicio)
                        .ThenInclude(x => x!.GrupoMuscular)
            .OrderByDescending(r => r.EsActiva)
            .ThenBy(r => r.Nombre)
            .ToListAsync();

        Catalogo = await db.Ejercicios.Include(e => e.GrupoMuscular)
            .Where(e => e.Activo)
            .OrderBy(e => e.GrupoMuscular!.Orden).ThenBy(e => e.Nombre)
            .ToListAsync();

        Elegida = id is { } i
            ? Rutinas.FirstOrDefault(r => r.Id == i)
            : Nueva ? null : Rutinas.FirstOrDefault(r => r.EsActiva) ?? Rutinas.FirstOrDefault();

        if (TempData["Mensaje"] is string m) Mensaje = m;
        if (TempData["Error"] is string e) Error = e;
    }

    public static string Reps(RutinaEjercicio e)
        => e.RepsMax is { } max && max != e.RepsMin ? $"{e.RepsMin}/{max}" : $"{e.RepsMin}";

    public static string Peso(RutinaEjercicio e)
    {
        if (e.PesoRef is null)
            return e.Observacion ?? "—";
        var texto = $"{e.PesoRef.Value.ToString("0.##")} kg";
        if (e.PuedeMas) texto += " +";
        if (!string.IsNullOrWhiteSpace(e.Observacion)) texto += $" ({e.Observacion})";
        return texto;
    }
}
