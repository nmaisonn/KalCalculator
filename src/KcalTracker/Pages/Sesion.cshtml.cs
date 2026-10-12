using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class SesionModel(KcalDbContext db, ServicioDia dias, ServicioGym gym) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public SesionGym? Sesion { get; private set; }
    public IReadOnlyList<RutinaDia> Dias { get; private set; } = [];
    public IReadOnlyList<PuntoProgresion>? Puntos { get; private set; }
    public ResumenProgresion? Resumen { get; private set; }
    public GraficoProgresion? Grafico { get; private set; }
    public string? EjercicioNombre { get; private set; }

    [BindProperty(SupportsGet = true, Name = "ej")]
    public int? EjercicioSel { get; set; }

    [BindProperty] public int SesionId { get; set; }
    [BindProperty] public double? Kcal { get; set; }
    [BindProperty] public double Cantidad { get; set; } = 1;
    [BindProperty] public List<int> EjId { get; set; } = [];
    [BindProperty] public List<int> Series { get; set; } = [];
    [BindProperty] public List<int?> Reps { get; set; } = [];
    [BindProperty] public List<double?> Peso { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int? dia, int? id)
    {
        var rutina = await db.Rutinas.Include(r => r.Dias).FirstOrDefaultAsync(r => r.EsActiva);
        Dias = rutina?.Dias.OrderBy(d => d.Orden).ToList() ?? [];

        if (id is { } sid)
            Sesion = await gym.PorIdAsync(sid);
        else
        {
            var lista = await gym.DeFechaAsync(Fecha);
            Sesion = dia is { } diaId ? lista.FirstOrDefault(s => s.RutinaDiaId == diaId) : lista.FirstOrDefault();
        }

        if (Sesion is not null)
        {
            SesionId = Sesion.Id;
            Kcal = Sesion.Kcal;
            Cantidad = Sesion.Cantidad <= 0 ? 1 : Sesion.Cantidad;
            Fecha = Sesion.Fecha;
            await CargarProgresionAsync(Sesion);
        }
        if (dia is { } precargarDia && Sesion is null)
        {
            var creada = await dias.PrecargarSesionAsync(Fecha, precargarDia);
            return RedirectToPage(new { id = creada.Id });
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAbrirAsync(int dia)
    {
        var existente = (await gym.DeFechaAsync(Fecha)).FirstOrDefault(s => s.RutinaDiaId == dia);
        if (existente is null)
            existente = await dias.PrecargarSesionAsync(Fecha, dia);
        return RedirectToPage(new { id = existente.Id });
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        var s = await gym.PorIdAsync(SesionId);
        if (s is null) return RedirectToPage();
        var nueva = Cantidad > 0 ? Cantidad : 1;
        var vieja = s.Cantidad <= 0 ? 1 : s.Cantidad;
        if (Math.Abs(nueva - vieja) > 0.01)
        {
            var f = nueva / vieja;
            s.Cantidad = nueva;
            var baseKcal = Kcal ?? s.Kcal;
            s.Kcal = baseKcal is { } k ? Redondeo.Kcal(k * f) : null;
        }
        else
            s.Kcal = Kcal;
        s.Fecha = Fecha;
        for (var i = 0; i < EjId.Count; i++)
        {
            var e = s.Ejercicios.FirstOrDefault(x => x.Id == EjId[i]);
            if (e is null) continue;
            if (i < Series.Count) e.Series = Series[i];
            if (i < Reps.Count) e.Reps = Reps[i];
            if (i < Peso.Count) e.Peso = Peso[i];
        }
        await gym.GuardarAsync(s);
        return RedirectToPage(new { id = s.Id, ej = EjercicioSel });
    }

    private async Task CargarProgresionAsync(SesionGym sesion)
    {
        var lista = sesion.Ejercicios.OrderBy(x => x.Orden).ToList();
        var linea = lista.FirstOrDefault(x => x.Id == EjercicioSel)
            ?? lista.FirstOrDefault(x => x.EjercicioId is not null);
        EjercicioSel = linea?.Id;
        EjercicioNombre = linea?.Nombre;
        if (linea?.EjercicioId is not { } eid) return;
        Puntos = await gym.ProgresionAsync(eid);
        Resumen = Progresion.Resumir(Puntos);
        Grafico = Progresion.Grafico(Puntos);
    }

    public async Task<IActionResult> OnPostBorrarAsync(int id)
    {
        await gym.EliminarAsync(id);
        return RedirectToPage("/Index", new { Fecha });
    }
}
