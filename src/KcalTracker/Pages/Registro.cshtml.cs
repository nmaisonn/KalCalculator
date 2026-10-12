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
public class RegistroModel(ServicioCuentas cuentas) : PageModel
{
    [BindProperty]
    public string? Nombre { get; set; }

    [BindProperty, Required(ErrorMessage = "Falta el correo."), EmailAddress(ErrorMessage = "El correo no parece válido.")]
    public string Email { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Falta la contraseña.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "La contraseña necesita al menos 8 caracteres.")]
    public string Clave { get; set; } = "";

    public IActionResult OnGet()
        => User.Identity?.IsAuthenticated == true ? RedirectToPage("/Index") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var (resultado, usuario) = await cuentas.RegistrarAsync(Email, Clave, Nombre);

        if (resultado == ResultadoRegistro.EmailYaUsado)
        {
            ModelState.AddModelError(nameof(Email), "Ya hay una cuenta con ese correo.");
            return Page();
        }

        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, usuario!.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre ?? usuario.Email)
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToPage("/Bienvenida");
    }
}
