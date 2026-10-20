using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Dialogs;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using IdermaCapilar_Historial.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace IdermaCapilar_Historial.Pages;

public sealed partial class EnfermerasPage : Page
{
    public EnfermerasViewModel ViewModel { get; } = new();

    public EnfermerasPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
        => UiSafe.Run(XamlRoot, () =>
        {
            ViewModel.Cargar();
            return Task.CompletedTask;
        }, "No se pudo cargar el listado de enfermeras");

    private void Nuevo_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, () => EditarAsync(null));

    private void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Enfermera enfermera })
        {
            UiSafe.Run(XamlRoot, () => EditarAsync(enfermera));
        }
    }

    private void Eliminar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Enfermera enfermera })
        {
            UiSafe.Run(XamlRoot, () => EliminarAsync(enfermera));
        }
    }

    private async Task EliminarAsync(Enfermera enfermera)
    {
        if (!await DialogHelper.ConfirmarAsync(XamlRoot, "Quitar enfermera", $"¿Eliminar a {enfermera.NombreMostrado} del listado?"))
        {
            return;
        }

        AppDatabase.Instance.EliminarEnfermera(enfermera.Id);
        EnfermeraCatalog.Instance.Recargar();
        ViewModel.Cargar();
    }

    private async Task EditarAsync(Enfermera? existente)
    {
        var dialog = new EnfermeraDialog { XamlRoot = XamlRoot };
        dialog.Cargar(existente);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        AppDatabase.Instance.GuardarEnfermera(dialog.Enfermera);
        EnfermeraCatalog.Instance.Recargar();
        ViewModel.Cargar();
    }
}
