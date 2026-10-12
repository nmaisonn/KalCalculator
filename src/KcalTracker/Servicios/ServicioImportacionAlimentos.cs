using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using KcalTracker.Datos;
using KcalTracker.Dominio;
using Microsoft.EntityFrameworkCore;

namespace KcalTracker.Servicios;

public sealed record ResultadoImportacionAlimentos(
    int Agregados,
    int YaEstaban,
    IReadOnlyList<string> Errores);

public sealed record FilaAlimentoImport(
    int Linea,
    string Nombre,
    string Porcion,
    double Kcal,
    double? Proteina,
    double? Carbos,
    double? Grasa,
    string? TipoComida,
    bool EsAfuera,
    string? Categoria = null);

public class ServicioImportacionAlimentos(KcalDbContext db)
{
    public const string TipoContenidoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string NombrePlantilla = "alimentos.xlsx";
    private const int MaxFilas = 1000;

    public static byte[] PlantillaXlsx()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Alimentos");
        string[] encabezados =
        [
            "Nombre", "Porción", "kcal", "Proteína", "Carbos", "Grasa", "Tipo de comida", "Tipo de alimento", "De afuera"
        ];
        for (var i = 0; i < encabezados.Length; i++)
        {
            var celda = ws.Cell(1, i + 1);
            celda.Value = encabezados[i];
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("1F6B4A");
            celda.Style.Font.FontColor = XLColor.White;
        }

        ws.Cell(2, 1).Value = "Pechuga al horno";
        ws.Cell(2, 2).Value = "150 g";
        ws.Cell(2, 3).Value = 165;
        ws.Cell(2, 4).Value = 31;
        ws.Cell(2, 5).Value = 0;
        ws.Cell(2, 6).Value = 3.6;
        ws.Cell(2, 7).Value = "Almuerzo";
        ws.Cell(2, 8).Value = "Comida";
        ws.Cell(2, 9).Value = "no";

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
        ws.Column(1).Width = 28;
        ws.Column(7).Width = 16;
        ws.Column(8).Width = 16;

        var ayuda = wb.AddWorksheet("Ayuda");
        ayuda.Cell(1, 1).Value = "Cada fila es un alimento nuevo. Las columnas coinciden con la tabla de Alimentos.";
        ayuda.Cell(2, 1).Value = "Tipo de comida: tramo de la tabla (Desayuno, Almuerzo, Cena…). Vacío = sin tramo.";
        ayuda.Cell(3, 1).Value = "Tipo de alimento: cómo se agrupa en Hoy (Comida, Fruta, De afuera…). «De afuera» cuenta solo las kcal.";
        ayuda.Cell(4, 1).Value = "De afuera: sí / no. Si es sí y no hay tipo, se asigna «De afuera».";
        ayuda.Cell(5, 1).Value = "Si el nombre y la porción ya existen, esa fila se omite.";
        ayuda.Column(1).Width = 110;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<ResultadoImportacionAlimentos> ImportarAsync(Stream archivo, string nombreArchivo, CancellationToken ct = default)
    {
        IReadOnlyList<FilaAlimentoImport> filas;
        try
        {
            filas = EsCsv(nombreArchivo) ? LeerCsv(archivo) : LeerXlsx(archivo);
        }
        catch (Exception)
        {
            return new ResultadoImportacionAlimentos(0, 0, ["No se pudo leer el archivo. Probá con la plantilla .xlsx o un CSV exportado desde Excel."]);
        }

        return await ImportarFilasAsync(filas, ct);
    }

