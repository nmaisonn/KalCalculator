using KcalTracker.Datos;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker;

/// <summary>
/// Una cuenta nueva no entra a Hoy hasta terminar (o omitir) la bienvenida:
/// datos de arranque, objetivo y catálogos.
/// </summary>
public sealed class FiltroBienvenidaPendiente : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var ruta = context.ActionDescriptor.ViewEnginePath;
        if (ruta is "/Bienvenida" or "/Salir" or "/Ingreso" or "/Registro" or "/Error")
        {
            await next();
            return;
        }

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var actual = context.HttpContext.RequestServices.GetRequiredService<IUsuarioActual>();
        if (actual.UsuarioId is not { } id)
        {
            await next();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<KcalDbContext>();
        var pendiente = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => u.BienvenidaPendiente)
            .FirstOrDefaultAsync();

        if (pendiente)
        {
            context.Result = new RedirectToPageResult("/Bienvenida");
            return;
        }

        await next();
    }
}
