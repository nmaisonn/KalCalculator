using System.Text.Json;
using System.Text.Json.Serialization;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

public class RespaldoCuenta
{
    public string Email { get; set; } = "";
    public DateTime ExportadoEl { get; set; }
    public Perfil? Perfil { get; set; }
    public Configuracion? Configuracion { get; set; }
    public List<Medicion> Mediciones { get; set; } = [];
    public List<Alimento> Alimentos { get; set; } = [];
    public List<Combo> Combos { get; set; } = [];
    public List<Actividad> Actividades { get; set; } = [];
    public List<Ejercicio> Ejercicios { get; set; } = [];
    public List<Rutina> Rutinas { get; set; } = [];
    public List<ProtocoloCardio> Protocolos { get; set; } = [];
    public List<Comida> Comidas { get; set; } = [];
    public List<ActividadLog> ActividadLogs { get; set; } = [];
    public List<SesionGym> SesionesGym { get; set; } = [];
    public List<SesionCardio> SesionesCardio { get; set; } = [];
}

public class ServicioRespaldo(KcalDbContext db, IUsuarioActual usuario)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<byte[]> ExportarAsync(CancellationToken ct = default)
    {
        var cuenta = await db.Usuarios.FirstAsync(u => u.Id == usuario.UsuarioId, ct);
        var datos = new RespaldoCuenta
        {
            Email = cuenta.Email,
            ExportadoEl = DateTime.UtcNow,
            Perfil = await db.Perfiles.SingleAsync(ct),
            Configuracion = await db.Configuraciones.SingleAsync(ct),
            Mediciones = await db.Mediciones.OrderBy(m => m.Fecha).ToListAsync(ct),
            Alimentos = await db.Alimentos.Where(a => a.UsuarioId == usuario.UsuarioId).ToListAsync(ct),
            Combos = await db.Combos.Include(c => c.Items).ToListAsync(ct),
            Actividades = await db.Actividades.ToListAsync(ct),
            Ejercicios = await db.Ejercicios.ToListAsync(ct),
            Rutinas = await db.Rutinas.Include(r => r.Dias).ThenInclude(d => d.Ejercicios).ToListAsync(ct),
            Protocolos = await db.ProtocolosCardio.Include(p => p.Bloques).ToListAsync(ct),
            Comidas = await db.Comidas.Include(c => c.Items).ToListAsync(ct),
            ActividadLogs = await db.ActividadLogs.ToListAsync(ct),
            SesionesGym = await db.SesionesGym.Include(s => s.Ejercicios).ToListAsync(ct),
            SesionesCardio = await db.SesionesCardio.ToListAsync(ct)
        };
        return JsonSerializer.SerializeToUtf8Bytes(datos, Json);
    }
}

public sealed record SugerenciaNutricional(
    string Nombre,
    string Porcion,
    double Kcal,
    double? Proteina,
    double? Carbos,
    double? Grasa,
    string Fuente,
    Calculo.ResultadoSugerencia Validacion,
    Dominio.OrigenDato Origen = Dominio.OrigenDato.OpenFoodFacts,
    bool EsEstimacion = false,
    string? Nota = null);