    public async Task<ResultadoImportacionAlimentos> ImportarFilasAsync(
        IReadOnlyList<FilaAlimentoImport> filas, CancellationToken ct = default)
    {
        if (filas.Count == 0)
            return new(0, 0, ["El archivo no tiene filas de alimentos."]);
        if (filas.Count > MaxFilas)
            return new(0, 0, [$"Demasiadas filas ({filas.Count}). El máximo es {MaxFilas}."]);

        var existentes = await db.Alimentos
            .Where(a => a.Activo && a.UsuarioId == db.UsuarioIdActual)
            .ToListAsync(ct);
        var tipos = await db.TiposComida.Where(t => t.Activo).ToListAsync(ct);
        var afueraCat = await new ServicioCatalogos(db).AsegurarAfueraAsync(ct);
        var categorias = await db.CategoriasAlimento
            .Where(c => c.Activo && c.UsuarioId == db.UsuarioIdActual)
            .ToListAsync(ct);
        var vistos = new HashSet<string>(existentes.Select(a => Clave(a.Nombre, a.Porcion)));
        var agregados = 0;
        var yaEstaban = 0;
        var errores = new List<string>();

        foreach (var fila in filas)
        {
            if (string.IsNullOrWhiteSpace(fila.Nombre))
            {
                errores.Add($"Fila {fila.Linea}: falta el nombre.");
                continue;
            }
            if (fila.Kcal < 0)
            {
                errores.Add($"Fila {fila.Linea}: kcal inválidas o faltan.");
                continue;
            }

            var clave = Clave(fila.Nombre, fila.Porcion);
            if (!vistos.Add(clave))
            {
                yaEstaban++;
                continue;
            }

            int? tipoId = null;
            if (!string.IsNullOrWhiteSpace(fila.TipoComida))
            {
                var tipo = tipos.FirstOrDefault(t => Mismo(t.Nombre, fila.TipoComida));
                if (tipo is null)
                    errores.Add($"Fila {fila.Linea}: no hay un tramo «{fila.TipoComida.Trim()}». El alimento se carga igual, sin tipo.");
                else
                    tipoId = tipo.Id;
            }

            int? catId = null;
            if (!string.IsNullOrWhiteSpace(fila.Categoria))
            {
                var cat = categorias.FirstOrDefault(c => Mismo(c.Nombre, fila.Categoria));
                if (cat is null)
                    errores.Add($"Fila {fila.Linea}: no hay un tipo de alimento «{fila.Categoria.Trim()}». El alimento se carga igual, sin tipo.");
                else
                    catId = cat.Id;
            }

            var esAfuera = fila.EsAfuera || CategoriaAlimento.EsAfuera(fila.Categoria);
            if (esAfuera && catId is null)
                catId = afueraCat.Id;

            db.Alimentos.Add(new Alimento
            {
                Nombre = fila.Nombre.Trim(),
                Porcion = string.IsNullOrWhiteSpace(fila.Porcion) ? "1" : fila.Porcion.Trim(),
                Kcal = fila.Kcal,
                Proteina = fila.Proteina,
                Carbos = fila.Carbos,
                Grasa = fila.Grasa,
                TipoComidaId = tipoId,
                CategoriaAlimentoId = catId,
                EsAfuera = esAfuera,
                Activo = true,
                Origen = OrigenDato.Manual
            });
            agregados++;
        }

        if (agregados > 0)
            await db.SaveChangesAsync(ct);

        return new(agregados, yaEstaban, errores);
    }

    internal static IReadOnlyList<FilaAlimentoImport> LeerXlsx(Stream archivo)
    {
        using var wb = new XLWorkbook(archivo);
        var ws = wb.Worksheets.First();
        var usado = ws.RangeUsed();
        if (usado is null) return [];

        var filas = new List<string[]>();
        var cols = usado.ColumnCount();
        foreach (var fila in usado.Rows())
        {
            var celdas = new string[cols];
            for (var i = 0; i < cols; i++)
                celdas[i] = TextoCelda(fila.Cell(i + 1));
            filas.Add(celdas);
        }
        return MapearFilas(filas);
    }

