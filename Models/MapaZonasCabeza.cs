using System.Text;

namespace IdermaCapilar_Historial.Models;

public enum VistaMapa
{
    Cabeza,
    Frente,
    Dorso
}

public sealed record ZonaCabeza(string Id, string Nombre, string Archivo, VistaMapa Vista = VistaMapa.Cabeza);

public static class MapaZonasCabeza
{
    public const char Separador = '|';
    public const int Tamano = 1000;

    public static IReadOnlyList<VistaMapa> Vistas { get; } =
        [VistaMapa.Cabeza, VistaMapa.Frente, VistaMapa.Dorso];

    public static IReadOnlyList<ZonaCabeza> Cabeza { get; } =
    [
        new("zona4", "Línea frontal", "zona4.png"),
        new("zona3", "Medio", "zona3.png"),
        new("zona2", "Zona media", "zona2.png"),
        new("zona1", "Vértex", "zona1.png"),
        new("circulo", "Coronilla", "circulo.png"),
        new("izquierda", "Temporal izquierda", "izquierda.png"),
        new("derecha", "Temporal derecha", "derecha.png")
    ];

    public static IReadOnlyList<ZonaCabeza> Frente { get; } =
    [
        new("f_submenton", "Submentón (papada)", "cuello.png", VistaMapa.Frente),
        new("f_brazo_izq", "Brazo izquierdo anterior", "brazoizquierdo.png", VistaMapa.Frente),
        new("f_brazo_der", "Brazo derecho anterior", "brazoderecho.png", VistaMapa.Frente),
        new("f_hipocondrio_izq", "Hipocondrio izquierdo", "superiorizquierdo.png", VistaMapa.Frente),
        new("f_hipocondrio_der", "Hipocondrio derecho", "superiorderecho.png", VistaMapa.Frente),
        new("f_abdomen", "Abdomen", "central.png", VistaMapa.Frente),
        new("f_fosa_iliaca_izq", "Fosa ilíaca izquierda", "inferiorizquierdo.png", VistaMapa.Frente),
        new("f_fosa_iliaca_der", "Fosa ilíaca derecha", "inferiorderecho.png", VistaMapa.Frente),
        new("f_pubis", "Región púbica", "pelvis.png", VistaMapa.Frente),
        new("f_muslo_izq", "Muslo izquierdo anterior", "piernaizquierda.png", VistaMapa.Frente),
        new("f_muslo_der", "Muslo derecho anterior", "piernaderecha.png", VistaMapa.Frente)
    ];

    public static IReadOnlyList<ZonaCabeza> Dorso { get; } =
    [
        new("d_brazo_izq", "Brazo izquierdo posterior", "brazoizquierdo.png", VistaMapa.Dorso),
        new("d_brazo_der", "Brazo derecho posterior", "brazoderecho.png", VistaMapa.Dorso),
        new("d_escapular_izq", "Región escapular izquierda", "espaldaizquierda.png", VistaMapa.Dorso),
        new("d_escapular_der", "Región escapular derecha", "espaldaderecha.png", VistaMapa.Dorso),
        new("d_lumbar_izq", "Región lumbar izquierda", "espaldabajaizquierda.png", VistaMapa.Dorso),
        new("d_lumbar_der", "Región lumbar derecha", "espaldabajaderecha.png", VistaMapa.Dorso),
        new("d_flanco_izq", "Flanco izquierdo", "rolloizquierdo.png", VistaMapa.Dorso),
        new("d_flanco_der", "Flanco derecho", "rolloderecho.png", VistaMapa.Dorso),
        new("d_gluteo_izq", "Glúteo izquierdo", "gluteoizquierdo.png", VistaMapa.Dorso),
        new("d_gluteo_der", "Glúteo derecho", "gluteoderecho.png", VistaMapa.Dorso),
        new("d_muslo_izq", "Muslo izquierdo posterior", "piernaizquierda.png", VistaMapa.Dorso),
        new("d_muslo_der", "Muslo derecho posterior", "piernaderecha.png", VistaMapa.Dorso)
    ];

