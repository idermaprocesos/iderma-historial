using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace IdermaCapilar_Historial.Helpers;

public static class FotoPaciente
{
    public static async Task<BitmapImage?> CargarAsync(string? ruta, int? anchoMiniatura = null)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
        {
            return null;
        }

        try
        {
            var image = new BitmapImage();
            if (anchoMiniatura is > 0)
            {
                image.DecodePixelWidth = anchoMiniatura.Value;
            }

            var archivo = await StorageFile.GetFileFromPathAsync(ruta);
            using var origen = await archivo.OpenAsync(FileAccessMode.Read);
            var memoria = new InMemoryRandomAccessStream();
            await RandomAccessStream.CopyAsync(origen, memoria);
            memoria.Seek(0);
            await image.SetSourceAsync(memoria);
            return image;
        }
        catch
        {
            return null;
        }
    }

    public static async Task AsignarAsync(IEnumerable<Paciente> pacientes)
    {
        foreach (var paciente in pacientes)
        {
            if (string.IsNullOrWhiteSpace(paciente.FotoRuta))
            {
                continue;
            }

            try
            {
                paciente.Foto = await CargarAsync(paciente.FotoRuta, 96);
            }
            catch
            {
                paciente.Foto = null;
            }
        }
    }
}
