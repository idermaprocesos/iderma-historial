using System.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Dialogs;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using IdermaCapilar_Historial.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace IdermaCapilar_Historial.Pages;

public sealed partial class PacienteDetallePage : Page
{
    public PacienteDetalleViewModel ViewModel { get; } = new();
    public AutoGuardadoService AutoGuardado => AutoGuardadoService.Instance;
    public DoctorCatalog Doctores => DoctorCatalog.Instance;

    private readonly Func<bool> _guardarSilencioso;
    private bool _cargandoNacimiento;

    public PacienteDetallePage()
    {
        InitializeComponent();
        FechaUi.AplicarRango(NacimientoPicker);
        _guardarSilencioso = ViewModel.GuardarSilencioso;
    }

    private void Pivot_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
    {
        args.Handled = true;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        await UiSafe.Await(XamlRoot, async () =>
        {
            _cargandoNacimiento = true;
            try
            {
            var pacienteId = e.Parameter switch
            {
                PacienteDetalleNavegacion destino => destino.PacienteId,
                int id => id,
                _ => 0
            };

            if (pacienteId > 0)
            {
                if (!ViewModel.Cargar(pacienteId))
                {
                    await DialogHelper.AvisoAsync(XamlRoot, "Expediente no encontrado", "El expediente ya no existe o no se pudo abrir.");
                    AppNavigation.GoBack();
                    return;
                }

                Doctores.Asegurar(ViewModel.DatosMedicos.MedicoResponsable);
                if (string.IsNullOrWhiteSpace(ViewModel.DatosMedicos.MedicoResponsable))
                {
                    ViewModel.DatosMedicos.MedicoResponsable = Doctores.Predeterminado;
                }

                try
                {
                    await ViewModel.CargarFotoAsync();
                }
                catch
                {
                }

                AplicarFotoEnControl();

                if (e.Parameter is PacienteDetalleNavegacion { Pestaña: "Operaciones" })
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        foreach (var item in DetallePivot.Items.OfType<PivotItem>())
                        {
                            if (item.Header as string == "Operaciones")
                            {
                                DetallePivot.SelectedItem = item;
                                break;
                            }
                        }
                    });
                }
            }

            ViewModel.Paciente.PropertyChanged += OnCambioFormulario;
            ViewModel.DatosMedicos.PropertyChanged += OnCambioFormulario;
            AutoGuardado.Registrar(_guardarSilencioso);
            FechaUi.Asignar(NacimientoPicker, ViewModel.Paciente.FechaNacimiento);
            }
            finally
            {
                DispatcherQueue.TryEnqueue(() => _cargandoNacimiento = false);
            }
        }, "No se pudo abrir el expediente");
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.Paciente.PropertyChanged -= OnCambioFormulario;
        ViewModel.DatosMedicos.PropertyChanged -= OnCambioFormulario;
        AutoGuardado.Quitar(_guardarSilencioso);
        try
        {
            _guardarSilencioso();
        }
        catch
        {
        }
    }

    private void OnCambioFormulario(object? sender, PropertyChangedEventArgs e)
    {
        AutoGuardado.PedirGuardado();
        if (e.PropertyName is nameof(Paciente.Nombres) or nameof(Paciente.Apellidos) or nameof(Paciente.FotoRuta))
        {
            AplicarFotoEnControl();
        }
    }

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

    private void Telefono_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        => TelefonoFormato.AplicarEnCaja(sender);

    private void GuardarPersonales_Click(object sender, RoutedEventArgs e)
    {
        UiSafe.Run(XamlRoot, () =>
        {
            ViewModel.GuardarPersonales();
            AplicarFotoEnControl();
            return Task.CompletedTask;
        }, "No se pudo guardar");
    }

    private void GuardarMedicos_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, () =>
        {
            ViewModel.GuardarMedicos();
            return Task.CompletedTask;
        }, "No se pudo guardar");

    private void ImprimirDatos_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, async () =>
        {
            try
            {
                ViewModel.GuardarSilencioso();
            }
            catch
            {
            }

            await ImpresionService.ImprimirDatosPacienteAsync(ViewModel.Paciente);
        }, "No se pudo imprimir");

    private void ImprimirHistorial_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, async () =>
        {
            try
            {
                ViewModel.GuardarSilencioso();
            }
            catch
            {
            }

            await ImpresionService.ImprimirHistorialClinicoAsync(ViewModel.Paciente.Id);
        }, "No se pudo imprimir");

    private void VerFoto_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, () => DialogHelper.MostrarFotoAsync(XamlRoot, ViewModel.Paciente.FotoRuta, ViewModel.Paciente.NombreCompleto));

    private void Foto_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, CambiarFotoAsync);

    private async Task CambiarFotoAsync()
    {
        if (App.MainAppWindow is null)
        {
            return;
        }

        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainAppWindow));
        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".webp");

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        var anterior = ViewModel.Paciente.FotoRuta;
        var extension = Path.GetExtension(file.Name);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var nombre = $"{ViewModel.Paciente.Id}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
        var carpeta = await StorageFolder.GetFolderFromPathAsync(AppDatabase.Instance.FotosPath);
        var destinoFile = await file.CopyAsync(carpeta, nombre, NameCollisionOption.ReplaceExisting);
        await ViewModel.ActualizarFotoAsync(destinoFile.Path);
        AplicarFotoEnControl();

        if (!string.IsNullOrWhiteSpace(anterior) &&
            !string.Equals(anterior, destinoFile.Path, StringComparison.OrdinalIgnoreCase) &&
            File.Exists(anterior))
        {
            try
            {
                File.Delete(anterior);
            }
            catch
            {
                // El archivo anterior puede seguir en uso.
            }
        }
    }

    private void AplicarFotoEnControl()
    {
        try
        {
            FotoPaciente.ProfilePicture = null;
            FotoPaciente.ProfilePicture = ViewModel.Foto;
        }
        catch
        {
        }
    }

    private void Eliminar_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, EliminarExpedienteAsync);

    private async Task EliminarExpedienteAsync()
    {
        if (!await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Eliminar expediente",
                $"Se eliminará de forma permanente el historial de {ViewModel.Paciente.NombreCompleto}, incluyendo citas, operaciones, observaciones y contactos."))
        {
            return;
        }

        ViewModel.EliminarExpediente();
        AppNavigation.Navigate(typeof(PacientesPage));
    }

    private void NuevoContacto_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, () => EditarContactoAsync(null));

    private void EditarContacto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ContactoEmergencia contacto })
        {
            UiSafe.Run(XamlRoot, () => EditarContactoAsync(contacto));
        }
    }

    private void EliminarContacto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ContactoEmergencia contacto })
        {
            UiSafe.Run(XamlRoot, async () =>
            {
                if (await DialogHelper.ConfirmarAsync(XamlRoot, "Quitar contacto", $"¿Eliminar a {contacto.Nombre}?"))
                {
                    ViewModel.EliminarContacto(contacto);
                }
            });
        }
    }

    private void NuevoProcedimiento_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, () => EditarProcedimientoAsync(null));

    private void Procedimiento_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Procedimiento procedimiento })
        {
            e.Handled = true;
            AppNavigation.Navigate(typeof(OperacionDetallePage), procedimiento.Id);
        }
    }

    private void VerProcedimiento_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Procedimiento procedimiento })
        {
            AppNavigation.Navigate(typeof(OperacionDetallePage), procedimiento.Id);
        }
    }

    private void EditarProcedimiento_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Procedimiento procedimiento })
        {
            UiSafe.Run(XamlRoot, () => EditarProcedimientoAsync(procedimiento));
        }
    }

    private void EliminarProcedimiento_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Procedimiento procedimiento })
        {
            UiSafe.Run(XamlRoot, async () =>
            {
                if (await DialogHelper.ConfirmarAsync(XamlRoot, "Quitar procedimiento", $"¿Eliminar {procedimiento.Tipo} del {procedimiento.FechaTexto}?"))
                {
                    ViewModel.EliminarProcedimiento(procedimiento);
                }
            });
        }
    }

    private void NuevaObservacion_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, () => EditarObservacionAsync(null));

    private void EditarObservacion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Observacion observacion })
        {
            UiSafe.Run(XamlRoot, () => EditarObservacionAsync(observacion));
        }
    }

    private void EliminarObservacion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Observacion observacion })
        {
            UiSafe.Run(XamlRoot, async () =>
            {
                if (await DialogHelper.ConfirmarAsync(XamlRoot, "Quitar observación", "¿Eliminar esta nota clínica?"))
                {
                    ViewModel.EliminarObservacion(observacion);
                }
            });
        }
    }

    private void NuevaCita_Click(object sender, RoutedEventArgs e) => UiSafe.Run(XamlRoot, () => EditarCitaAsync(null));

    private void EditarCita_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Cita cita })
        {
            UiSafe.Run(XamlRoot, () => EditarCitaAsync(cita));
        }
    }

    private void ComprobanteCita_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Cita cita })
        {
            UiSafe.Run(XamlRoot, () => ComprobantePanel.MostrarAsync(XamlRoot, cita));
        }
    }

    private void EliminarCita_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Cita cita })
        {
            UiSafe.Run(XamlRoot, async () =>
            {
                if (await DialogHelper.ConfirmarAsync(XamlRoot, "Quitar cita", $"¿Eliminar la cita del {cita.FechaTexto} a las {cita.HoraTexto}?"))
                {
                    ViewModel.EliminarCita(cita);
                }
            });
        }
    }

    private async Task EditarCitaAsync(Cita? existente)
    {
        var dialog = new CitaDialog { XamlRoot = XamlRoot };
        dialog.Cargar(existente, existente?.FechaInicio.Date, existente?.FechaInicio.TimeOfDay, ViewModel.Paciente.Id);
        var resultado = await dialog.ShowAsync();
        if (resultado == ContentDialogResult.Secondary && existente is not null)
        {
            if (await DialogHelper.ConfirmarAsync(XamlRoot, "Eliminar cita", $"¿Eliminar la cita del {existente.FechaTexto}?"))
            {
                ViewModel.EliminarCita(existente);
            }

            return;
        }

        if (resultado != ContentDialogResult.Primary)
        {
            return;
        }

        if (dialog.Cita.Estado is not "Cancelada" and not "No asistió" &&
            AppDatabase.Instance.HayMedicoOcupado(dialog.Cita))
        {
            await DialogHelper.AvisoAsync(
                XamlRoot,
                "Médico ocupado",
                $"{dialog.Cita.Medico} ya tiene una cita u operación en ese horario. Elija otra hora.");
            return;
        }

        if (dialog.Cita.Estado is not "Cancelada" and not "No asistió" &&
            AppDatabase.Instance.HayPacienteSolapado(dialog.Cita) &&
            !await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Horario ocupado",
                "El paciente ya tiene otra cita en ese horario. ¿Guardar de todos modos?",
                "Guardar"))
        {
            return;
        }

        if (!AppSettings.Current.EsDiaLaborable(dialog.Cita.FechaInicio.DateTime) &&
            !await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Día no laborable",
                "Ese día está marcado como no laborable en Ajustes. ¿Agendar de todos modos?",
                "Agendar"))
        {
            return;
        }

        ViewModel.GuardarCita(dialog.Cita);
    }

    private async Task EditarContactoAsync(ContactoEmergencia? existente)
    {
        var dialog = new ContactoDialog { XamlRoot = XamlRoot };
        dialog.Cargar(existente, ViewModel.Paciente.Id);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.GuardarContacto(dialog.Contacto);
        }
    }

    private async Task EditarProcedimientoAsync(Procedimiento? existente)
    {
        var dialog = new ProcedimientoDialog { XamlRoot = XamlRoot };
        dialog.Cargar(existente, ViewModel.Paciente.Id);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.GuardarProcedimiento(dialog.Procedimiento);
            if (existente is null && dialog.Procedimiento.Id > 0)
            {
                AppNavigation.Navigate(typeof(OperacionDetallePage), dialog.Procedimiento.Id);
            }
        }
    }

    private async Task EditarObservacionAsync(Observacion? existente)
    {
        var dialog = new ObservacionDialog { XamlRoot = XamlRoot };
        dialog.Cargar(existente, ViewModel.Paciente.Id);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.GuardarObservacion(dialog.Observacion);
        }
    }
}
