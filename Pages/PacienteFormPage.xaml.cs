using System.ComponentModel;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace IdermaCapilar_Historial.Pages;

public sealed partial class PacienteFormPage : Page
{
    public PacienteFormViewModel ViewModel { get; } = new();
    public AutoGuardadoService AutoGuardado => AutoGuardadoService.Instance;

    private readonly Func<bool> _guardarBorrador;
    private bool _cargandoNacimiento;

    public PacienteFormPage()
    {
        InitializeComponent();
        FechaUi.AplicarRango(NacimientoPicker);
        _guardarBorrador = () => ViewModel.Guardar(out _, silencioso: true);
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        try
        {
            _cargandoNacimiento = true;
            if (e.Parameter is int id)
            {
                if (!ViewModel.CargarExistente(id))
                {
                    await UiSafe.MostrarAsync(XamlRoot, "Expediente no encontrado", "El expediente ya no existe o no se pudo abrir.");
                    AppNavigation.GoBack();
                    return;
                }
            }
            else
            {
                ViewModel.CargarNuevo();
            }

            ViewModel.Paciente.PropertyChanged += OnCambioFormulario;
            ViewModel.Contacto.PropertyChanged += OnCambioFormulario;
            AutoGuardado.Registrar(_guardarBorrador);
            FechaUi.Asignar(NacimientoPicker, ViewModel.Paciente.FechaNacimiento);
        }
        catch (Exception ex)
        {
            await UiSafe.MostrarAsync(XamlRoot, "No se pudo abrir el formulario", ex.Message);
        }
        finally
        {
            DispatcherQueue.TryEnqueue(() => _cargandoNacimiento = false);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.Paciente.PropertyChanged -= OnCambioFormulario;
        ViewModel.Contacto.PropertyChanged -= OnCambioFormulario;
        AutoGuardado.Quitar(_guardarBorrador);
        try
        {
            _guardarBorrador();
        }
        catch
        {
            // El borrador se reintenta en el siguiente autoguardado.
        }
    }

    private void OnCambioFormulario(object? sender, PropertyChangedEventArgs e) => AutoGuardado.PedirGuardado();

    private void Nacimiento_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (_cargandoNacimiento)
        {
            return;
        }

        try
        {
            var nueva = FechaUi.DesdePicker(sender.Date);
            if (FechaUi.MismaFechaCalendario(ViewModel.Paciente.FechaNacimiento, nueva))
            {
                return;
            }

            ViewModel.Paciente.FechaNacimiento = nueva;
        }
        catch
        {
        }
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel.Guardar(out var id))
            {
                Frame.Navigate(typeof(PacienteDetallePage), id);
            }
        }
        catch (Exception ex)
        {
            await UiSafe.MostrarAsync(XamlRoot, "No se pudo guardar", ex.Message);
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        AppNavigation.GoBack();
    }

    private void Telefono_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        => TelefonoFormato.AplicarEnCaja(sender);
}
