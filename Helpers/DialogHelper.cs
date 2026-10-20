using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace IdermaCapilar_Historial.Helpers;

public static class DialogHelper
{
    public static async Task<bool> ConfirmarAsync(XamlRoot? root, string titulo, string mensaje, string confirmar = "Eliminar")
    {
        if (root is null)
        {
            return false;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Title = titulo,
            Content = mensaje,
            PrimaryButtonText = confirmar,
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    public static async Task AvisoAsync(XamlRoot? root, string titulo, string mensaje)
    {
        if (root is null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Title = titulo,
            Content = mensaje,
            CloseButtonText = "Aceptar"
        };
        await dialog.ShowAsync();
    }

    public static async Task MostrarFotoAsync(XamlRoot? root, string? ruta, string titulo)
    {
        if (root is null)
        {
            return;
        }
        var imagen = await FotoPaciente.CargarAsync(ruta);
        if (imagen is null)
        {
            await AvisoAsync(root, titulo, "Este paciente aún no tiene fotografía.");
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Title = titulo,
            CloseButtonText = "Cerrar",
            Content = new ScrollViewer
            {
                ZoomMode = ZoomMode.Enabled,
                MinZoomFactor = 1,
                MaxZoomFactor = 4,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollMode = ScrollMode.Auto,
                VerticalScrollMode = ScrollMode.Auto,
                MaxWidth = 920,
                MaxHeight = 720,
                Content = new Image
                {
                    Source = imagen,
                    Stretch = Stretch.Uniform,
                    MinWidth = 280,
                    MinHeight = 280
                }
            }
        };

        await dialog.ShowAsync();
    }

    public static async Task<string?> PedirTextoAsync(
        XamlRoot? root,
        string titulo,
        string mensaje,
        string confirmar = "Continuar",
        string placeholder = "Escriba aquí...")
    {
        if (root is null)
        {
            return null;
        }

        var caja = new TextBox
        {
            AcceptsReturn = true,
            Height = 100,
            PlaceholderText = placeholder,
            TextWrapping = TextWrapping.Wrap
        };
        var error = new TextBlock
        {
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Firebrick),
            Text = "Escriba el motivo para continuar.",
            Visibility = Visibility.Collapsed,
            TextWrapping = TextWrapping.Wrap
        };
        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Title = titulo,
            PrimaryButtonText = confirmar,
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            Content = new StackPanel
            {
                Spacing = 8,
                MinWidth = 360,
                Children =
                {
                    new TextBlock { Text = mensaje, TextWrapping = TextWrapping.Wrap },
                    caja,
                    error
                }
            }
        };
        dialog.PrimaryButtonClick += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(caja.Text))
            {
                return;
            }

            error.Visibility = Visibility.Visible;
            args.Cancel = true;
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary
            ? caja.Text.Trim()
            : null;
    }
}
