using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Helpers;

public static class AppNavigation
{
    public static Frame? Frame { get; set; }

    public static bool Navigate(Type pageType, object? parameter = null)
    {
        return Frame is not null && Frame.Navigate(pageType, parameter);
    }

    public static void Reiniciar(Type pageType)
    {
        if (Frame is null)
        {
            return;
        }

        Frame.Navigate(pageType);
        Frame.BackStack.Clear();
    }

    public static void GoBack()
    {
        if (Frame?.CanGoBack == true)
        {
            Frame.GoBack();
        }
    }
}

public sealed class PacientesNavegacion
{
    public bool SoloActivos { get; init; }
}

public sealed class CitasNavegacion
{
    public string? Vista { get; init; }
    public DateTime? Fecha { get; init; }
}

public sealed class PacienteDetalleNavegacion
{
    public int PacienteId { get; init; }
    public string? Pestaña { get; init; }
}
