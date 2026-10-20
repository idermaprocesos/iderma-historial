using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Dialogs;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace IdermaCapilar_Historial.Pages;

public sealed partial class OperacionDetallePage : Page
{
    private Procedimiento _operacion = new();
    private bool _guardando;

    public OperacionDetallePage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        var id = e.Parameter is int valor ? valor : 0;
        UiSafe.Run(XamlRoot, () =>
        {
            Cargar(id);
            return Task.CompletedTask;
        }, "No se pudo abrir la operación");
    }

    private void Cargar(int id)
    {
        var cargada = id > 0 ? AppDatabase.Instance.GetProcedimiento(id) : null;
        if (cargada is null)
        {
            AppNavigation.GoBack();
            return;
        }

        _operacion = cargada;
        PintarEncabezado();
        ObsMedicoBox.Text = _operacion.Observaciones ?? string.Empty;
        MapaCabeza.Cargar(_operacion.ZonasOperadas);
        PintarChecklist();
        PintarSesiones();
    }

    private void PintarEncabezado()
    {
        TituloText.Text = string.IsNullOrWhiteSpace(_operacion.NombrePaciente)
            ? _operacion.Tipo
            : $"{_operacion.Tipo} · {_operacion.NombrePaciente}";
        SubtituloText.Text = _operacion.DetalleLista;
        DescripcionText.Text = _operacion.DescripcionProcedimiento;
    }

    private void PintarSesiones()
    {
        SesionesPanel.Children.Clear();
        var tramos = _operacion.Tramos();
        var puedeQuitar = tramos.Count > 1;
        foreach (var tramo in tramos)
        {
            var texto = new StackPanel { Spacing = 2 };
            texto.Children.Add(new TextBlock
            {
                Text = tramo.Titulo,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            var detalle = $"{tramo.RangoTexto}  ·  {tramo.DuracionTexto}";
            if (!tramo.EsPrincipal && !string.IsNullOrWhiteSpace(tramo.Estado))
            {
                detalle += $"  ·  {tramo.Estado}";
            }

            texto.Children.Add(new TextBlock
            {
                Text = detalle,
                Opacity = 0.78,
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrWhiteSpace(tramo.Notas))
            {
                texto.Children.Add(new TextBlock
                {
                    Text = tramo.Notas,
                    Opacity = 0.65,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            var acciones = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center
            };
            var cambiar = new Button { Content = "Cambiar horario", Tag = tramo };
            cambiar.Click += CambiarSesion_Click;
            acciones.Children.Add(cambiar);
            if (puedeQuitar)
            {
                var quitar = new Button { Content = "Quitar", Tag = tramo };
                quitar.Click += QuitarSesion_Click;
                acciones.Children.Add(quitar);
            }

            var fila = new Grid { ColumnSpacing = 12 };
            fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(acciones, 1);
            fila.Children.Add(texto);
            fila.Children.Add(acciones);

            SesionesPanel.Children.Add(new Border
            {
                Padding = new Thickness(10, 8, 10, 8),
                CornerRadius = new CornerRadius(8),
                Background = BrochaSuave(),
                Child = fila
            });
        }
    }

    private static Microsoft.UI.Xaml.Media.Brush BrochaSuave()
        => Application.Current.Resources.TryGetValue("CardBackgroundFillColorSecondaryBrush", out var valor)
           && valor is Microsoft.UI.Xaml.Media.Brush brocha
            ? brocha
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

    private void NuevaSesion_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, () => EditarSesionAsync(null));

    private void CambiarSesion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TramoOperacion tramo })
        {
            UiSafe.Run(XamlRoot, () => EditarSesionAsync(tramo));
        }
    }

    private void QuitarSesion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TramoOperacion tramo })
        {
            UiSafe.Run(XamlRoot, () => QuitarSesionAsync(tramo));
        }
    }

    private async Task EditarSesionAsync(TramoOperacion? tramo)
    {
        var dialog = new SesionDialog { XamlRoot = XamlRoot };
        if (tramo is null)
        {
            dialog.CargarNueva(_operacion);
        }
        else
        {
            dialog.Cargar(tramo, _operacion);
        }

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        if (!AppSettings.Current.EsDiaLaborable(dialog.Inicio.DateTime) &&
            !await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Día no laborable",
                "Ese día está marcado como no laborable en Ajustes. ¿Agendar de todos modos?",
                "Agendar"))
        {
            return;
        }

        if (tramo is null || !tramo.EsPrincipal)
        {
            var sesion = tramo is null
                ? new SesionOperacion { ProcedimientoId = _operacion.Id }
                : _operacion.Sesiones.First(s => s.Id == tramo.SesionId);
            sesion.FechaInicio = dialog.Inicio;
            sesion.DuracionMinutos = dialog.DuracionMinutos;
            sesion.Estado = dialog.Estado;
            sesion.Notas = dialog.Notas;
            AppDatabase.Instance.GuardarSesion(sesion);
        }
        else
        {
            _operacion.Fecha = dialog.Inicio;
            _operacion.DuracionMinutos = dialog.DuracionMinutos;
            AppDatabase.Instance.GuardarProcedimiento(_operacion);
        }

        Cargar(_operacion.Id);
    }

    private async Task QuitarSesionAsync(TramoOperacion tramo)
    {
        if (_operacion.Tramos().Count <= 1)
        {
            return;
        }

        if (!await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Quitar sesión",
                $"Se quitará {tramo.Titulo} ({tramo.RangoTexto}) de esta operación.",
                "Quitar"))
        {
            return;
        }

        if (tramo.EsPrincipal)
        {
            var reemplazo = _operacion.Sesiones
                .OrderBy(s => s.Estado is "Cancelada" ? 1 : 0)
                .ThenBy(s => s.FechaInicio)
                .First();
            _operacion.Fecha = reemplazo.FechaInicio;
            _operacion.DuracionMinutos = reemplazo.DuracionMinutos;
            AppDatabase.Instance.GuardarProcedimiento(_operacion);
            AppDatabase.Instance.EliminarSesion(reemplazo.Id);
        }
        else
        {
            AppDatabase.Instance.EliminarSesion(tramo.SesionId);
        }

        Cargar(_operacion.Id);
    }

    private void PintarChecklist()
    {
        ChecklistPanel.Children.Clear();
        var hechos = ChecklistOperacion.IdsHechos(_operacion.ChecklistPasos);
        string? fase = null;
        foreach (var paso in ChecklistOperacion.ParaTipo(_operacion.Tipo))
        {
            if (paso.Fase != fase)
            {
                fase = paso.Fase;
                ChecklistPanel.Children.Add(new TextBlock
                {
                    Text = fase,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 10, 0, 4),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            var contenido = new StackPanel { Spacing = 2 };
            contenido.Children.Add(new TextBlock
            {
                Text = paso.Titulo,
                TextWrapping = TextWrapping.Wrap
            });
            contenido.Children.Add(new TextBlock
            {
                Text = paso.Instruccion,
                Opacity = 0.72,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });

            var caja = new CheckBox
            {
                Content = contenido,
                Tag = paso.Id,
                IsChecked = hechos.Contains(paso.Id),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            caja.Checked += Checklist_Changed;
            caja.Unchecked += Checklist_Changed;
            ChecklistPanel.Children.Add(caja);
        }

        ActualizarResumen();
    }

    private void Checklist_Changed(object sender, RoutedEventArgs e)
    {
        if (_guardando)
        {
            return;
        }

        GuardarChecklist();
    }

    private void MarcarTodos_Click(object sender, RoutedEventArgs e) => MarcarTodos(true);

    private void DesmarcarTodos_Click(object sender, RoutedEventArgs e) => MarcarTodos(false);

    private void MarcarTodos(bool valor)
    {
        _guardando = true;
        foreach (var caja in Casillas())
        {
            caja.IsChecked = valor;
        }

        _guardando = false;
        GuardarChecklist();
    }

    private void GuardarChecklist()
    {
        _operacion.ChecklistPasos = ChecklistOperacion.Serializar(
            Casillas().Where(c => c.IsChecked == true).Select(c => c.Tag as string ?? string.Empty));
        AppDatabase.Instance.GuardarProcedimiento(_operacion);
        ActualizarResumen();
        PintarEncabezado();
    }

    private void ActualizarResumen()
    {
        var total = Casillas().Count();
        var hechos = Casillas().Count(c => c.IsChecked == true);
        var completa = total > 0 && hechos == total;
        var realizada = string.Equals(_operacion.Estado, "Realizado", StringComparison.OrdinalIgnoreCase);

        ChecklistResumenText.Text = total == 0
            ? "No hay pasos para este tipo."
            : completa
                ? $"{hechos} de {total} pasos. Ya puede completar la operación."
                : $"{hechos} de {total} pasos. Puede completar igual; se pedirá el motivo de lo que falte.";

        CompletarButton.Content = realizada ? "Operación completada" : "Completar operación";
        CompletarButton.IsEnabled = !realizada;
    }

    private IEnumerable<CheckBox> Casillas() => ChecklistPanel.Children.OfType<CheckBox>();

    private void Mapa_ZonasChanged(object? sender, EventArgs e)
    {
        _operacion.ZonasOperadas = MapaCabeza.Valor;
        if (!string.IsNullOrWhiteSpace(MapaCabeza.Nombres))
        {
            _operacion.AreaTratada = MapaCabeza.Nombres;
        }

        AppDatabase.Instance.GuardarProcedimiento(_operacion);
        PintarEncabezado();
    }

    private void ObsMedicoBox_LostFocus(object sender, RoutedEventArgs e)
        => GuardarObservacionesMedico();

    private void GuardarObservacionesMedico()
    {
        var texto = ObsMedicoBox.Text?.Trim();
        _operacion.Observaciones = string.IsNullOrWhiteSpace(texto) ? null : texto;
        AppDatabase.Instance.GuardarProcedimiento(_operacion);
        PintarEncabezado();
    }

    private void Completar_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, CompletarAsync);

    private async Task CompletarAsync()
    {
        GuardarObservacionesMedico();
        if (string.Equals(_operacion.Estado, "Realizado", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var total = Casillas().Count();
        var hechos = Casillas().Count(c => c.IsChecked == true);
        if (total > 0 && hechos < total)
        {
            var motivo = await DialogHelper.PedirTextoAsync(
                XamlRoot,
                "Checklist incompleto",
                $"Hay {hechos} de {total} pasos marcados. Explique por qué se completa la operación sin el resto.",
                "Completar",
                "Motivo (obligatorio)...");
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return;
            }

            var nota = $"Checklist incompleto ({hechos}/{total}): {motivo}";
            _operacion.Observaciones = string.IsNullOrWhiteSpace(_operacion.Observaciones)
                ? nota
                : _operacion.Observaciones.Trim() + Environment.NewLine + Environment.NewLine + nota;
            ObsMedicoBox.Text = _operacion.Observaciones;
        }
        else if (!await DialogHelper.ConfirmarAsync(
                     XamlRoot,
                     "Completar operación",
                     "Se marcará como realizada. ¿Continuar?",
                     "Completar"))
        {
            return;
        }

        _operacion.Estado = "Realizado";
        AppDatabase.Instance.GuardarProcedimiento(_operacion);
        PintarEncabezado();
        ActualizarResumen();
    }

    private void Editar_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, EditarAsync);

    private async Task EditarAsync()
    {
        var dialog = new ProcedimientoDialog { XamlRoot = XamlRoot };
        dialog.Cargar(_operacion, _operacion.PacienteId);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        dialog.Procedimiento.ChecklistPasos = _operacion.ChecklistPasos;
        dialog.Procedimiento.ZonasOperadas = _operacion.ZonasOperadas;
        AppDatabase.Instance.GuardarProcedimiento(dialog.Procedimiento);
        Cargar(dialog.Procedimiento.Id);
    }

    private void Expediente_Click(object sender, RoutedEventArgs e)
        => AppNavigation.Navigate(typeof(PacienteDetallePage), new PacienteDetalleNavegacion
        {
            PacienteId = _operacion.PacienteId,
            Pestaña = "Operaciones"
        });
}
