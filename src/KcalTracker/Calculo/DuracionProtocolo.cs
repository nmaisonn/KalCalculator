using KcalTracker.Dominio;

namespace KcalTracker.Calculo;

/// <summary>RF-L4, CA-37: duración total = Σ minutos × repeticiones.</summary>
public static class DuracionProtocolo
{
    public static double Minutos(IEnumerable<BloqueCardio> bloques)
        => bloques.Sum(b => b.Minutos * Math.Max(1, b.Repeticiones));
}
