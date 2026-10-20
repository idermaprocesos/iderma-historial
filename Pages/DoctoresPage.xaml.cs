using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Dialogs;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using IdermaCapilar_Historial.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace IdermaCapilar_Historial.Pages;

public sealed partial class DoctoresPage : Page
{
    public DoctoresViewModel ViewModel { get; } = new();

    public DoctoresPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
        => UiSafe.Run(XamlRoot, () =>
        {
            ViewModel.Cargar();
            return Task.CompletedTask;
        }, "No se pudo cargar el listado de doctores");

    private void Nuevo_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, () => EditarAsync(null));

    private void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Doctor doctor })
        {
            UiSafe.Run(XamlRoot, () => EditarAsync(doctor));
        }
    }

    private void Eliminar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Doctor doctor })
        {
            UiSafe.Run(XamlRoot, () => EliminarAsync(doctor));
        }
    }

    private async Task EliminarAsync(Doctor doctor)
    {
        if (!await DialogHelper.ConfirmarAsync(XamlRoot, "Quitar doctor", $"¿Eliminar a {doctor.NombreMostrado} del listado?"))
        {
            return;
        }

        AppDatabase.Instance.EliminarDoctor(doctor.Id);
        DoctorCatalog.Instance.Recargar();
        ViewModel.Cargar();
    }

    private async Task EditarAsync(Doctor? existente)
    {
        var dialog = new DoctorDialog { XamlRoot = XamlRoot };
        dialog.Cargar(existente);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        AppDatabase.Instance.GuardarDoctor(dialog.Doctor);
        DoctorCatalog.Instance.Recargar();
        ViewModel.Cargar();
    }
}
