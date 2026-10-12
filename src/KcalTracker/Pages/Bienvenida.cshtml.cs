using System.Globalization;
using System.Security.Claims;
using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Datos.Plantillas;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class BienvenidaModel(
    KcalDbContext db,
    IUsuarioActual usuarioActual,
    ServicioPlantillas plantillas,
    ServicioMediciones mediciones) : PageModel
{
    public const string PasoVos = "vos";
    public const string PasoObjetivo = "objetivo";
    public const string PasoPlantilla = "plantilla";

    [BindProperty(SupportsGet = true)]
    public bool Repetir { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Paso { get; set; } = PasoVos;

    [BindProperty]
    public string Modo { get; set; } = "plantilla";

    [BindProperty]
    public List<PartePlantilla> Partes { get; set; } = [];

    [BindProperty] public string? Nombre { get; set; }
    [BindProperty] public double? AlturaCm { get; set; }
    [BindProperty] public DateOnly? FechaNacimiento { get; set; }
    [BindProperty] public string SexoElegido { get; set; } = "";
    [BindProperty] public string FactorElegido { get; set; } = ValoresDeFabrica.FactorActividad.ToString("0.##", CultureInfo.InvariantCulture);
    [BindProperty] public double? FactorLibre { get; set; }

    [BindProperty] public DateOnly FechaMedicion { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty] public double? PesoKg { get; set; }
    [BindProperty] public double? GrasaPct { get; set; }
    [BindProperty] public double? BmrMedido { get; set; }
    [BindProperty] public int MedicionId { get; set; }

    [BindProperty] public double? GrasaObjetivoPct { get; set; } = 15;
    [BindProperty] public int PlazoSemanas { get; set; } = ValoresDeFabrica.PlazoSemanas;
    [BindProperty] public TipoObjetivo TipoObjetivo { get; set; } = TipoObjetivo.Corte;

    public PlantillaInfo Plantilla { get; private set; } = PlantillaExcel.Info;
    public IReadOnlyList<OpcionParte> Opciones { get; private set; } = [];
    public ResultadoDeficit? Deficit { get; private set; }
    public ResultadoPlan? Plan { get; private set; }
    public ResultadoAlcance? Alcance { get; private set; }
    public string? Error { get; private set; }

    public bool EsAlta => !Repetir;

    public async Task<IActionResult> OnGetAsync()
    {
        var cuenta = await CuentaAsync();
        if (!cuenta.BienvenidaPendiente && !Repetir)
            return RedirectToPage("/Index");

        NormalizarPaso();
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostGuardarVosAsync()
    {
        NormalizarPaso();
        Paso = PasoVos;
        if (PesoKg is null or <= 0)
        {
            Error = "Cargá el peso de arranque, o omití este paso.";
            await CargarOpcionesAsync();
            return Page();
        }

        var factor = FactorDesdeFormulario();
        if (factor is null or <= 0)
        {
            Error = "El factor de actividad tiene que ser un número mayor a cero.";
            await CargarOpcionesAsync();
            return Page();
        }

        var sexo = SexoElegido switch
        {
            "m" => Sexo.Masculino,
            "f" => Sexo.Femenino,
            _ => (Sexo?)null
        };

        if (BmrMedido is not > 0 && GrasaPct is not > 0
            && (AlturaCm is not > 0 || FechaNacimiento is null || sexo is null))
        {
            Error = "Sin GER de báscula ni % de grasa, hace falta altura, fecha de nacimiento y sexo para estimar el basal (Mifflin-St Jeor).";
            await CargarOpcionesAsync();
            return Page();
        }

        var cuenta = await CuentaAsync();
        var perfil = await db.Perfiles.SingleAsync();
        var cfg = await db.Configuraciones.SingleAsync();

        var nombre = string.IsNullOrWhiteSpace(Nombre) ? null : Nombre.Trim();
        var cambiaNombre = cuenta.Nombre != nombre;
        cuenta.Nombre = nombre;

        perfil.AlturaCm = AlturaCm is > 0 ? AlturaCm : null;
        perfil.FechaNacimiento = FechaNacimiento;
        perfil.Sexo = sexo;
        perfil.FactorActividad = factor.Value;

        cfg.MetodoBmr = Metabolismo.Elegir(BmrMedido, GrasaPct);
        cfg.BmrMedido = BmrMedido is > 0 ? BmrMedido : null;

        await db.SaveChangesAsync();
        if (cambiaNombre)
            await RenovarCookieAsync(cuenta);

        string? nota = "Arranque";
        if (MedicionId > 0)
        {
            nota = await db.Mediciones.AsNoTracking()
                .Where(m => m.Id == MedicionId)
                .Select(m => m.Nota)
                .FirstOrDefaultAsync();
        }

        var fecha = FechaMedicion == default ? DateOnly.FromDateTime(DateTime.Today) : FechaMedicion;
        var resultado = await mediciones.GuardarAsync(new Medicion
        {
            Id = MedicionId,
            Fecha = fecha,
            PesoKg = PesoKg.Value,
            GrasaPct = GrasaPct is > 0 ? GrasaPct : null,
            Nota = nota
        });

        if (!resultado.Ok)
        {
            Error = resultado.Error;
            await CargarOpcionesAsync();
            return Page();
        }

        var guardada = await db.Mediciones
            .OrderByDescending(m => m.Id)
            .FirstAsync(m => m.Fecha == fecha);
        await mediciones.MarcarReferenciaAsync(guardada.Id);

        return RedirectToPage(new { paso = PasoObjetivo });
    }

    public Task<IActionResult> OnPostOmitirVosAsync()
        => IrA(PasoObjetivo);

    public async Task<IActionResult> OnPostGuardarObjetivoAsync()
    {
        NormalizarPaso();
        Paso = PasoObjetivo;
        if (PlazoSemanas < 1)
        {
            Error = "El plazo tiene que ser de al menos una semana.";
            await CargarVistaObjetivoAsync();
            return Page();
        }

        if (GrasaObjetivoPct is < 0 or >= 100)
        {
            Error = "El porcentaje de grasa objetivo tiene que estar entre 0 y 100.";
            await CargarVistaObjetivoAsync();
            return Page();
        }

        var perfil = await db.Perfiles.SingleAsync();
        perfil.GrasaObjetivoPct = GrasaObjetivoPct;
        perfil.PlazoSemanas = PlazoSemanas;
        perfil.TipoObjetivo = TipoObjetivo;
        perfil.FechaInicioObjetivo ??= DateOnly.FromDateTime(DateTime.Today);
        await db.SaveChangesAsync();
        return RedirectToPage(new { paso = PasoPlantilla });
    }

    public Task<IActionResult> OnPostOmitirObjetivoAsync()
        => IrA(PasoPlantilla);

    public async Task<IActionResult> OnPostOmitirAsync()
    {
        await MarcarBienvenidaVistaAsync();
        return Siguiente();
    }

    public async Task<IActionResult> OnPostImportarAsync()
    {
        if (Modo != "plantilla")
            return await OnPostOmitirAsync();

        var resultado = await plantillas.ImportarAsync(PlantillaExcel.Id, Partes);
        await MarcarBienvenidaVistaAsync();

        TempData["Mensaje"] = resultado.Creados == 0 && resultado.OmitidosPorYaExistir == 0
            ? "No se importó nada: no había partes marcadas."
            : resultado.OmitidosPorYaExistir == 0
                ? $"Se copiaron {resultado.Creados} ítems a tu cuenta. Son tuyos: podés editarlos o borrarlos."
                : $"Se copiaron {resultado.Creados} ítems. {resultado.OmitidosPorYaExistir} ya estaban y se dejaron como estaban.";

        return Siguiente();
    }

    private async Task<IActionResult> IrA(string paso)
    {
        await CargarOpcionesAsync();
        return RedirectToPage(new { paso, Repetir = Repetir ? true : (bool?)null });
    }

    private IActionResult Siguiente()
        => Repetir
            ? RedirectToPage("/Ajustes", new { tab = "cuenta" })
            : RedirectToPage("/Index");

    private async Task MarcarBienvenidaVistaAsync()
    {
        var cuenta = await CuentaAsync();
        cuenta.BienvenidaPendiente = false;
        await db.SaveChangesAsync();
    }

    private void NormalizarPaso()
    {
        if (Repetir)
        {
            Paso = PasoPlantilla;
            return;
        }

        ViewData["Alta"] = true;
        if (Paso is not PasoVos and not PasoObjetivo and not PasoPlantilla)
            Paso = PasoVos;
    }

    private async Task CargarAsync()
    {
        await CargarOpcionesAsync();
        if (Paso is PasoVos or PasoObjetivo)
            await PrefillPerfilAsync();
        if (Paso == PasoObjetivo)
            await CargarVistaObjetivoAsync();
    }

    private async Task CargarOpcionesAsync()
    {
        Opciones = plantillas.Partes();
        if (Partes.Count == 0)
            Partes = Opciones.Where(o => o.MarcadaPorDefecto).Select(o => o.Parte).ToList();
    }

    private async Task PrefillPerfilAsync()
    {
        var cuenta = await CuentaAsync();
        var perfil = await db.Perfiles.SingleAsync();
        var cfg = await db.Configuraciones.SingleAsync();

        Nombre ??= cuenta.Nombre;
        AlturaCm ??= perfil.AlturaCm;
        FechaNacimiento ??= perfil.FechaNacimiento;
        if (string.IsNullOrEmpty(SexoElegido))
        {
            SexoElegido = perfil.Sexo switch
            {
                Sexo.Masculino => "m",
                Sexo.Femenino => "f",
                _ => ""
            };
        }

        FactorElegido = FactorComoOpcion(perfil.FactorActividad);
        FactorLibre = perfil.FactorActividad;
        BmrMedido ??= cfg.BmrMedido;
        GrasaObjetivoPct ??= perfil.GrasaObjetivoPct ?? 15;
        if (perfil.PlazoSemanas > 0) PlazoSemanas = perfil.PlazoSemanas;
        TipoObjetivo = perfil.TipoObjetivo;

        var referencia = await mediciones.ReferenciaAsync();
        if (referencia is { } r)
        {
            MedicionId = r.Medicion.Id;
            FechaMedicion = r.Medicion.Fecha;
            PesoKg ??= r.Medicion.PesoKg;
            GrasaPct ??= r.Medicion.GrasaPct;
        }
    }

    private async Task CargarVistaObjetivoAsync()
    {
        await CargarOpcionesAsync();
        var perfil = await db.Perfiles.SingleAsync();
        var cfg = await db.Configuraciones.SingleAsync();
        var referencia = await mediciones.ReferenciaAsync();
        if (referencia is null) return;

        var med = referencia.Medicion;
        var edad = perfil.FechaNacimiento is { } nac
            ? Metabolismo.Edad(nac, med.Fecha)
            : null;
        var bmr = Metabolismo.Calcular(
            cfg.MetodoBmr, cfg.BmrMedido, med.PesoKg, med.GrasaPct,
            perfil.AlturaCm, edad, perfil.Sexo);
        var gasto = Metabolismo.GastoSinDeporte(bmr, perfil.FactorActividad);
        var grasaObj = GrasaObjetivoPct ?? perfil.GrasaObjetivoPct;
        var plazo = PlazoSemanas > 0 ? PlazoSemanas : perfil.PlazoSemanas;
        Plan = null;
        if (gasto is not null)
        {
            Plan = PlanObjetivo.De(
                med.PesoKg, med.GrasaPct, grasaObj,
                plazo, cfg.KcalPorKgGrasa, gasto.Value, cfg,
                perfil.FechaInicioObjetivo);
        }

        Deficit = DeficitObjetivo.De(Plan, gasto, med.GrasaPct, TipoObjetivo, null);
        Alcance = AlcanceObjetivo.De(Plan, Deficit, med.GrasaPct, grasaObj ?? 0, TipoObjetivo);
    }

    public async Task<IActionResult> OnPostRecalcularObjetivoAsync()
    {
        NormalizarPaso();
        Paso = PasoObjetivo;
        if (PlazoSemanas < 1) PlazoSemanas = 1;
        await CargarVistaObjetivoAsync();
        return Partial("_BienvenidaObjetivoCalc", this);
    }

    private double? FactorDesdeFormulario()
    {
        if (FactorElegido == "otro") return FactorLibre;
        return double.TryParse(FactorElegido, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? v : FactorLibre;
    }

    private static string FactorComoOpcion(double factor)
    {
        foreach (var sugerido in ValoresDeFabrica.FactoresActividadSugeridos)
        {
            if (Math.Abs(factor - sugerido) < 0.0001)
                return sugerido.ToString("0.##", CultureInfo.InvariantCulture);
        }
        return "otro";
    }

    private async Task RenovarCookieAsync(Dominio.Usuario cuenta)
    {
        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, cuenta.Id.ToString()),
            new Claim(ClaimTypes.Name, cuenta.Nombre ?? cuenta.Email)
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = true });
    }

    private Task<Dominio.Usuario> CuentaAsync()
    {
        var id = usuarioActual.UsuarioId
            ?? throw new InvalidOperationException("No hay cuenta en sesión.");
        return db.Usuarios.FirstAsync(u => u.Id == id);
    }
}
