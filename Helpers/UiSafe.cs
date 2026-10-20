using Microsoft.UI.Xaml;

namespace IdermaCapilar_Historial.Helpers;

public static class UiSafe
{
    public static async void Run(XamlRoot? root, Func<Task> accion, string titulo = "No se pudo completar la acción")
    {
        await Await(root, accion, titulo);
    }

    public static async Task Await(XamlRoot? root, Func<Task> accion, string titulo = "No se pudo completar la acción")
    {
        try
        {
            await accion();
        }
        catch (Exception ex)
        {
            await MostrarAsync(root, titulo, ex.Message);
        }
    }

    public static async Task MostrarAsync(XamlRoot? root, string titulo, string mensaje)
    {
        try
        {
            if (root is not null)
            {
                await DialogHelper.AvisoAsync(root, titulo, mensaje);
            }
        }
        catch
        {
            // Evita un segundo crash si el diálogo tampoco puede mostrarse.
        }
    }
}
