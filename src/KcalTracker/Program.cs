using System.Globalization;
using KcalTracker;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var cultura = new CultureInfo("es-UY");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    o.DefaultRequestCulture = new RequestCulture(cultura);
    o.SupportedCultures = [cultura];
    o.SupportedUICultures = [cultura];
});

builder.Services.AddScoped<FiltroBienvenidaPendiente>();
builder.Services.AddRazorPages(opciones =>
{
    // Todo pide sesión salvo lo que se marque con [AllowAnonymous]. Es al revés de lo
    // habitual a propósito: si alguien agrega una pantalla y olvida protegerla, queda
    // cerrada en lugar de abierta.
    opciones.Conventions.AuthorizeFolder("/");
}).AddMvcOptions(o =>
{
    o.Filters.AddService<FiltroBienvenidaPendiente>();
    o.ModelBinderProviders.Insert(0, new KcalTracker.DateOnlyIsoBinderProvider());
    o.ModelBinderProviders.Insert(0, new KcalTracker.DoubleIsoBinderProvider());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualDeCookie>();
builder.Services.AddSingleton<CatalogoCompartido>();
builder.Services.AddScoped<SemillaCatalogoCompartido>();
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<ServicioCuentas>();
builder.Services.AddScoped<ServicioCatalogos>();
builder.Services.AddScoped<ServicioPlantillas>();
builder.Services.AddScoped<ServicioMediciones>();
builder.Services.AddScoped<ServicioDia>();
builder.Services.AddScoped<ServicioAlimentos>();
builder.Services.AddScoped<ServicioImportacionAlimentos>();
builder.Services.AddScoped<ServicioActividades>();
builder.Services.AddScoped<ServicioGym>();
builder.Services.AddScoped<ServicioRutinas>();
builder.Services.AddScoped<ServicioCardio>();
builder.Services.AddScoped<ServicioPeriodo>();
builder.Services.AddScoped<ServicioExportacionPeriodo>();
builder.Services.AddScoped<ServicioContexto>();
builder.Services.AddScoped<ServicioRespaldo>();
builder.Services.Configure<NutricionOpciones>(builder.Configuration.GetSection(NutricionOpciones.Seccion));
builder.Services.AddScoped<OpenFoodFactsProveedor>();
builder.Services.AddScoped<UsdaProveedor>();
builder.Services.AddScoped<ModeloLenguajeProveedor>();
builder.Services.AddScoped<ServicioNutricion>();
builder.Services.AddHttpClient("off", c =>
{
    c.BaseAddress = new Uri("https://world.openfoodfacts.org/");
    c.Timeout = TimeSpan.FromSeconds(8);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("kcal-tracker/1.0");
});
builder.Services.AddHttpClient("usda", c =>
{
    c.BaseAddress = new Uri("https://api.nal.usda.gov/fdc/v1/");
    c.Timeout = TimeSpan.FromSeconds(8);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("kcal-tracker/1.0");
});
builder.Services.AddHttpClient("openai", c =>
{
    c.Timeout = TimeSpan.FromSeconds(25);
});

var cadena = builder.Configuration.GetConnectionString("Kcal")
    ?? RutaSqlite.Cadena(builder.Environment.ContentRootPath);

builder.Services.AddDbContext<KcalDbContext>(opciones => opciones.UseSqlite(cadena));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Ingreso";
        opciones.LogoutPath = "/Salir";
        opciones.AccessDeniedPath = "/Ingreso";
        opciones.ExpireTimeSpan = TimeSpan.FromDays(30);
        opciones.SlidingExpiration = true;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

using (var alcance = app.Services.CreateScope())
{
    var db = alcance.ServiceProvider.GetRequiredService<KcalDbContext>();
    await db.Database.MigrateAsync();
    await ServicioCuentas.CompletarFilasPorCuentaAsync(db);
    await alcance.ServiceProvider.GetRequiredService<SemillaCatalogoCompartido>().AsegurarAsync();
}

if (app.Environment.IsDevelopment())
{
    using var semilla = app.Services.CreateScope();
    await SemillaDesarrollo.AsegurarAsync(semilla.ServiceProvider);
}

app.Run();

public partial class Program;
