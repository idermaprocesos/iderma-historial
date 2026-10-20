using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Dialogs;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace IdermaCapilar_Historial.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _cargando;
    private readonly AlertaTemporal _alerta;
    public IReadOnlyList<string> VistasCalendario => Catalogos.VistasCalendario;

    public SettingsPage()
    {
        InitializeComponent();
        _alerta = new AlertaTemporal(() => Info.IsOpen = false);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _cargando = true;
        var settings = AppSettings.Current;
        AutoSwitch.IsOn = settings.Autoguardado;
        RutaText.Text = $"Carpeta: {AppDatabase.Instance.FolderPath}";

        foreach (ComboBoxItem item in IntervaloBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag as string == settings.AutoguardadoMinutos.ToString())
            {
                IntervaloBox.SelectedItem = item;
            }
        }

        IntervaloBox.SelectedItem ??= IntervaloBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag as string == "15");
        HoraInicioPicker.Time = settings.HoraInicioAgenda;
        HoraFinPicker.Time = settings.HoraFinAgenda;
        SeleccionarTag(DuracionCitaBox, settings.CitasDuracionMinutos.ToString(), "30");
        SeleccionarTag(IntervaloCitaBox, settings.CitasIntervaloMinutos.ToString(), "30");
        VistaCitaBox.SelectedItem = Catalogos.VistasCalendario.Contains(settings.CitasVistaPredeterminada)
            ? settings.CitasVistaPredeterminada
            : "Mes";
        SabadoSwitch.IsOn = settings.CitasAtenderSabado;
        DomingoSwitch.IsOn = settings.CitasAtenderDomingo;
        MostrarMotivos();
        _cargando = false;
    }

    private void MostrarMotivos()
    {
        var settings = AppSettings.Current;
        settings.AsegurarMotivos();
        MotivosCitaLista.ItemsSource = null;
        MotivosCitaLista.ItemsSource = settings.MotivosCita.ToList();
        MotivosOperacionLista.ItemsSource = null;
        MotivosOperacionLista.ItemsSource = settings.MotivosOperacion.ToList();
    }

    private void GuardarAgenda_Click(object sender, RoutedEventArgs e)
    {
        var settings = AppSettings.Current;
        settings.CitasHoraInicio = DateTime.Today.Add(HoraInicioPicker.Time).ToString("HH:mm");
        settings.CitasHoraFin = DateTime.Today.Add(HoraFinPicker.Time).ToString("HH:mm");
        if (DuracionCitaBox.SelectedItem is ComboBoxItem { Tag: string duracion } && int.TryParse(duracion, out var minutos))
        {
            settings.CitasDuracionMinutos = minutos;
        }

        if (IntervaloCitaBox.SelectedItem is ComboBoxItem { Tag: string intervalo } && int.TryParse(intervalo, out var slot))
        {
            settings.CitasIntervaloMinutos = slot;
        }

        settings.CitasVistaPredeterminada = VistaCitaBox.SelectedItem as string ?? "Mes";
        settings.CitasAtenderSabado = SabadoSwitch.IsOn;
        settings.CitasAtenderDomingo = DomingoSwitch.IsOn;
        settings.Save();
        Mostrar("Agenda de citas actualizada.");
    }

    private void NuevoMotivoCita_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, () => EditarMotivoAsync(AppSettings.Current.MotivosCita, null, "Nuevo motivo de cita"));

    private void EditarMotivoCita_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TipoAgendaConfig motivo })
        {
            UiSafe.Run(XamlRoot, () => EditarMotivoAsync(AppSettings.Current.MotivosCita, motivo, "Editar motivo de cita"));
        }
    }

    private void EliminarMotivoCita_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TipoAgendaConfig motivo })
        {
            UiSafe.Run(XamlRoot, () => EliminarMotivoAsync(AppSettings.Current.MotivosCita, motivo));
        }
    }

    private void NuevoMotivoOperacion_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, () => EditarMotivoAsync(AppSettings.Current.MotivosOperacion, null, "Nuevo motivo de operación"));

    private void EditarMotivoOperacion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TipoAgendaConfig motivo })
        {
            UiSafe.Run(XamlRoot, () => EditarMotivoAsync(AppSettings.Current.MotivosOperacion, motivo, "Editar motivo de operación"));
        }
    }

    private void EliminarMotivoOperacion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TipoAgendaConfig motivo })
        {
            UiSafe.Run(XamlRoot, () => EliminarMotivoAsync(AppSettings.Current.MotivosOperacion, motivo));
        }
    }

    private void RestaurarMotivos_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, RestaurarMotivosAsync);

    private async Task RestaurarMotivosAsync()
    {
        if (!await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Restaurar motivos",
                "Se reemplazarán los motivos y colores actuales por los predeterminados.",
                "Restaurar"))
        {
            return;
        }

        var settings = AppSettings.Current;
        settings.MotivosCita = Catalogos.MotivosCitaPredeterminados.Select(TipoAgendaConfig.From).ToList();
        settings.MotivosOperacion = Catalogos.MotivosOperacionPredeterminados.Select(TipoAgendaConfig.From).ToList();
        settings.Save();
        MostrarMotivos();
        Mostrar("Motivos restaurados.");
    }

    private async Task EditarMotivoAsync(List<TipoAgendaConfig> lista, TipoAgendaConfig? existente, string titulo)
    {
        var dialog = new TipoMotivoDialog { XamlRoot = XamlRoot };
        dialog.Cargar(existente, titulo);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        var duplicado = lista.Any(m =>
            !ReferenceEquals(m, existente) &&
            m.Nombre.Equals(dialog.Motivo.Nombre, StringComparison.CurrentCultureIgnoreCase));
        if (duplicado)
        {
            Mostrar("Ya existe un motivo con ese nombre.", false);
            return;
        }

        if (existente is null)
        {
            lista.Add(dialog.Motivo);
        }
        else
        {
            existente.Nombre = dialog.Motivo.Nombre;
            existente.R = dialog.Motivo.R;
            existente.G = dialog.Motivo.G;
            existente.B = dialog.Motivo.B;
        }

        AppSettings.Current.Save();
        MostrarMotivos();
        Mostrar("Motivo guardado.");
    }

    private async Task EliminarMotivoAsync(List<TipoAgendaConfig> lista, TipoAgendaConfig motivo)
    {
        if (lista.Count <= 1)
        {
            Mostrar("Debe quedar al menos un motivo.", false);
            return;
        }

        if (!await DialogHelper.ConfirmarAsync(XamlRoot, "Quitar motivo", $"¿Eliminar «{motivo.Nombre}»?", "Quitar"))
        {
            return;
        }

        lista.Remove(motivo);
        AppSettings.Current.Save();
        MostrarMotivos();
        Mostrar("Motivo eliminado.");
    }

    private static void SeleccionarTag(ComboBox combo, string valor, string respaldo)
    {
        combo.SelectedItem = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag as string == valor)
            ?? combo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag as string == respaldo);
    }

    private void AutoSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando)
        {
            return;
        }

        var settings = AppSettings.Current;
        settings.Autoguardado = AutoSwitch.IsOn;
        settings.Save();
        AutoGuardadoService.Instance.AplicarAjustes();
    }

    private void Intervalo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || IntervaloBox.SelectedItem is not ComboBoxItem { Tag: string texto } || !int.TryParse(texto, out var minutos))
        {
            return;
        }

        var settings = AppSettings.Current;
        settings.AutoguardadoMinutos = minutos;
        settings.Save();
        AutoGuardadoService.Instance.AplicarAjustes();
    }

    private void Exportar_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, ExportarAsync);

    private async Task ExportarAsync()
    {
        if (App.MainAppWindow is null)
        {
            return;
        }

        var picker = new FileSavePicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainAppWindow));
        picker.SuggestedFileName = $"IdermaCapilar-historial-{DateTime.Now:yyyyMMdd}";
        picker.FileTypeChoices.Add("Copia de historial", [".zip"]);
        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        BackupService.Exportar(file.Path);
        Mostrar("Datos exportados correctamente.");
    }

    private void Importar_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, ImportarAsync);

    private async Task ImportarAsync()
    {
        if (App.MainAppWindow is null)
        {
            return;
        }

        if (!await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Importar datos",
                "Se reemplazarán los expedientes actuales. Antes se creará una copia de seguridad automática.",
                "Importar"))
        {
            return;
        }

        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainAppWindow));
        picker.FileTypeFilter.Add(".zip");
        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        BackupService.Importar(file.Path);
        DoctorCatalog.Instance.Recargar();
        EnfermeraCatalog.Instance.Recargar();
        AppNavigation.Reiniciar(typeof(InicioPage));
    }

    private void AbrirCarpeta_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, async () =>
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(AppDatabase.Instance.FolderPath);
        }, "No se pudo abrir la carpeta");

    private void Mostrar(string mensaje, bool ok = true)
    {
        Info.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        Info.Message = mensaje;
        Info.IsOpen = true;
        _alerta.Programar(ok);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _alerta.Detener();
        base.OnNavigatedFrom(e);
    }
}
