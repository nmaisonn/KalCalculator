using System.Security.Claims;

namespace KcalTracker.Datos;

/// <summary>Lee la cuenta de la cookie de autenticación de la petición en curso.</summary>
public class UsuarioActualDeCookie(IHttpContextAccessor acceso) : IUsuarioActual
{
    public int? UsuarioId
    {
        get
        {
            var valor = acceso.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(valor, out var id) ? id : null;
        }
    }
}
