using KcalTracker;
using KcalTracker.Calculo;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using KcalTracker.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Pages;

public class CardioModel(KcalDbContext db, ServicioCardio cardio) : PageModel
{
    public IReadOnlyList<SesionCardio> Sesiones { get; private set; } = [];
    public IReadOnlyList<FilaCardio> Lista { get; private set; } = [];
    public string? Mensaje { get; private set; }
    public string? Error { get; private set; }

    [BindProperty] public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty] public int? ProtocoloId { get; set; }
    [BindProperty] public double Minutos { get; set; } = 30;
    [BindProperty] public double? Velocidad { get; set; }
    [BindProperty] public double? Inclinacion { get; set; }
    [BindProperty] public double? Kcal { get; set; }
    [BindProperty] public double? KcalMin { get; set; }
    [BindProperty] public double? KcalMax { get; set; }
    [BindProperty] public string? Nota { get; set; }

    [BindProperty] public int ProtoId { get; set; }
    [BindProperty] public string ProtoNombre { get; set; } = "";
    [BindProperty] public string? ProtoDescripcion { get; set; }
    [BindProperty] public double ProtoMinutos { get; set; } = 30;
    [BindProperty] public double? ProtoKcal { get; set; }
    public bool Editando { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Vuelta { get; set; }

    public async Task OnGetAsync(
        int? id,
        bool nuevo = false,
        int? protocoloId = null,
        bool nuevoProtocolo = false,
        int? editarProtocolo = null)
    {
        await CargarAsync();
        if (protocoloId is { } pid)
        {
            var p = Lista.FirstOrDefault(x => x.Protocolo.Id == pid)?.Protocolo;
            if (p is not null)
            {
                ProtocoloId = p.Id;
                Minutos = DuracionProtocolo.Minutos(p.Bloques);
                Nota = p.Nombre;
            }
        }

        var editId = editarProtocolo ?? id;
        if (nuevo || nuevoProtocolo || editId is not null)
        {
            Editando = true;
            if (editId is { } eid)
            {
                var fila = Lista.FirstOrDefault(x => x.Protocolo.Id == eid);
                if (fila is not null)
                {
                    ProtoId = fila.Protocolo.Id;
                    ProtoNombre = fila.Protocolo.Nombre;
                    ProtoDescripcion = fila.Protocolo.Descripcion;
                    ProtoMinutos = fila.Minutos;
                    ProtoKcal = fila.KcalEst > 0 ? fila.KcalEst : null;
                }
            }
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        var kcal = ServicioCardio.KcalEfectivas(Kcal, KcalMin, KcalMax);
        await cardio.GuardarSesionAsync(new SesionCardio
        {
            Fecha = Fecha,
            ProtocoloId = ProtocoloId == 0 ? null : ProtocoloId,
            Minutos = Minutos,
            Velocidad = Velocidad,
            Inclinacion = Inclinacion,
            Kcal = kcal,
            KcalMin = KcalMin,
            KcalMax = KcalMax,
            Nota = Nota
        });
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBorrarAsync(int id)
    {
        await cardio.EliminarSesionAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostProtocoloAsync()
    {
        if (string.IsNullOrWhiteSpace(ProtoNombre))
        {
            await CargarAsync();
            Editando = true;
            Error = "Falta el nombre.";
            return Page();
        }

        if (ProtoMinutos <= 0)
        {
            await CargarAsync();
            Editando = true;
            Error = "Poné los minutos.";
            return Page();
        }

        if (ProtoKcal is not > 0)
        {
            await CargarAsync();
            Editando = true;
            Error = "Poné las kcal.";
            return Page();
        }

        BloqueCardio? previo = null;
        if (ProtoId != 0)
        {
            var actual = await db.ProtocolosCardio.Include(p => p.Bloques)
                .FirstOrDefaultAsync(p => p.Id == ProtoId);
            previo = actual?.Bloques.OrderBy(b => b.Orden).FirstOrDefault();
        }

        var bloques = new List<BloqueCardio>
        {
            new()
            {
                Minutos = ProtoMinutos,
                Velocidad = previo?.Velocidad,
                Inclinacion = previo?.Inclinacion,
                Repeticiones = 1
            }
        };

        await cardio.GuardarProtocoloAsync(new ProtocoloCardio
        {
            Id = ProtoId,
            Nombre = ProtoNombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(ProtoDescripcion) ? null : ProtoDescripcion.Trim(),
            Kcal = ProtoKcal,
            Activo = true
        }, bloques);
        TempData["Mensaje"] = "Cardio guardado. Las sesiones ya registradas no cambian.";
        return Volver.A(this, Vuelta);
    }

    public async Task<IActionResult> OnPostBorrarProtocoloAsync(int id)
    {
        await cardio.DesactivarProtocoloAsync(id);
        return RedirectToPage();
    }

    private async Task CargarAsync()
    {
        Sesiones = await db.SesionesCardio.Include(s => s.Protocolo).OrderByDescending(s => s.Fecha).Take(30).ToListAsync();
        var protocolos = await db.ProtocolosCardio.Include(p => p.Bloques).Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        var usos = await db.SesionesCardio.Where(s => s.ProtocoloId != null)
            .GroupBy(s => s.ProtocoloId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
        var medicion = await ServicioMediciones.ObtenerReferenciaAsync(db);
        var peso = medicion?.PesoKg ?? 70;
        Lista = protocolos.Select(p => new FilaCardio(
            p,
            DuracionProtocolo.Minutos(p.Bloques),
            Redondeo.Kcal(EstimacionCardio.DeProtocolo(p, peso)),
            usos.GetValueOrDefault(p.Id))).ToList();
        if (TempData["Mensaje"] is string m) Mensaje = m;
    }

    public sealed record FilaCardio(ProtocoloCardio Protocolo, double Minutos, double KcalEst, int Sesiones);
}
