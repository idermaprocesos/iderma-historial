using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using IdermaCapilar_Historial.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace IdermaCapilar_Historial.Pages;

public sealed partial class PacientesPage : Page
{
    public PacientesViewModel ViewModel { get; } = new();

    public PacientesPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.AplicarNavegacion(e.Parameter is PacientesNavegacion { SoloActivos: true });
        await UiSafe.Await(XamlRoot, () => ViewModel.CargarAsync(), "No se pudieron cargar los pacientes");
    }

    private void Nuevo_Click(object sender, RoutedEventArgs e)
    {
        AppNavigation.Navigate(typeof(PacienteFormPage));
    }

    private async void VerFoto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Paciente paciente })
        {
            await UiSafe.Await(XamlRoot, () => DialogHelper.MostrarFotoAsync(XamlRoot, paciente.FotoRuta, paciente.NombreCompleto));
        }
    }

    private async void CargarMas_Click(object sender, RoutedEventArgs e)
    {
        await UiSafe.Await(XamlRoot, () => ViewModel.CargarMasAsync(), "No se pudieron cargar más pacientes");
    }

    private void Lista_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Paciente paciente)
        {
            AppNavigation.Navigate(typeof(PacienteDetallePage), paciente.Id);
        }
    }
}