    public static IReadOnlyList<ZonaCabeza> Zonas { get; } =
        Cabeza.Concat(Frente).Concat(Dorso).ToList();

    private static readonly Dictionary<string, string> Alias = new(StringComparer.OrdinalIgnoreCase)
    {
        ["frontal"] = "zona4",
        ["medio"] = "zona3",
        ["vertex"] = "circulo",
        ["entrada_izq"] = "izquierda",
        ["entrada_der"] = "derecha",
        ["donante"] = "zona1",
        ["zonaizquierda"] = "izquierda",
        ["zonaderecha"] = "derecha",
        ["zonacirculo"] = "circulo",
        ["cuello"] = "f_submenton",
        ["papada"] = "f_submenton",
        ["central"] = "f_abdomen",
        ["pelvis"] = "f_pubis",
        ["superiorderecho"] = "f_hipocondrio_der",
        ["superiorizquierdo"] = "f_hipocondrio_izq",
        ["inferiorderecho"] = "f_fosa_iliaca_der",
        ["inferiorizquierdo"] = "f_fosa_iliaca_izq",
        ["rolloderecho"] = "d_flanco_der",
        ["rolloizquierdo"] = "d_flanco_izq"
    };

    public static IReadOnlyList<ZonaCabeza> De(VistaMapa vista) => vista switch
    {
        VistaMapa.Frente => Frente,
        VistaMapa.Dorso => Dorso,
        _ => Cabeza
    };

    public static string Titulo(VistaMapa vista) => vista switch
    {
        VistaMapa.Frente => "Cuerpo, vista anterior",
        VistaMapa.Dorso => "Cuerpo, vista posterior",
        _ => "Cabeza"
    };

    public static string CarpetaTema(bool oscuro) => oscuro ? "dark" : "light";

    public static string CarpetaVista(VistaMapa vista) => vista switch
    {
        VistaMapa.Frente => "MapaCuerpo/frente",
        VistaMapa.Dorso => "MapaCuerpo/dorso",
        _ => "MapaCabeza"
    };

    public static string RutaPaquete(VistaMapa vista, bool oscuro, string archivo)
        => $"ms-appx:///Assets/{CarpetaVista(vista)}/{CarpetaTema(oscuro)}/{archivo}";

    public static string RutaPaquete(bool oscuro, string archivo)
        => RutaPaquete(VistaMapa.Cabeza, oscuro, archivo);

    public static Uri UriCompleto(VistaMapa vista, bool oscuro)
        => new(RutaPaquete(vista, oscuro, "completo.png"));

    public static Uri UriCompleto(bool oscuro) => UriCompleto(VistaMapa.Cabeza, oscuro);

    public static Uri UriZona(ZonaCabeza zona, bool oscuro)
        => new(RutaPaquete(zona.Vista, oscuro, zona.Archivo));

    public static string RutaDisco(VistaMapa vista, bool oscuro, string archivo)
        => Path.Combine(AppContext.BaseDirectory, "Assets", CarpetaVista(vista).Replace('/', Path.DirectorySeparatorChar), CarpetaTema(oscuro), archivo);

    public static string RutaDisco(bool oscuro, string archivo)
        => RutaDisco(VistaMapa.Cabeza, oscuro, archivo);

