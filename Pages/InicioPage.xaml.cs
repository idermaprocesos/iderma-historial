using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using IdermaCapilar_Historial.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace IdermaCapilar_Historial.Pages;

public sealed partial class InicioPage : Page
{
    public InicioViewModel ViewModel { get; } = new();

    public InicioPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        await UiSafe.Await(XamlRoot, () => ViewModel.CargarAsync());
    }

    private void NuevoExpediente_Click(object sender, RoutedEventArgs e)
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

    private void Paciente_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Paciente paciente)
        {
            AppNavigation.Navigate(typeof(PacienteDetallePage), paciente.Id);
        }
    }

    private void Cita_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Cita cita)
        {
            AppNavigation.Navigate(typeof(CitasPage), cita.FechaInicio.Date);
        }
    }

    private void VerAgenda_Click(object sender, RoutedEventArgs e)
    {
        AppNavigation.Navigate(typeof(CitasPage));
    }

    private void Pacientes_Click(object sender, RoutedEventArgs e)
        => AppNavigation.Navigate(typeof(PacientesPage));

    private void Activos_Click(object sender, RoutedEventArgs e)
        => AppNavigation.Navigate(typeof(PacientesPage), new PacientesNavegacion { SoloActivos = true });

    private void CitasHoy_Click(object sender, RoutedEventArgs e)
        => AppNavigation.Navigate(typeof(CitasPage), new CitasNavegacion { Vista = "Día", Fecha = DateTime.Today });

    private void Pendientes_Click(object sender, RoutedEventArgs e)
        => AppNavigation.Navigate(typeof(CitasPage), new CitasNavegacion { Vista = "Agenda", Fecha = DateTime.Today });

    private void Operaciones_Click(object sender, RoutedEventArgs e)
        => AppNavigation.Navigate(typeof(OperacionesPage));
}
