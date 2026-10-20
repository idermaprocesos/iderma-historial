using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using IdermaCapilar_Historial.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace IdermaCapilar_Historial.Pages;

public sealed partial class OperacionesPage : Page
{
    public OperacionesViewModel ViewModel { get; } = new();

    public OperacionesPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
        => UiSafe.Run(XamlRoot, () =>
        {
            ViewModel.Cargar();
            return Task.CompletedTask;
        }, "No se pudieron cargar las operaciones");

    private void CargarMas_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, () =>
        {
            ViewModel.CargarMas();
            return Task.CompletedTask;
        }, "No se pudieron cargar más operaciones");

    private void Lista_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Procedimiento operacion)
        {
            AppNavigation.Navigate(typeof(OperacionDetallePage), operacion.Id);
        }
    }
}
