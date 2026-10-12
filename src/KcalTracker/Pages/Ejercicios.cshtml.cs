using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class EjerciciosModel(KcalDbContext db) : PageModel
{
    public IReadOnlyList<GrupoMuscular> Grupos { get; private set; } = [];
    public IReadOnlyList<FilaEjercicio> Lista { get; private set; } = [];
    public Ejercicio? Editando { get; private set; }
    public string? Mensaje { get; private set; }
    public string? Error { get; private set; }

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Nombre { get; set; } = "";
    [BindProperty] public int? GrupoMuscularId { get; set; }

    public async Task OnGetAsync(int? id, bool nuevo = false)
    {
        await CargarAsync();
        if (nuevo) Editando = new Ejercicio();
        else if (id is { } i)
        {
            Editando = Lista.FirstOrDefault(x => x.Ejercicio.Id == i)?.Ejercicio;
            if (Editando is { } e)
            {
                Id = e.Id; Nombre = e.Nombre; GrupoMuscularId = e.GrupoMuscularId;
            }
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            Error = "Falta el nombre.";
            await CargarAsync();
            Editando = new Ejercicio { Id = Id };
            return Page();
        }

        if (Id == 0)
            db.Ejercicios.Add(new Ejercicio { Nombre = Nombre.Trim(), GrupoMuscularId = GrupoMuscularId, Activo = true });
        else
        {
            var e = await db.Ejercicios.FirstOrDefaultAsync(x => x.Id == Id);
            if (e is not null)
            {
                e.Nombre = Nombre.Trim();
                e.GrupoMuscularId = GrupoMuscularId;
            }
        }
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Ejercicio guardado. Las sesiones ya registradas no cambian.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBorrarAsync(int id)
    {
        var e = await db.Ejercicios.FirstOrDefaultAsync(x => x.Id == id);
        if (e is not null)
        {
            e.Activo = false;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    private async Task CargarAsync()
    {
        Grupos = await db.GruposMusculares.Where(g => g.Activo).OrderBy(g => g.Orden).ToListAsync();
        var ejercicios = await db.Ejercicios.Include(e => e.GrupoMuscular)
            .Where(e => e.Activo).OrderBy(e => e.GrupoMuscular!.Orden).ThenBy(e => e.Nombre).ToListAsync();
        var enRutinas = (await db.RutinaEjercicios.Select(x => new { x.EjercicioId, x.RutinaDia!.RutinaId }).ToListAsync())
            .GroupBy(x => x.EjercicioId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RutinaId).Distinct().Count());
        var enSesiones = (await db.SesionEjercicios.Where(x => x.EjercicioId != null)
            .GroupBy(x => x.EjercicioId!.Value).Select(g => new { g.Key, N = g.Count() }).ToListAsync())
            .ToDictionary(x => x.Key, x => x.N);
        Lista = ejercicios.Select(e => new FilaEjercicio(
            e,
            enRutinas.GetValueOrDefault(e.Id),
            enSesiones.GetValueOrDefault(e.Id))).ToList();
        if (TempData["Mensaje"] is string m) Mensaje = m;
    }

    public sealed record FilaEjercicio(Ejercicio Ejercicio, int Rutinas, int Sesiones);
}
