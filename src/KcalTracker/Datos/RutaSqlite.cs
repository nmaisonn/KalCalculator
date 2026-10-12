namespace KcalTracker.Datos;

/// <summary>
/// La base vive en <c>data/kcal.db</c> junto al repo para que clone y commit
/// se lleven el historial. Si no hay solución cerca (un publish suelto),
/// cae a LocalAppData.
/// </summary>
public static class RutaSqlite
{
    public const string NombreArchivo = "kcal.db";

    public static string Cadena(string? contentRoot = null)
    {
        var archivo = Archivo(contentRoot);
        var carpeta = Path.GetDirectoryName(archivo);
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);
        return $"Data Source={archivo}";
    }

    public static string Archivo(string? contentRoot = null)
    {
        var raiz = RaizRepo(contentRoot);
        if (raiz is not null)
            return Path.Combine(raiz, "data", NombreArchivo);

        var local = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "kcal-tracker");
        Directory.CreateDirectory(local);
        return Path.Combine(local, NombreArchivo);
    }

    private static string? RaizRepo(string? contentRoot)
    {
        var actual = new DirectoryInfo(
            string.IsNullOrWhiteSpace(contentRoot) ? Directory.GetCurrentDirectory() : contentRoot);
        while (actual is not null)
        {
            if (File.Exists(Path.Combine(actual.FullName, "KcalTracker.slnx")))
                return actual.FullName;
            actual = actual.Parent;
        }
        return null;
    }
}