    public static HashSet<string> Ids(string? guardado)
    {
        if (string.IsNullOrWhiteSpace(guardado))
        {
            return [];
        }

        var validos = Zonas.Select(z => z.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var bruto in guardado.Split(Separador, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var id = Alias.TryGetValue(bruto, out var alias) ? alias : bruto;
            if (validos.Contains(id))
            {
                set.Add(id);
            }
        }

        return set;
    }

    public static string Serializar(IEnumerable<string> ids)
    {
        var orden = Zonas.Select(z => z.Id).ToList();
        var set = Ids(string.Join(Separador, ids.Where(id => !string.IsNullOrWhiteSpace(id))));
        var lista = orden.Where(set.Contains).ToList();
        return lista.Count == 0 ? string.Empty : string.Join(Separador, lista);
    }

    public static string Nombres(string? guardado)
    {
        var set = Ids(guardado);
        var nombres = Zonas.Where(z => set.Contains(z.Id)).Select(z => z.Nombre).ToList();
        return nombres.Count == 0 ? string.Empty : string.Join(", ", nombres);
    }

    public static IReadOnlyList<(string Nombre, bool Operada)> Filas(string? guardado, VistaMapa? vista = null)
    {
        var set = Ids(guardado);
        var zonas = vista is null ? Zonas : De(vista.Value);
        return zonas.Select(z => (z.Nombre, set.Contains(z.Id))).ToList();
    }

    public static string Unir(IEnumerable<string?> guardados)
        => Serializar(guardados.SelectMany(Ids));

    public static bool TieneVista(string? guardado, VistaMapa vista)
    {
        var set = Ids(guardado);
        return De(vista).Any(z => set.Contains(z.Id));
    }

    public static string Html(string? guardado, bool todasLasVistas = false)
    {
        var bloques = new StringBuilder();
        foreach (var vista in Vistas)
        {
            if (!todasLasVistas && !TieneVista(guardado, vista))
            {
                continue;
            }

            bloques.AppendLine(HtmlVista(vista, guardado, incluirVacias: todasLasVistas));
        }

        return bloques.ToString();
    }

    private static string HtmlVista(VistaMapa vista, string? guardado, bool incluirVacias = false)
    {
        var set = Ids(guardado);
        var capas = new List<string>
        {
            CapaHtml(RutaDisco(vista, false, "completo.png"), 0)
        };
        var z = 1;
        foreach (var zona in De(vista))
        {
            if (!set.Contains(zona.Id))
            {
                continue;
            }

            capas.Add(CapaHtml(RutaDisco(vista, false, zona.Archivo), z++));
        }

        var filas = new StringBuilder();
        foreach (var (nombre, operada) in Filas(guardado, vista))
        {
            if (!operada)
            {
                continue;
            }

            filas.AppendLine($"<tr><td>{Web(nombre)}</td><td class=\"si\">Sí</td></tr>");
        }

        if (filas.Length == 0 && incluirVacias)
        {
            filas.AppendLine("<tr><td colspan=\"2\">Ninguna en esta vista</td></tr>");
        }

        return $"""
            <h4>{Web(Titulo(vista))}</h4>
            <div class="mapa-cabeza">
              <div class="mapa-cabeza-fila">
                <div class="mapa-cabeza-lienzo" aria-label="{Web(Titulo(vista))}">
                  {string.Join(Environment.NewLine, capas)}
                </div>
                <table class="zonas-operadas">
                  <tr><th>Zona</th><th>Operada</th></tr>
                  {filas}
                </table>
              </div>
            </div>
            """;
    }

    private static readonly Dictionary<string, string> DataUriCache = new(StringComparer.OrdinalIgnoreCase);

    private static string CapaHtml(string ruta, int z)
    {
        var data = DataUri(ruta);
        if (string.IsNullOrEmpty(data))
        {
            return string.Empty;
        }

        return $"<img src=\"{data}\" alt=\"\" style=\"z-index:{z}\" />";
    }

    private static string DataUri(string ruta)
    {
        if (DataUriCache.TryGetValue(ruta, out var cached))
        {
            return cached;
        }

        if (!File.Exists(ruta))
        {
            return string.Empty;
        }

        var uri = "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(ruta));
        DataUriCache[ruta] = uri;
        return uri;
    }

    private static string Web(string texto) => System.Net.WebUtility.HtmlEncode(texto);
}
