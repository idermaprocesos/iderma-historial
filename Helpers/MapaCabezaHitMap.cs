using IdermaCapilar_Historial.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace IdermaCapilar_Historial.Helpers;

internal static class MapaCabezaHitMap
{
    private static readonly Dictionary<VistaMapa, MapaHit> Mapas = [];
    private static Task? _carga;

    public static Task AsegurarAsync()
    {
        return _carga ??= CargarTodosAsync();
    }

    public static string? ZonaEn(double x, double y, double ancho, double alto)
        => ZonaEn(VistaMapa.Cabeza, x, y, ancho, alto);

    public static string? ZonaEn(VistaMapa vista, double x, double y, double ancho, double alto)
    {
        if (!Mapas.TryGetValue(vista, out var mapa) || ancho <= 0 || alto <= 0)
        {
            return null;
        }

        var px = (int)(x * MapaZonasCabeza.Tamano / ancho);
        var py = (int)(y * MapaZonasCabeza.Tamano / alto);
        if (px < 0 || py < 0 || px >= MapaZonasCabeza.Tamano || py >= MapaZonasCabeza.Tamano)
        {
            return null;
        }

        var indice = mapa.Celdas[(py * MapaZonasCabeza.Tamano) + px];
        return indice == 0 ? null : mapa.Ids[indice - 1];
    }

    private static async Task CargarTodosAsync()
    {
        foreach (var vista in MapaZonasCabeza.Vistas)
        {
            Mapas[vista] = await CargarVistaAsync(vista);
        }
    }

    private static async Task<MapaHit> CargarVistaAsync(VistaMapa vista)
    {
        var zonas = MapaZonasCabeza.De(vista);
        var ids = zonas.Select(z => z.Id).ToArray();
        var celdas = new byte[MapaZonasCabeza.Tamano * MapaZonasCabeza.Tamano];

        for (var i = 0; i < zonas.Count; i++)
        {
            var zona = zonas[i];
            var uri = MapaZonasCabeza.UriZona(zona, true);
            StorageFile archivo;
            try
            {
                archivo = await StorageFile.GetFileFromApplicationUriAsync(uri);
            }
            catch
            {
                continue;
            }

            using IRandomAccessStream stream = await archivo.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var datos = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                new BitmapTransform
                {
                    ScaledWidth = MapaZonasCabeza.Tamano,
                    ScaledHeight = MapaZonasCabeza.Tamano,
                    InterpolationMode = BitmapInterpolationMode.NearestNeighbor
                },
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);
            var pixeles = datos.DetachPixelData();
            var marca = (byte)(i + 1);
            for (var p = 0; p < celdas.Length; p++)
            {
                var o = p * 4;
                if (EsRelleno(pixeles[o], pixeles[o + 1], pixeles[o + 2], pixeles[o + 3]))
                {
                    celdas[p] = marca;
                }
            }
        }

        return new MapaHit(ids, celdas);
    }

    private static bool EsRelleno(byte b, byte g, byte r, byte a)
    {
        if (a < 128)
        {
            return false;
        }

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        if (max - min < 28)
        {
            return false;
        }

        return g > r && b > r;
    }

    private sealed record MapaHit(string[] Ids, byte[] Celdas);
}
