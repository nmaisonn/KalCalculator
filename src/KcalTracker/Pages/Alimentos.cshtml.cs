using KcalTracker;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class AlimentosModel(
    KcalDbContext db,
    ServicioAlimentos alimentos,
    ServicioNutricion nutricion,
    ServicioImportacionAlimentos importacion) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Filtro { get; set; } = "todos";

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Tipo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Vuelta { get; set; }

    public FuenteAlimentos Fuente { get; private set; } = FuenteAlimentos.Propios;
    public int UsuarioIdCatalogo { get; private set; }
    public IReadOnlyList<Alimento> Lista { get; private set; } = [];
    public int TotalLista { get; private set; }
    public IReadOnlyList<TipoComida> Tipos { get; private set; } = [];
    public IReadOnlyList<CategoriaAlimento> Categorias { get; private set; } = [];
    public Alimento? Editando { get; private set; }
    public IReadOnlyList<SugerenciaNutricional> Sugerencias { get; private set; } = [];
    public string? ErrorBusqueda { get; private set; }
    public string? EstadoFuentes { get; private set; }
    public string? Mensaje { get; private set; }
    public string? Error { get; private set; }
    public IReadOnlyList<string> ErroresImportacion { get; private set; } = [];

    [BindProperty] public int Id { get; set; }
    [BindProperty] public string Nombre { get; set; } = "";
    [BindProperty] public string Porcion { get; set; } = "";
    [BindProperty] public double Kcal { get; set; }
    [BindProperty] public double? Proteina { get; set; }
    [BindProperty] public double? Carbos { get; set; }
    [BindProperty] public double? Grasa { get; set; }
    [BindProperty] public bool EsAfuera { get; set; }
    [BindProperty] public int? TipoComidaId { get; set; }
    [BindProperty] public int? CategoriaAlimentoId { get; set; }
    [BindProperty] public OrigenDato Origen { get; set; } = OrigenDato.Manual;
    [BindProperty] public string? Ext { get; set; }
    [BindProperty] public IFormFile? Archivo { get; set; }

    public async Task OnGetAsync(int? id, bool nuevo = false, string? nombre = null, string? porcion = null,
        double? kcal = null, double? proteina = null, double? carbos = null, double? grasa = null, string? origen = null)
    {
        await CargarListaAsync();
        if (nuevo)
        {
            Editando = new Alimento();
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                Nombre = nombre; Porcion = porcion ?? ""; Kcal = kcal ?? 0;
                Proteina = proteina; Carbos = carbos; Grasa = grasa;
                Origen = origen switch
                {
                    "usda" => OrigenDato.Usda,
                    "ia" => OrigenDato.ModeloLenguaje,
                    "off" => OrigenDato.OpenFoodFacts,
                    _ => OrigenDato.Manual
                };
            }
        }
        else if (id is { } i)
        {
            Editando = Lista.FirstOrDefault(a => a.Id == i) ?? await alimentos.PorIdAsync(i);
            if (Editando is { } a)
            {
                if (CatalogoAlimentos.EsDeLaApp(a, db.UsuarioIdCatalogo))
                {
                    Editando = null;
                }
                else
                {
                    Id = a.Id; Nombre = a.Nombre; Porcion = a.Porcion; Kcal = a.Kcal;
                    Proteina = a.Proteina; Carbos = a.Carbos; Grasa = a.Grasa; EsAfuera = a.EsAfuera;
                    TipoComidaId = a.TipoComidaId;
                    CategoriaAlimentoId = a.CategoriaAlimentoId;
                    Origen = a.Origen;
                }
            }
        }
    }

    public IActionResult OnGetPlantilla()
        => File(ServicioImportacionAlimentos.PlantillaXlsx(), ServicioImportacionAlimentos.TipoContenidoXlsx,
            ServicioImportacionAlimentos.NombrePlantilla);

    public async Task<IActionResult> OnPostCalcularAsync()
    {
        await CargarListaAsync();
        var (res, estado, err) = await nutricion.CalcularAsync(Ext ?? Q ?? "", incluirModelo: true);
        Sugerencias = res;
        EstadoFuentes = estado;
        ErrorBusqueda = err;
        return Page();
    }

    public async Task<IActionResult> OnPostBuscarCatalogosAsync()
    {
        await CargarListaAsync();
        var (res, estado, err) = await nutricion.BuscarAsync(Ext ?? "");
        Sugerencias = res;
        EstadoFuentes = estado;
        ErrorBusqueda = err;
        return Page();
    }

    public async Task<IActionResult> OnPostImportarAsync()
    {
        if (Archivo is null || Archivo.Length == 0)
        {
            Error = "Elegí un archivo Excel o CSV.";
            await CargarListaAsync();
            return Page();
        }

        await using var stream = Archivo.OpenReadStream();
        var r = await importacion.ImportarAsync(stream, Archivo.FileName);
        ErroresImportacion = r.Errores;
        if (r.Agregados == 0 && r.YaEstaban == 0 && r.Errores.Count > 0)
        {
            Error = r.Errores[0];
            await CargarListaAsync();
            return Page();
        }

        var partes = new List<string>();
        if (r.Agregados > 0) partes.Add(r.Agregados == 1 ? "1 alimento nuevo" : $"{r.Agregados} alimentos nuevos");
        if (r.YaEstaban > 0) partes.Add(r.YaEstaban == 1 ? "1 ya estaba" : $"{r.YaEstaban} ya estaban");
        TempData["Mensaje"] = partes.Count == 0 ? "No se importó nada." : "Se importó: " + string.Join(", ", partes) + ".";
        if (r.Errores.Count > 0)
            TempData["Mensaje"] += " " + r.Errores.Count + (r.Errores.Count == 1 ? " aviso." : " avisos.");
        if (r.Agregados > 0)
            await PasarAAmbosSiSoloCatalogoAsync();
        return RedirectToPage(new { Filtro, Q, Tipo, Vuelta });
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        var (ok, error, aviso) = await alimentos.GuardarAsync(new Alimento
        {
            Id = Id, Nombre = Nombre, Porcion = Porcion, Kcal = Kcal,
            Proteina = Proteina, Carbos = Carbos, Grasa = Grasa, EsAfuera = EsAfuera, Activo = true,
            TipoComidaId = TipoComidaId is > 0 ? TipoComidaId : null,
            CategoriaAlimentoId = CategoriaAlimentoId is > 0 ? CategoriaAlimentoId : null,
            Origen = Origen == 0 ? OrigenDato.Manual : Origen
        });
        if (!ok)
        {
            Error = error;
            await CargarListaAsync();
            Editando = new Alimento { Id = Id };
            return Page();
        }
        TempData["Mensaje"] = aviso ?? "Alimento guardado.";
        await PasarAAmbosSiSoloCatalogoAsync();
        return Volver.A(this, Vuelta);
    }

    public async Task<IActionResult> OnPostBorrarAsync(int id)
    {
        var a = await db.Alimentos.FirstOrDefaultAsync(x => x.Id == id);
        if (a is not null && !db.EsDelCatalogo(a.UsuarioId))
        {
            a.Activo = false;
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { Filtro, Q, Tipo });
    }

    public async Task<IActionResult> OnPostFuenteAsync(FuenteAlimentos fuente)
    {
        var perfil = await db.Perfiles.SingleAsync();
        perfil.FuenteAlimentos = fuente;
        await db.SaveChangesAsync();
        return RedirectToPage(new { Filtro, Q, Tipo, Vuelta });
    }

    private async Task CargarListaAsync()
    {
        await new ServicioCatalogos(db).AsegurarAfueraAsync();
        var perfil = await db.Perfiles.SingleAsync();
        Fuente = perfil.FuenteAlimentos;
        UsuarioIdCatalogo = db.UsuarioIdCatalogo;
        Tipos = await db.TiposComida.Where(t => t.Activo).OrderBy(t => t.Orden).ToListAsync();
        Categorias = await db.CategoriasAlimento
            .Where(c => c.Activo && c.UsuarioId == db.UsuarioIdActual)
            .OrderBy(c => c.Orden).ToListAsync();
        var q = CatalogoAlimentos.Visibles(
            db.Alimentos.Include(a => a.TipoComida).Include(a => a.CategoriaAlimento).Where(a => a.Activo),
            db.UsuarioIdActual, db.UsuarioIdCatalogo, Fuente);
        Filtro = (Filtro ?? "todos").ToLowerInvariant();
        q = Filtro switch
        {
            "macros" => q.Where(a => !a.EsAfuera && a.Proteina != null),
            "afuera" => q.Where(a => a.EsAfuera),
            _ => q
        };
        var lista = CatalogoAlimentos.SinDuplicarCatalogo(await q.ToListAsync(), db.UsuarioIdActual).ToList();
        lista = lista
            .OrderBy(a => a.CategoriaAlimento?.Orden ?? int.MaxValue)
            .ThenBy(a => a.Nombre)
            .ToList();
        TotalLista = lista.Count;

        var busqueda = (Q ?? "").Trim();
        if (busqueda.Length > 0)
        {
            lista = lista.Where(a => Contiene(a.Nombre, busqueda) || Contiene(a.Porcion, busqueda)).ToList();
        }

        var tipo = (Tipo ?? "").Trim();
        if (tipo is "sin")
            lista = lista.Where(a => a.TipoComidaId is null).ToList();
        else if (int.TryParse(tipo, out var tipoId) && tipoId > 0)
            lista = lista.Where(a => a.TipoComidaId == tipoId).ToList();

        Lista = lista;
        if (TempData["Mensaje"] is string m && Mensaje is null) Mensaje = m;
    }

    private async Task PasarAAmbosSiSoloCatalogoAsync()
    {
        var perfil = await db.Perfiles.SingleAsync();
        if (perfil.FuenteAlimentos != FuenteAlimentos.Catalogo) return;
        perfil.FuenteAlimentos = FuenteAlimentos.Ambos;
        await db.SaveChangesAsync();
    }

    private static bool Contiene(string texto, string q)
        => ServicioImportacionAlimentos.TextoContiene(texto, q);
}
