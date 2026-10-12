using System.Globalization;
using System.Security.Claims;
using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class AjustesModel(
    KcalDbContext db,
    IUsuarioActual usuarioActual,
    ServicioCatalogos catalogos,
    ServicioCuentas cuentas,
    ServicioRespaldo respaldo) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "perfil";

    public Usuario Cuenta { get; private set; } = null!;
    public Perfil Perfil { get; private set; } = null!;
    public Configuracion Cfg { get; private set; } = null!;
    public Medicion? Medicion { get; private set; }

    public ResultadoBmr? BmrElegido { get; private set; }
    public ResultadoBmr? BmrKatch { get; private set; }
    public ResultadoBmr? BmrMifflin { get; private set; }
    public ResultadoPlan? Plan { get; private set; }
    public ResultadoDeficit? Deficit { get; private set; }
    public ResultadoProteina? Proteina { get; private set; }
    public RitmoObjetivo? Ritmo { get; private set; }
    public ResultadoAlcance? Alcance { get; private set; }
    public ConstantesSugeridas? Sugeridas { get; private set; }
    public string? AvisoCalculo { get; private set; }

    public IReadOnlyList<ItemCatalogo> TiposComida { get; private set; } = [];
    public IReadOnlyList<ItemCatalogo> TiposActividad { get; private set; } = [];
    public IReadOnlyList<ItemCatalogo> Categorias { get; private set; } = [];
    public IReadOnlyList<ItemCatalogo> Grupos { get; private set; } = [];

    public int Comidas { get; private set; }
    public int SesionesGym { get; private set; }
    public int SesionesCardio { get; private set; }
    public int Mediciones { get; private set; }

    [BindProperty] public string? Nombre { get; set; }
    [BindProperty] public double? AlturaCm { get; set; }
    [BindProperty] public DateOnly? FechaNacimiento { get; set; }
    [BindProperty] public string SexoElegido { get; set; } = "";
    [BindProperty] public string FactorElegido { get; set; } = "1.35";
    [BindProperty] public double? FactorLibre { get; set; }
    [BindProperty] public FuenteAlimentos FuenteAlimentos { get; set; } = FuenteAlimentos.Ambos;

    [BindProperty] public double? GrasaObjetivoPct { get; set; }
    [BindProperty] public int PlazoSemanas { get; set; } = ValoresDeFabrica.PlazoSemanas;
    [BindProperty] public DateOnly? FechaInicioObjetivo { get; set; }
    [BindProperty] public double? ProteinaObjetivoG { get; set; }
    [BindProperty] public bool UsarSugerida { get; set; }
    [BindProperty] public TipoObjetivo TipoObjetivo { get; set; } = TipoObjetivo.Corte;
    [BindProperty] public double? DeficitDiarioKcal { get; set; }

    [BindProperty] public MetodoBmr MetodoBmr { get; set; }
    [BindProperty] public double? BmrMedido { get; set; }
    [BindProperty] public double KcalPorKgGrasa { get; set; }
    [BindProperty] public double KcalPorKgMagro { get; set; }
    [BindProperty] public double PctGastoMaxPct { get; set; }
    [BindProperty] public double PctPesoSemanalPct { get; set; }
    [BindProperty] public double CobMinContrastePct { get; set; }
    [BindProperty] public double CobMinCalibradoPct { get; set; }

    [BindProperty] public string? ClaveActual { get; set; }
    [BindProperty] public string? ClaveNueva { get; set; }
    [BindProperty] public string? ClaveNueva2 { get; set; }
    [BindProperty] public string? CorreoConfirmacion { get; set; }

    public string? Mensaje { get; private set; }
    public string? Error { get; private set; }

    public VistaCalculoObjetivo VistaCalculo => new()
    {
        Deficit = Deficit,
        Plan = Plan,
        Ritmo = Ritmo,
        TipoObjetivo = TipoObjetivo,
        GrasaObjetivoPct = GrasaObjetivoPct ?? 0,
        Alcance = Alcance,
        Handler = "RecalcularObjetivo"
    };

    public async Task<IActionResult> OnGetExportarAsync()
    {
        var bytes = await respaldo.ExportarAsync();
        return File(bytes, "application/json", $"kcal-tracker-{DateTime.Today:yyyy-MM-dd}.json");
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRecalcularObjetivoAsync()
    {
        Tab = "objetivo";
        await CargarEntidadesAsync();
        RecalcularVista();
        return Partial("_CalculoObjetivo", VistaCalculo);
    }

    public async Task<IActionResult> OnPostGuardarPerfilAsync()
    {
        Tab = "perfil";
        var factor = FactorDesdeFormulario();
        if (factor is null or <= 0)
        {
            await CargarParaErrorAsync();
            Error = "El factor de actividad tiene que ser un número mayor a cero.";
            return Page();
        }

        await CargarEntidadesAsync();
        Perfil.AlturaCm = AlturaCm;
        Perfil.FechaNacimiento = FechaNacimiento;
        Perfil.Sexo = SexoElegido switch
        {
            "m" => Sexo.Masculino,
            "f" => Sexo.Femenino,
            _ => null
        };
        Perfil.FactorActividad = factor.Value;
        Perfil.FuenteAlimentos = FuenteAlimentos;

        var nombre = string.IsNullOrWhiteSpace(Nombre) ? null : Nombre.Trim();
        var cambiaNombre = Cuenta.Nombre != nombre;
        Cuenta.Nombre = nombre;
        await db.SaveChangesAsync();
        if (cambiaNombre)
            await RenovarCookieAsync();

        TempData["Mensaje"] = "Perfil guardado.";
        return RedirectToPage(new { tab = "perfil" });
    }

    public async Task<IActionResult> OnPostGuardarObjetivoAsync()
    {
        Tab = "objetivo";
        if (PlazoSemanas < 1)
        {
            await CargarParaErrorAsync();
            Error = "El plazo tiene que ser de al menos una semana.";
            return Page();
        }

        if (GrasaObjetivoPct is < 0 or >= 100)
        {
            await CargarParaErrorAsync();
            Error = "El porcentaje de grasa objetivo tiene que estar entre 0 y 100.";
            return Page();
        }

        await CargarEntidadesAsync();
        if (UsarSugerida)
        {
            var calc = ProteinaNecesaria.Calcular(Medicion?.PesoKg, Medicion?.GrasaPct, TipoObjetivo);
            if (calc is { } p) ProteinaObjetivoG = p.SugeridaG;
        }

        Perfil.GrasaObjetivoPct = GrasaObjetivoPct;
        Perfil.PlazoSemanas = PlazoSemanas;
        Perfil.FechaInicioObjetivo = FechaInicioObjetivo is { } fi && fi != default ? fi : null;
        Perfil.ProteinaObjetivoG = ProteinaObjetivoG is > 0 ? ProteinaObjetivoG : null;
        Perfil.TipoObjetivo = TipoObjetivo;
        Perfil.DeficitDiarioKcal = DeficitDiarioKcal is > 0 ? DeficitDiarioKcal : null;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Objetivo guardado.";
        return RedirectToPage(new { tab = "objetivo" });
    }

    public async Task<IActionResult> OnPostGuardarCalculoAsync()
    {
        Tab = "calculo";
        if (MetodoBmr == MetodoBmr.Medido && BmrMedido is null)
        {
            await CargarParaErrorAsync();
            Error = "Si elegís un valor medido, hay que cargarlo.";
            return Page();
        }

        await CargarEntidadesAsync();
        if (KcalPorKgGrasa is < 1000 or > 20000
            || KcalPorKgMagro is < 200 or > 8000
            || PctGastoMaxPct is < 1 or > 80
            || PctPesoSemanalPct is < 0.05 or > 5
            || CobMinContrastePct is < 1 or > 100
            || CobMinCalibradoPct is < 1 or > 100)
        {
            await CargarParaErrorAsync();
            Error = "Algún número del cálculo quedó vacío o fuera de rango. No se guardó: revisá los campos y volvé a intentar.";
            return Page();
        }

        Cfg.MetodoBmr = MetodoBmr;
        if (MetodoBmr == MetodoBmr.Medido) Cfg.BmrMedido = BmrMedido;
        Cfg.KcalPorKgGrasa = KcalPorKgGrasa;
        Cfg.KcalPorKgMagro = KcalPorKgMagro;
        Cfg.PctGastoMax = PctGastoMaxPct / 100;
        Cfg.PctPesoSemanal = PctPesoSemanalPct / 100;
        Cfg.CobMinContraste = CobMinContrastePct / 100;
        Cfg.CobMinCalibrado = CobMinCalibradoPct / 100;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Cálculo guardado.";
        return RedirectToPage(new { tab = "calculo" });
    }

    public async Task<IActionResult> OnPostRestaurarCalculoAsync()
    {
        await CargarEntidadesAsync();
        var s = ConstantesNutricion.Calcular(Medicion?.GrasaPct);
        Cfg.KcalPorKgGrasa = s.KcalPorKgGrasa;
        Cfg.KcalPorKgMagro = s.KcalPorKgMagro;
        Cfg.PctGastoMax = s.PctGastoMax;
        Cfg.PctPesoSemanal = s.PctPesoSemanal;
        Cfg.CobMinContraste = s.CobMinContraste;
        Cfg.CobMinCalibrado = s.CobMinCalibrado;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Aplicamos los valores calculados. El método de GER no se tocó.";
        return RedirectToPage(new { tab = "calculo" });
    }

    public Task<IActionResult> OnPostAgregarTipoAsync(Catalogo catalogo, string nombre)
        => EjecutarCatalogo(catalogo, () => catalogos.AgregarAsync(catalogo, nombre ?? ""));

    public Task<IActionResult> OnPostRenombrarTipoAsync(Catalogo catalogo, int id, string nombre)
        => EjecutarCatalogo(catalogo, () => catalogos.RenombrarAsync(catalogo, id, nombre ?? ""));

    public Task<IActionResult> OnPostActivarTipoAsync(Catalogo catalogo, int id)
        => EjecutarCatalogo(catalogo, () => catalogos.CambiarActivoAsync(catalogo, id, true));

    public Task<IActionResult> OnPostDesactivarTipoAsync(Catalogo catalogo, int id)
        => EjecutarCatalogo(catalogo, () => catalogos.CambiarActivoAsync(catalogo, id, false));

    public Task<IActionResult> OnPostSubirTipoAsync(Catalogo catalogo, int id)
        => EjecutarCatalogo(catalogo, () => catalogos.MoverAsync(catalogo, id, -1));

    public Task<IActionResult> OnPostBajarTipoAsync(Catalogo catalogo, int id)
        => EjecutarCatalogo(catalogo, () => catalogos.MoverAsync(catalogo, id, 1));

    public Task<IActionResult> OnPostEliminarTipoAsync(Catalogo catalogo, int id)
        => EjecutarCatalogo(catalogo, () => catalogos.EliminarAsync(catalogo, id));

    public async Task<IActionResult> OnPostCambiarClaveAsync()
    {
        Tab = "cuenta";
        await CargarParaErrorAsync();
        ClaveActual ??= "";
        ClaveNueva ??= "";
        ClaveNueva2 ??= "";

        if (ClaveNueva.Length < 8)
        {
            Error = "La contraseña nueva necesita al menos 8 caracteres.";
            return Page();
        }

        if (ClaveNueva != ClaveNueva2)
        {
            Error = "La contraseña nueva y la repetición no coinciden.";
            return Page();
        }

        var resultado = await cuentas.CambiarClaveAsync(Cuenta.Id, ClaveActual, ClaveNueva);
        if (resultado == ServicioCuentas.ResultadoCambioClave.ClaveActualIncorrecta)
        {
            Error = "La contraseña actual no coincide.";
            return Page();
        }

        TempData["Mensaje"] = "Contraseña cambiada.";
        return RedirectToPage(new { tab = "cuenta" });
    }

    public async Task<IActionResult> OnPostEliminarCuentaAsync()
    {
        await CargarEntidadesAsync();
        Tab = "cuenta";

        if (!string.Equals((CorreoConfirmacion ?? "").Trim(), Cuenta.Email, StringComparison.OrdinalIgnoreCase))
        {
            await CargarCatalogosYResumenAsync();
            RecalcularVista();
            Error = "Escribí tu correo para confirmar. Sin eso no se borra nada.";
            return Page();
        }

        await cuentas.EliminarCuentaAsync(Cuenta.Id);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Registro");
    }

    private async Task<IActionResult> EjecutarCatalogo(Catalogo catalogo, Func<Task<(bool Ok, string? Error)>> accion)
    {
        Tab = "tipos";
        var (ok, error) = await accion();
        if (EsHtmx)
        {
            var modelo = new ListaCatalogoModelo
            {
                Catalogo = catalogo,
                Titulo = TituloDe(catalogo),
                Items = await catalogos.ListarAsync(catalogo),
                Error = ok ? null : error
            };
            return Partial("Shared/_ListaCatalogo", modelo);
        }

        if (!ok)
        {
            await CargarAsync();
            Error = error;
            return Page();
        }
        TempData["Mensaje"] = "Tipos actualizados.";
        return RedirectToPage(new { tab = "tipos" });
    }

    private async Task CargarAsync()
    {
        await CargarEntidadesAsync();
        if (TempData["Mensaje"] is string mensajeTemp)
            Mensaje = mensajeTemp;
        RellenarFormularioDesdeEntidades();
        RecalcularVista();
        await CargarCatalogosYResumenAsync();
    }

    /// <summary>
    /// Carga entidades para un POST. No pisa los BindProperty: si se rellena el
    /// formulario acá, Guardar* vuelve a escribir los valores viejos de la base.
    /// </summary>
    private async Task CargarEntidadesAsync()
    {
        var id = usuarioActual.UsuarioId
            ?? throw new InvalidOperationException("No hay cuenta en sesión.");

        Cuenta = await db.Usuarios.FirstAsync(u => u.Id == id);
        Perfil = await db.Perfiles.SingleAsync();
        Cfg = await db.Configuraciones.SingleAsync();
        Medicion = await ServicioMediciones.ObtenerReferenciaAsync(db);
    }

    private async Task CargarParaErrorAsync()
    {
        await CargarEntidadesAsync();
        RecalcularVista();
        await CargarCatalogosYResumenAsync();
    }

    private async Task CargarCatalogosYResumenAsync()
    {
        TiposComida = await catalogos.ListarAsync(Catalogo.Comida);
        TiposActividad = await catalogos.ListarAsync(Catalogo.Actividad);
        Categorias = await catalogos.ListarAsync(Catalogo.Categoria);
        Grupos = await catalogos.ListarAsync(Catalogo.Musculo);

        Comidas = await db.Comidas.CountAsync();
        SesionesGym = await db.SesionesGym.CountAsync();
        SesionesCardio = await db.SesionesCardio.CountAsync();
        Mediciones = await db.Mediciones.CountAsync();
    }

    private void RellenarFormularioDesdeEntidades()
    {
        Nombre = Cuenta.Nombre;
        AlturaCm = Perfil.AlturaCm;
        FechaNacimiento = Perfil.FechaNacimiento;
        SexoElegido = Perfil.Sexo switch
        {
            Sexo.Masculino => "m",
            Sexo.Femenino => "f",
            _ => ""
        };
        FactorElegido = FactorComoOpcion(Perfil.FactorActividad);
        FactorLibre = Perfil.FactorActividad;
        FuenteAlimentos = Perfil.FuenteAlimentos;

        GrasaObjetivoPct = Perfil.GrasaObjetivoPct;
        PlazoSemanas = Perfil.PlazoSemanas;
        FechaInicioObjetivo = Perfil.FechaInicioObjetivo;
        ProteinaObjetivoG = Perfil.ProteinaObjetivoG;
        TipoObjetivo = Perfil.TipoObjetivo;
        DeficitDiarioKcal = Perfil.DeficitDiarioKcal;

        MetodoBmr = Cfg.MetodoBmr;
        BmrMedido = Cfg.BmrMedido;
        KcalPorKgGrasa = Cfg.KcalPorKgGrasa;
        KcalPorKgMagro = Cfg.KcalPorKgMagro;
        PctGastoMaxPct = Cfg.PctGastoMax * 100;
        PctPesoSemanalPct = Cfg.PctPesoSemanal * 100;
        CobMinContrastePct = Cfg.CobMinContraste * 100;
        CobMinCalibradoPct = Cfg.CobMinCalibrado * 100;
    }

    private void RecalcularVista()
    {
        Proteina = ProteinaNecesaria.Calcular(Medicion?.PesoKg, Medicion?.GrasaPct, TipoObjetivo);
        Sugeridas = ConstantesNutricion.Calcular(Medicion?.GrasaPct);
        if (Medicion is null) return;

        var edad = Perfil.FechaNacimiento is { } nac
            ? Metabolismo.Edad(nac, Medicion.Fecha)
            : null;

        BmrKatch = Medicion.GrasaPct is null
            ? new ResultadoBmr(null, MetodoBmr.KatchMcArdle, null, false, "Esta medición no trae porcentaje de grasa.")
            : new ResultadoBmr(
                Metabolismo.KatchMcArdle(Medicion.PesoKg, Medicion.GrasaPct.Value),
                MetodoBmr.KatchMcArdle, MetodoBmr.KatchMcArdle, false, null);

        BmrMifflin = Metabolismo.Calcular(
            MetodoBmr.Mifflin, null, Medicion.PesoKg, Medicion.GrasaPct,
            Perfil.AlturaCm, edad, Perfil.Sexo);

        BmrElegido = Metabolismo.Calcular(
            Cfg.MetodoBmr, Cfg.BmrMedido, Medicion.PesoKg, Medicion.GrasaPct,
            Perfil.AlturaCm, edad, Perfil.Sexo);

        var gasto = Metabolismo.GastoSinDeporte(BmrElegido, Perfil.FactorActividad);
        var grasaObj = GrasaObjetivoPct ?? Perfil.GrasaObjetivoPct;
        var plazo = PlazoSemanas > 0 ? PlazoSemanas : Perfil.PlazoSemanas;
        var kcalGrasa = KcalPorKgGrasa > 0 ? KcalPorKgGrasa : Cfg.KcalPorKgGrasa;
        var pctGasto = PctGastoMaxPct > 0 ? PctGastoMaxPct / 100 : Cfg.PctGastoMax;
        var pctPeso = PctPesoSemanalPct > 0 ? PctPesoSemanalPct / 100 : Cfg.PctPesoSemanal;
        if (gasto is not null)
        {
            var techos = ConstantesNutricion.TechosDePlan(Medicion.GrasaPct, pctGasto, pctPeso);
            Plan = PlanObjetivo.De(
                Medicion.PesoKg, Medicion.GrasaPct, grasaObj,
                plazo, kcalGrasa, gasto.Value, techos.PctGastoMax, techos.PctPesoSemanal,
                FechaInicioObjetivo ?? Perfil.FechaInicioObjetivo);
        }

        Deficit = DeficitObjetivo.De(Plan, gasto, Medicion.GrasaPct, TipoObjetivo, DeficitDiarioKcal);
        Ritmo = RitmoObjetivo.De(
            Plan?.Plazo ?? PlazoCorrido.De(plazo, FechaInicioObjetivo ?? Perfil.FechaInicioObjetivo),
            Plan, null, Deficit);
        Alcance = AlcanceObjetivo.De(Plan, Deficit, Medicion.GrasaPct, grasaObj ?? 0, TipoObjetivo);

        AvisoCalculo = AvisoDeRango();
    }

    private string? AvisoDeRango()
    {
        if (Cfg.KcalPorKgGrasa is < 4000 or > 12000)
            return $"kcal por kilo de grasa está fuera del rango habitual (4000 a 12000). El cálculo da {Sugeridas?.KcalPorKgGrasa.ToString("0") ?? ValoresDeFabrica.KcalPorKgGrasa.ToString("0")}.";
        if (Cfg.KcalPorKgMagro is < 500 or > 4000)
            return $"kcal por kilo de magro está fuera del rango habitual. El cálculo da {Sugeridas?.KcalPorKgMagro.ToString("0") ?? ValoresDeFabrica.KcalPorKgMagro.ToString("0")}.";
        if (Cfg.PctGastoMax is < 0.05 or > 0.5)
            return "El techo como % del gasto diario está fuera del rango habitual (5 % a 50 %).";
        return null;
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

    private async Task RenovarCookieAsync()
    {
        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Cuenta.Id.ToString()),
            new Claim(ClaimTypes.Name, Cuenta.Nombre ?? Cuenta.Email)
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = true });
    }

    private bool EsHtmx => Request.Headers.ContainsKey("HX-Request");

    public static string TituloDe(Catalogo catalogo) => catalogo switch
    {
        Catalogo.Comida => "Tipos de comida",
        Catalogo.Actividad => "Tipos de actividad",
        Catalogo.Categoria => "Tipos de alimento",
        Catalogo.Musculo => "Grupos musculares",
        _ => catalogo.ToString()
    };
}

public sealed class ListaCatalogoModelo
{
    public Catalogo Catalogo { get; init; }
    public string Titulo { get; init; } = "";
    public IReadOnlyList<ItemCatalogo> Items { get; init; } = [];
    public string? Error { get; init; }
}
