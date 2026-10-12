using KcalTracker;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class ComboModel(KcalDbContext db, ServicioAlimentos alimentos) : PageModel
{
    public IReadOnlyList<Alimento> Catalogo { get; private set; } = [];
    public IReadOnlyList<TipoComida> Tipos { get; private set; } = [];
    public IReadOnlyList<ComboVista> Lista { get; private set; } = [];
    public bool Formulario { get; private set; }
    public string? Error { get; private set; }
    public string? Mensaje { get; private set; }
    public double KcalEstimadas { get; private set; }

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Nombre { get; set; } = "";
    [BindProperty] public int? TipoComidaId { get; set; }
    [BindProperty] public List<int> AlimentoIds { get; set; } = [];
    [BindProperty] public List<double> Cantidades { get; set; } = [];
    [BindProperty] public int NuevoAlimentoId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Vuelta { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id, bool nuevo = false, int? tipoComidaId = null)
    {
        if (TempData["Mensaje"] is string m) Mensaje = m;
        await CargarCatalogoAsync();
        if (nuevo)
        {
            Formulario = true;
            if (tipoComidaId is { } tipo) TipoComidaId = tipo;
            return Page();
        }
        if (id is null)
        {
            await CargarListaAsync();
            return Page();
        }

        var combo = await db.Combos.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id);
        if (combo is null) return RedirectToPage("/Combo");

        Formulario = true;
        Id = combo.Id;
        Nombre = combo.Nombre;
        TipoComidaId = combo.TipoComidaId;
        AlimentoIds = combo.Items.Select(i => i.AlimentoId).ToList();
        Cantidades = combo.Items.Select(i => i.Cantidad).ToList();
        Recalcular();
        return Page();
    }

    public async Task<IActionResult> OnPostAgregarAsync()
    {
        Formulario = true;
        await CargarCatalogoAsync();
        if (NuevoAlimentoId != 0 && !AlimentoIds.Contains(NuevoAlimentoId))
        {
            AlimentoIds.Add(NuevoAlimentoId);
            Cantidades.Add(1);
        }
        Recalcular();
        return Page();
    }

    public async Task<IActionResult> OnPostQuitarAsync(int i)
    {
        Formulario = true;
        await CargarCatalogoAsync();
        if (i >= 0 && i < AlimentoIds.Count)
        {
            AlimentoIds.RemoveAt(i);
            if (i < Cantidades.Count) Cantidades.RemoveAt(i);
        }
        Recalcular();
        return Page();
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        Formulario = true;
        await CargarCatalogoAsync();
        Recalcular();
        var items = new List<(int, double)>();
        for (var i = 0; i < AlimentoIds.Count; i++)
        {
            var cant = i < Cantidades.Count ? Cantidades[i] : 1;
            if (cant <= 0) cant = 1;
            items.Add((AlimentoIds[i], cant));
        }

        var (ok, error) = await alimentos.GuardarComboAsync(new Combo
        {
            Id = Id,
            Nombre = Nombre,
            TipoComidaId = TipoComidaId == 0 ? null : TipoComidaId,
            Activo = true
        }, items);

        if (!ok)
        {
            Error = error;
            return Page();
        }
        TempData["Mensaje"] = "Combo guardado.";
        return Volver.A(this, Vuelta, "/Combo");
    }

    public async Task<IActionResult> OnPostBorrarAsync()
    {
        var combo = await db.Combos.FirstOrDefaultAsync(c => c.Id == Id);
        if (combo is not null)
        {
            combo.Activo = false;
            await db.SaveChangesAsync();
            TempData["Mensaje"] = "Combo desactivado.";
        }
        return Volver.A(this, Vuelta, "/Combo");
    }

    private async Task CargarCatalogoAsync()
    {
        var fuente = await db.Perfiles.Select(p => p.FuenteAlimentos).SingleAsync();
        Catalogo = (await CatalogoAlimentos.CargarAsync(
                db.Alimentos.Where(a => a.Activo),
                db.UsuarioIdActual, db.UsuarioIdCatalogo, fuente))
            .OrderBy(a => a.Nombre)
            .ToList();
        Tipos = await db.TiposComida.Where(t => t.Activo).OrderBy(t => t.Orden).ToListAsync();
    }

    private async Task CargarListaAsync()
    {
        var combos = await db.Combos.Include(c => c.Items).ThenInclude(i => i.Alimento)
            .Include(c => c.TipoComida).Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();
        Lista = combos.Select(c => new ComboVista(
            c.Id, c.Nombre, c.TipoComida?.Nombre,
            string.Join(" + ", c.Items.Select(i => i.Alimento?.Nombre ?? "")),
            c.Items.Sum(i => (i.Alimento?.Kcal ?? 0) * i.Cantidad))).ToList();
    }

    private void Recalcular()
    {
        KcalEstimadas = 0;
        for (var i = 0; i < AlimentoIds.Count; i++)
        {
            var a = Catalogo.FirstOrDefault(x => x.Id == AlimentoIds[i]);
            var cant = i < Cantidades.Count ? Cantidades[i] : 1;
            if (a is not null) KcalEstimadas += a.Kcal * cant;
        }
    }

    public sealed record ComboVista(int Id, string Nombre, string? Tipo, string Composicion, double Kcal);
}
