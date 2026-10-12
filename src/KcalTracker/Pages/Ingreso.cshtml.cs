using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KcalTracker.Pages;

[AllowAnonymous]
public class IngresoModel(ServicioCuentas cuentas) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Falta el correo."), EmailAddress(ErrorMessage = "El correo no parece válido.")]
    public string Email { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Falta la contraseña.")]
    public string Clave { get; set; } = "";

    public IActionResult OnGet()
        => User.Identity?.IsAuthenticated == true ? RedirectToPage("/Index") : Page();

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        if (!ModelState.IsValid) return Page();

        var usuario = await cuentas.VerificarAsync(Email, Clave);

        if (usuario is null)
        {
            // Sin distinguir si falló el correo o la contraseña, para no confirmar qué
            // cuentas existen.
            ModelState.AddModelError(string.Empty, "El correo o la contraseña no coinciden.");
            return Page();
        }

        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre ?? usuario.Email)
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = true });

        return LocalRedirect(returnUrl ?? "/");
    }
}