    internal static IReadOnlyList<FilaAlimentoImport> LeerCsv(Stream archivo)
    {
        using var lector = new StreamReader(archivo, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var texto = lector.ReadToEnd();
        return MapearFilas(ParsearCsv(texto));
    }

    internal static IReadOnlyList<FilaAlimentoImport> MapearFilas(IReadOnlyList<string[]> filas)
    {
        if (filas.Count == 0) return [];
        var encabezado = filas[0].Select(NormaEncabezado).ToArray();
        var idxNombre = Indice(encabezado, "nombre", "alimento");
        var idxPorcion = Indice(encabezado, "porcion", "porción", "portion");
        var idxKcal = Indice(encabezado, "kcal", "calorias", "calorías", "energia", "energía");
        var idxProt = Indice(encabezado, "proteina", "proteína", "p");
        var idxCarb = Indice(encabezado, "carbos", "carbohidratos", "c", "hc");
        var idxGrasa = Indice(encabezado, "grasa", "grasas", "g");
        var idxTipo = Indice(encabezado, "tipo de comida", "tramo");
        var idxCat = Indice(encabezado, "tipo de alimento", "categoria", "categoría");
        var idxAfuera = Indice(encabezado, "de afuera", "afuera");

        if (idxNombre < 0 || idxKcal < 0)
            throw new InvalidDataException("Faltan las columnas Nombre y kcal.");

        var salida = new List<FilaAlimentoImport>();
        for (var i = 1; i < filas.Count; i++)
        {
            var f = filas[i];
            var nombre = Valor(f, idxNombre);
            if (string.IsNullOrWhiteSpace(nombre) && f.All(string.IsNullOrWhiteSpace))
                continue;
            if (!TryNumero(Valor(f, idxKcal), out var kcal) && string.IsNullOrWhiteSpace(nombre))
                continue;
            if (!TryNumero(Valor(f, idxKcal), out kcal))
            {
                salida.Add(new FilaAlimentoImport(i + 1, nombre, Valor(f, idxPorcion), -1, null, null, null, null, false));
                continue;
            }

            salida.Add(new FilaAlimentoImport(
                i + 1,
                nombre,
                Valor(f, idxPorcion),
                kcal,
                TryNumero(Valor(f, idxProt), out var p) ? p : null,
                TryNumero(Valor(f, idxCarb), out var c) ? c : null,
                TryNumero(Valor(f, idxGrasa), out var g) ? g : null,
                NullSiVacio(Valor(f, idxTipo)),
                EsSi(Valor(f, idxAfuera)),
                NullSiVacio(Valor(f, idxCat))));
        }
        return salida;
    }

    internal static List<string[]> ParsearCsv(string texto)
    {
        var lineas = texto.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lineas.Length == 0) return [];
        var sep = lineas[0].Count(c => c == ';') > lineas[0].Count(c => c == ',') ? ';' : ',';
        return lineas.Select(l => SplitCsv(l, sep)).ToList();
    }

    private static string[] SplitCsv(string linea, char sep)
    {
        var campos = new List<string>();
        var actual = new StringBuilder();
        var comillas = false;
        for (var i = 0; i < linea.Length; i++)
        {
            var c = linea[i];
            if (c == '"')
            {
                if (comillas && i + 1 < linea.Length && linea[i + 1] == '"')
                {
                    actual.Append('"');
                    i++;
                }
                else comillas = !comillas;
            }
            else if (c == sep && !comillas)
            {
                campos.Add(actual.ToString().Trim());
                actual.Clear();
            }
            else actual.Append(c);
        }
        campos.Add(actual.ToString().Trim());
        return campos.ToArray();
    }

    private static string TextoCelda(IXLCell celda)
    {
        if (celda.IsEmpty()) return "";
        return celda.DataType switch
        {
            XLDataType.Number => celda.GetDouble().ToString(CultureInfo.InvariantCulture),
            XLDataType.Boolean => celda.GetBoolean() ? "sí" : "no",
            _ => celda.GetFormattedString().Trim()
        };
    }

    private static bool EsCsv(string nombre)
        => Path.GetExtension(nombre ?? "").Equals(".csv", StringComparison.OrdinalIgnoreCase);

    private static string Clave(string nombre, string porcion)
        => NormaEncabezado(nombre) + "|" + NormaEncabezado(porcion);

    private static bool Mismo(string a, string b)
        => NormaEncabezado(a) == NormaEncabezado(b);

    public static bool TextoContiene(string texto, string q)
    {
        var n = NormaEncabezado(q);
        return n.Length == 0 || NormaEncabezado(texto).Contains(n, StringComparison.Ordinal);
    }

    private static string NormaEncabezado(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var form = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(form.Length);
        foreach (var ch in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static int Indice(IReadOnlyList<string> encabezado, params string[] alias)
    {
        for (var i = 0; i < encabezado.Count; i++)
        {
            if (alias.Any(a => encabezado[i] == NormaEncabezado(a)))
                return i;
        }
        return -1;
    }

    private static string Valor(string[] fila, int idx)
        => idx < 0 || idx >= fila.Length ? "" : fila[idx].Trim();

    private static string? NullSiVacio(string s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool TryNumero(string s, out double v)
    {
        v = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim().Replace("\u00A0", "").Replace(" ", "");
        if (s.Contains(','))
        {
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.GetCultureInfo("es-UY"), out v)) return true;
            return double.TryParse(s.Replace('.', '\0').Replace(',', '.').Replace("\0", ""),
                NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
    }

    private static bool EsSi(string s)
    {
        s = NormaEncabezado(s);
        return s is "si" or "sí" or "yes" or "true" or "1" or "x";
    }
}
