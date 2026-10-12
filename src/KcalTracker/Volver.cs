using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KcalTracker;

public static class Volver
{
    public static IActionResult A(PageModel pagina, string? vuelta, string? fallback = null)
    {
        if (!string.IsNullOrWhiteSpace(vuelta) && pagina.Url.IsLocalUrl(vuelta))
            return pagina.LocalRedirect(vuelta);
        return fallback is null ? pagina.RedirectToPage() : pagina.RedirectToPage(fallback);
    }
}
