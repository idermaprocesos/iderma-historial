using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Dialogs;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using IdermaCapilar_Historial.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.UI;

namespace IdermaCapilar_Historial.Pages;

public sealed partial class CitasPage : Page
{
    public CitasViewModel ViewModel { get; } = new();
    public IReadOnlyList<TipoCitaOpcion> LeyendaCitas => Catalogos.OpcionesTipoCita;
    public IReadOnlyList<TipoCitaOpcion> LeyendaOperaciones =>
        Catalogos.OpcionesTipoProcedimiento
            .Select(t => new TipoCitaOpcion($"Op. {t.Nombre}", t.R, t.G, t.B))
            .ToList();

    private bool _cargandoVista;
    private bool _arrastrando;
    private bool _finalizandoArrastre;
    private bool _punteroCapturado;
    private int _colInicio;
    private int _filaInicio;
    private int _colActual;
    private int _filaActual;
    private bool _reubicando;
    private bool _finalizandoReubicacion;
    private bool _redimensionando;
    private bool _finalizandoRedimension;
    private EventoCalendario? _eventoReubicacion;
    private Button? _tarjetaReubicacion;
    private int _colOrigenCita;
    private int _filaOrigenCita;
    private int _colDestinoCita;
    private int _filaDestinoCita;
    private int _spanCita;
    private int _spanOrigenCita;
    private Point _origenPunteroCita;
    private bool _desplazoCita;
    private bool CalendarioBloqueado =>
        _arrastrando || _finalizandoArrastre || _reubicando || _finalizandoReubicacion
        || _redimensionando || _finalizandoRedimension;
    private List<TimeSpan> _horas = [];
    private Border[,] _celdas = new Border[0, 0];

    public CitasPage()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ViewModel.TituloPeriodo) or nameof(ViewModel.EsVistaSemana) or nameof(ViewModel.EsVistaMes))
            {
                if (!CalendarioBloqueado)
                {
                    ActualizarCalendario();
                }
            }
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
        => UiSafe.Run(XamlRoot, () =>
        {
            ViewModel.Inicializar();
            switch (e.Parameter)
            {
                case CitasNavegacion destino:
                    if (!string.IsNullOrWhiteSpace(destino.Vista))
                    {
                        ViewModel.Vista = destino.Vista;
                    }

                    ViewModel.IrA(destino.Fecha ?? DateTime.Today);
                    break;
                case DateTime fecha:
                    ViewModel.IrA(fecha);
                    break;
            }

            SeleccionarVista(ViewModel.Vista);
            ActualizarCalendario();
            return Task.CompletedTask;
        }, "No se pudo abrir la agenda");

    private void VistaBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (_cargandoVista || sender.SelectedItem?.Tag is not string vista)
        {
            return;
        }

        ViewModel.CambiarVista(vista);
        ActualizarCalendario();
    }

    private void Anterior_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Anterior();
        ActualizarCalendario();
    }

    private void Siguiente_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Siguiente();
        ActualizarCalendario();
    }

    private void Hoy_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IrHoy();
        ActualizarCalendario();
    }

    private void Nueva_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, () => EditarAsync(null, ViewModel.FechaReferencia, AppSettings.Current.HoraInicioAgenda));

    private void ImprimirSemana_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, () => ImpresionService.ImprimirCitasSemanaAsync(ViewModel.FechaReferencia), "No se pudo imprimir");

    private void IrADia(CalendarioDiaCelda celda)
    {
        ViewModel.IrA(celda.Fecha);
        ViewModel.CambiarVista("Día");
        SeleccionarVista("Día");
        ActualizarCalendario();
    }

    private void Slot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CalendarioSlot slot })
        {
            UiSafe.Run(XamlRoot, () => EditarAsync(null, slot.FechaHora.Date, slot.Hora));
        }
    }

    private void Cita_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: EventoCalendario evento })
        {
            UiSafe.Run(XamlRoot, () => AbrirEventoAsync(evento));
        }
        else if (sender is Button { Tag: Cita cita })
        {
            UiSafe.Run(XamlRoot, () => EditarAsync(cita));
        }
    }

    private void CitaLista_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is EventoCalendario evento)
        {
            UiSafe.Run(XamlRoot, () => AbrirEventoAsync(evento));
        }
        else if (e.ClickedItem is Cita cita)
        {
            UiSafe.Run(XamlRoot, () => EditarAsync(cita));
        }
    }

    private void Comprobante_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: EventoCalendario { Cita: { } cita } })
        {
            UiSafe.Run(XamlRoot, () => ComprobantePanel.MostrarAsync(XamlRoot, cita));
        }
    }

    private void Expediente_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: EventoCalendario evento })
        {
            AppNavigation.Navigate(typeof(PacienteDetallePage), evento.PacienteId);
        }
        else if (sender is Button { Tag: Cita cita })
        {
            AppNavigation.Navigate(typeof(PacienteDetallePage), cita.PacienteId);
        }
    }

    private void SemanaScroll_SizeChanged(object sender, SizeChangedEventArgs e) => AjustarAltoSemana();

    private void ActualizarCalendario()
    {
        if (CalendarioBloqueado)
        {
            return;
        }

        ActualizarGrillaSemana();
        ActualizarGrillaMes();
    }

    private void ActualizarGrillaSemana()
    {
        if (!ViewModel.EsVistaSemana)
        {
            return;
        }

        ConstruirGrillaSemana();
        AjustarAltoSemana();
    }

    private void ActualizarGrillaMes()
    {
        if (!ViewModel.EsVistaMes)
        {
            return;
        }

        ConstruirGrillaMes();
    }

    private void ConstruirGrillaMes()
    {
        var dias = ViewModel.DiasMes.ToList();
        if (dias.Count == 0)
        {
            return;
        }

        var filas = dias.Count / 7;
        var nombres = ViewModel.NombresDias.Count > 0
            ? ViewModel.NombresDias.ToList()
            : ["Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom"];
        var fondo = BrochaTema("CardBackgroundFillColorDefaultBrush");
        var fondoHoy = BrochaTema("CardBackgroundFillColorSecondaryBrush");
        var trazo = BrochaTema("CardStrokeColorDefaultBrush");
        var acento = BrochaTema("AccentFillColorDefaultBrush");
        var estiloCita = Application.Current.Resources["CitaTarjetaButtonStyle"] as Style;

        MesEncabezados.Children.Clear();
        MesEncabezados.ColumnDefinitions.Clear();
        MesCuerpo.Children.Clear();
        MesCuerpo.ColumnDefinitions.Clear();
        MesCuerpo.RowDefinitions.Clear();

        for (var i = 0; i < 7; i++)
        {
            MesEncabezados.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            MesCuerpo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var encabezado = new TextBlock
            {
                Text = nombres[i],
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetColumn(encabezado, i);
            MesEncabezados.Children.Add(encabezado);
        }

        for (var f = 0; f < filas; f++)
        {
            MesCuerpo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        for (var i = 0; i < dias.Count; i++)
        {
            var celda = dias[i];
            var marco = new Border
            {
                Margin = new Thickness(3),
                Padding = new Thickness(8, 6, 8, 6),
                CornerRadius = new CornerRadius(10),
                Opacity = celda.OpacidadMes,
                Background = celda.EsHoy ? fondoHoy : fondo,
                BorderBrush = celda.EsHoy ? acento : trazo,
                BorderThickness = new Thickness(celda.EsHoy ? 2 : 1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Tag = celda
            };
            marco.Tapped += MesCelda_Tapped;

            var interno = new Grid { ColumnSpacing = 6 };
            interno.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            interno.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var numero = new TextBlock
            {
                Text = celda.Numero,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(2, 2, 4, 0)
            };
            if (celda.EsHoy)
            {
                numero.Foreground = BrochaTema("AccentTextFillColorPrimaryBrush");
            }

            var citas = new StackPanel { Spacing = 3 };
            foreach (var cita in celda.CitasVisibles)
            {
                var tarjeta = new Button
                {
                    Background = cita.ColorEstado,
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 2, 6, 2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Foreground = new SolidColorBrush(Colors.White),
                    Tag = cita,
                    Style = estiloCita,
                    Content = new TextBlock
                    {
                        FontSize = 11,
                        Text = cita.ChipTexto,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Foreground = new SolidColorBrush(Colors.White)
                    }
                };
                tarjeta.Click += Cita_Click;
                tarjeta.Tapped += (_, e) => e.Handled = true;
                citas.Children.Add(tarjeta);
            }

            if (celda.HayMas)
            {
                var mas = new TextBlock
                {
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Opacity = 0.75,
                    Text = celda.TextoMas,
                    Margin = new Thickness(2, 2, 0, 0)
                };
                citas.Children.Add(mas);
            }

            Grid.SetColumn(citas, 1);
            interno.Children.Add(numero);
            interno.Children.Add(citas);
            marco.Child = interno;

            Grid.SetRow(marco, i / 7);
            Grid.SetColumn(marco, i % 7);
            MesCuerpo.Children.Add(marco);
        }
    }

    private void MesCelda_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border { Tag: CalendarioDiaCelda celda })
        {
            IrADia(celda);
        }
    }

    private void AjustarAltoSemana()
    {
        if (SemanaScroll.ActualHeight <= 0 || _horas.Count == 0)
        {
            return;
        }

        SemanaCuerpo.MinHeight = Math.Max(SemanaScroll.ActualHeight, _horas.Count * 36);
    }

    private void ConstruirGrillaSemana()
    {
        var horas = AppSettings.Current.HorariosCita().ToList();
        _horas = horas;
        var dias = ViewModel.Semana.ToList();
        if (dias.Count == 0 || horas.Count == 0)
        {
            return;
        }

        var intervalo = Math.Max(5, AppSettings.Current.CitasIntervaloMinutos);
        var fondo = BrochaTema("CardBackgroundFillColorDefaultBrush");
        var fondoHoy = BrochaTema("CardBackgroundFillColorSecondaryBrush");
        var trazo = BrochaTema("CardStrokeColorDefaultBrush");

        SemanaEncabezados.Children.Clear();
        SemanaEncabezados.ColumnDefinitions.Clear();
        SemanaEncabezados.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        for (var i = 0; i < 7; i++)
        {
            SemanaEncabezados.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var d = 0; d < dias.Count; d++)
        {
            var texto = new TextBlock
            {
                Text = dias[d].Encabezado,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 8)
            };
            if (dias[d].EsHoy)
            {
                texto.Foreground = BrochaTema("AccentTextFillColorPrimaryBrush");
            }

            Grid.SetColumn(texto, d + 1);
            SemanaEncabezados.Children.Add(texto);
        }

        SemanaCuerpo.Children.Clear();
        SemanaCuerpo.RowDefinitions.Clear();
        SemanaCuerpo.ColumnDefinitions.Clear();
        SemanaCuerpo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        for (var i = 0; i < 7; i++)
        {
            SemanaCuerpo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        foreach (var _ in horas)
        {
            SemanaCuerpo.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(1, GridUnitType.Star),
                MinHeight = 36
            });
        }

        _celdas = new Border[horas.Count, 7];

        for (var f = 0; f < horas.Count; f++)
        {
            var etiqueta = new TextBlock
            {
                Text = DateTime.Today.Add(horas[f]).ToString("HH:mm"),
                FontSize = 11,
                Opacity = 0.65,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 6, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(etiqueta, f);
            Grid.SetColumn(etiqueta, 0);
            SemanaCuerpo.Children.Add(etiqueta);

            for (var d = 0; d < 7; d++)
            {
                var celda = new Border
                {
                    Background = dias[d].EsHoy ? fondoHoy : fondo,
                    BorderBrush = trazo,
                    BorderThickness = new Thickness(0.5),
                    Tag = new PuntoSemana(d, f, dias[d].Fecha, horas[f])
                };
                celda.PointerPressed += CeldaSemana_Pressed;
                celda.PointerMoved += CeldaSemana_Moved;
                celda.PointerReleased += CeldaSemana_Released;
                celda.PointerCaptureLost += CeldaSemana_CaptureLost;
                Grid.SetRow(celda, f);
                Grid.SetColumn(celda, d + 1);
                SemanaCuerpo.Children.Add(celda);
                _celdas[f, d] = celda;
            }
        }

        foreach (var dia in dias.Select((col, indice) => (col, indice)))
        {
            foreach (var evento in dia.col.Eventos)
            {
                var hora = evento.FechaInicio.TimeOfDay == TimeSpan.Zero ? horas[0] : evento.FechaInicio.TimeOfDay;
                var fila = FilaDeHora(hora, horas, intervalo);
                if (fila < 0)
                {
                    continue;
                }

                var span = Math.Max(1, (int)Math.Ceiling(evento.DuracionMinutos / (double)intervalo));
                span = Math.Min(span, horas.Count - fila);

                var panel = new StackPanel();
                panel.Children.Add(new TextBlock
                {
                    Text = evento.HorarioTexto,
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Colors.White)
                });
                panel.Children.Add(new TextBlock
                {
                    Text = evento.TituloCorto,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.Wrap
                });
                panel.Children.Add(new TextBlock
                {
                    Text = evento.EsSesion ? $"Sesión · {evento.Tipo}" : evento.EsOperacion ? $"Op. {evento.Tipo}" : evento.Tipo,
                    FontSize = 11,
                    Opacity = 0.92,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });

                var cuerpo = new Grid();
                cuerpo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                cuerpo.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                cuerpo.Children.Add(panel);

                var tarjeta = new Button
                {
                    Background = evento.ColorEstado,
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(2),
                    Padding = new Thickness(6, 4, 6, 2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    Tag = evento,
                    Content = cuerpo,
                    ManipulationMode = ManipulationModes.None,
                    Style = Application.Current.Resources["CitaTarjetaButtonStyle"] as Style
                };
                if (evento.Cita is not null || evento.Procedimiento is not null)
                {
                    var asa = new Border
                    {
                        Height = 14,
                        Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
                        CornerRadius = new CornerRadius(0, 0, 4, 4),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Tag = evento,
                        Child = new TextBlock
                        {
                            Text = "\uE76F",
                            FontFamily = new FontFamily("Segoe MDL2 Assets"),
                            FontSize = 10,
                            Opacity = 0.95,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = new SolidColorBrush(Colors.White)
                        }
                    };
                    asa.PointerPressed += DuracionSemana_Pressed;
                    asa.PointerMoved += DuracionSemana_Moved;
                    asa.PointerReleased += DuracionSemana_Released;
                    asa.PointerCaptureLost += DuracionSemana_CaptureLost;
                    ToolTipService.SetToolTip(asa, "Arrastre en vertical para cambiar la duración");
                    Grid.SetRow(asa, 1);
                    cuerpo.Children.Add(asa);

                    tarjeta.PointerPressed += CitaSemana_Pressed;
                    tarjeta.PointerMoved += CitaSemana_Moved;
                    tarjeta.PointerReleased += CitaSemana_Released;
                    tarjeta.PointerCaptureLost += CitaSemana_CaptureLost;
                    ToolTipService.SetToolTip(tarjeta, evento.EsOperacion
                        ? "Arrastre para mover. El borde inferior cambia la duración. Pulse para abrir."
                        : "Arrastre para mover. El borde inferior cambia la duración. Pulse para editar.");
                }
                else
                {
                    tarjeta.Click += Cita_Click;
                }

                tarjeta.PointerPressed += (_, e) => e.Handled = true;
                Grid.SetColumn(tarjeta, dia.indice + 1);
                Grid.SetRow(tarjeta, fila);
                Grid.SetRowSpan(tarjeta, span);
                Canvas.SetZIndex(tarjeta, 2);
                SemanaCuerpo.Children.Add(tarjeta);
            }
        }
    }

    private void CitaSemana_Pressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Button { Tag: EventoCalendario evento } tarjeta ||
            (evento.Cita is null && evento.Procedimiento is null) ||
            _arrastrando ||
            _reubicando ||
            _redimensionando)
        {
            return;
        }

        _eventoReubicacion = evento;
        _tarjetaReubicacion = tarjeta;
        _reubicando = true;
        _desplazoCita = false;
        _origenPunteroCita = e.GetCurrentPoint(SemanaCuerpo).Position;
        _colOrigenCita = _colDestinoCita = Grid.GetColumn(tarjeta) - 1;
        _filaOrigenCita = _filaDestinoCita = Grid.GetRow(tarjeta);
        _spanCita = _spanOrigenCita = Math.Max(1, Grid.GetRowSpan(tarjeta));
        tarjeta.CapturePointer(e.Pointer);
        _punteroCapturado = true;
        e.Handled = true;
    }

    private void CitaSemana_Moved(object sender, PointerRoutedEventArgs e)
    {
        if (!_reubicando || _redimensionando || _eventoReubicacion is null)
        {
            return;
        }

        var posicion = e.GetCurrentPoint(SemanaCuerpo).Position;
        if (!_desplazoCita)
        {
            var dx = posicion.X - _origenPunteroCita.X;
            var dy = posicion.Y - _origenPunteroCita.Y;
            if ((dx * dx) + (dy * dy) < 64)
            {
                return;
            }

            _desplazoCita = true;
        }

        if (CeldaBajoPuntero(posicion) is not { } punto)
        {
            return;
        }

        var filaMax = Math.Max(0, _horas.Count - _spanCita);
        _colDestinoCita = punto.Columna;
        _filaDestinoCita = Math.Clamp(punto.Fila, 0, filaMax);
        if (_tarjetaReubicacion is not null)
        {
            _tarjetaReubicacion.Opacity = 0.4;
        }

        PintarDestinoCita();
    }

    private void CitaSemana_Released(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Button boton && _punteroCapturado)
        {
            boton.ReleasePointerCapture(e.Pointer);
        }

        UiSafe.Run(XamlRoot, FinalizarReubicacionAsync);
    }

    private void CitaSemana_CaptureLost(object sender, PointerRoutedEventArgs e)
        => UiSafe.Run(XamlRoot, FinalizarReubicacionAsync);

    private async Task FinalizarReubicacionAsync()
    {
        if (!_reubicando || _finalizandoReubicacion)
        {
            return;
        }

        _finalizandoReubicacion = true;
        _reubicando = false;
        _punteroCapturado = false;
        var evento = _eventoReubicacion;
        var arrastro = _desplazoCita;
        var colDestino = _colDestinoCita;
        var filaDestino = _filaDestinoCita;
        var mismaPosicion = colDestino == _colOrigenCita && filaDestino == _filaOrigenCita;
        LimpiarSeleccion();
        if (_tarjetaReubicacion is not null)
        {
            _tarjetaReubicacion.Opacity = 1;
        }

        _eventoReubicacion = null;
        _tarjetaReubicacion = null;
        _desplazoCita = false;
        _finalizandoReubicacion = false;

        if (evento is null)
        {
            return;
        }

        if (!arrastro || mismaPosicion)
        {
            await AbrirEventoAsync(evento);
            return;
        }

        if (colDestino < 0 || colDestino >= ViewModel.Semana.Count ||
            filaDestino < 0 || filaDestino >= _horas.Count)
        {
            return;
        }

        var fecha = ViewModel.Semana[colDestino].Fecha;
        var hora = _horas[filaDestino];
        if (evento.EsOperacion && evento.EsSesion)
        {
            await MoverSesionAsync(evento, fecha, hora);
            return;
        }

        if (evento.EsOperacion && evento.Procedimiento is not null)
        {
            await MoverOperacionAsync(evento.Procedimiento, fecha, hora);
            return;
        }

        if (evento.Cita is not null)
        {
            await MoverCitaAsync(evento.Cita, fecha, hora);
        }
    }

    private async Task MoverCitaAsync(Cita original, DateTime fecha, TimeSpan hora)
    {
        var movida = CopiarCita(original);
        movida.FechaInicio = FechaUi.Combinar(fecha, hora);
        if (!await ConfirmarYGuardarCitaAsync(movida))
        {
            return;
        }

        ViewModel.Recargar();
        ActualizarCalendario();
    }

    private async Task MoverOperacionAsync(Procedimiento original, DateTime fecha, TimeSpan hora)
    {
        var movida = CopiarProcedimiento(original);
        movida.Fecha = FechaUi.Combinar(fecha, hora);
        if (!await ConfirmarYGuardarOperacionAsync(movida))
        {
            return;
        }

        ViewModel.Recargar();
        ActualizarCalendario();
    }

    private async Task MoverSesionAsync(EventoCalendario evento, DateTime fecha, TimeSpan hora)
    {
        if (!await ReprogramarSesionAsync(evento, FechaUi.Combinar(fecha, hora), Math.Max(5, evento.DuracionMinutos)))
        {
            return;
        }

        ViewModel.Recargar();
        ActualizarCalendario();
    }

    private void DuracionSemana_Pressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border { Tag: EventoCalendario evento } asa ||
            (evento.Cita is null && evento.Procedimiento is null) ||
            _arrastrando ||
            _reubicando ||
            _redimensionando)
        {
            return;
        }

        var tarjeta = BuscarPadre<Button>(asa);
        if (tarjeta is null)
        {
            return;
        }

        _eventoReubicacion = evento;
        _tarjetaReubicacion = tarjeta;
        _redimensionando = true;
        _desplazoCita = false;
        _origenPunteroCita = e.GetCurrentPoint(SemanaCuerpo).Position;
        _colOrigenCita = _colDestinoCita = Grid.GetColumn(tarjeta) - 1;
        _filaOrigenCita = _filaDestinoCita = Grid.GetRow(tarjeta);
        _spanCita = _spanOrigenCita = Math.Max(1, Grid.GetRowSpan(tarjeta));
        asa.CapturePointer(e.Pointer);
        _punteroCapturado = true;
        e.Handled = true;
    }

    private void DuracionSemana_Moved(object sender, PointerRoutedEventArgs e)
    {
        if (!_redimensionando || _eventoReubicacion is null)
        {
            return;
        }

        var posicion = e.GetCurrentPoint(SemanaCuerpo).Position;
        if (!_desplazoCita)
        {
            var dy = posicion.Y - _origenPunteroCita.Y;
            if (Math.Abs(dy) < 8)
            {
                return;
            }

            _desplazoCita = true;
        }

        if (CeldaBajoPuntero(posicion) is not { } punto)
        {
            return;
        }

        var filaFin = Math.Clamp(punto.Fila, _filaOrigenCita, _horas.Count - 1);
        _spanCita = Math.Max(1, filaFin - _filaOrigenCita + 1);
        _colDestinoCita = _colOrigenCita;
        _filaDestinoCita = _filaOrigenCita;
        if (_tarjetaReubicacion is not null)
        {
            Grid.SetRowSpan(_tarjetaReubicacion, _spanCita);
        }

        PintarDestinoCita();
    }

    private void DuracionSemana_Released(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border borde && _punteroCapturado)
        {
            borde.ReleasePointerCapture(e.Pointer);
        }

        UiSafe.Run(XamlRoot, FinalizarRedimensionAsync);
    }

    private void DuracionSemana_CaptureLost(object sender, PointerRoutedEventArgs e)
        => UiSafe.Run(XamlRoot, FinalizarRedimensionAsync);

    private async Task FinalizarRedimensionAsync()
    {
        if (!_redimensionando || _finalizandoRedimension)
        {
            return;
        }

        _finalizandoRedimension = true;
        _redimensionando = false;
        _punteroCapturado = false;
        var evento = _eventoReubicacion;
        var arrastro = _desplazoCita;
        var span = _spanCita;
        var spanOrigen = _spanOrigenCita;
        LimpiarSeleccion();
        _eventoReubicacion = null;
        _tarjetaReubicacion = null;
        _desplazoCita = false;
        _finalizandoRedimension = false;

        if (evento is null || !arrastro || span == spanOrigen)
        {
            if (span != spanOrigen)
            {
                ActualizarCalendario();
            }

            return;
        }

        var intervalo = Math.Max(5, AppSettings.Current.CitasIntervaloMinutos);
        var duracion = Math.Max(intervalo, span * intervalo);
        if (!await GuardarDuracionAsync(evento, duracion))
        {
            ActualizarCalendario();
            return;
        }

        ViewModel.Recargar();
        ActualizarCalendario();
    }

    private async Task<bool> GuardarDuracionAsync(EventoCalendario evento, int duracionMinutos)
    {
        if (evento.EsOperacion && evento.EsSesion)
        {
            return await ReprogramarSesionAsync(evento, evento.FechaInicio, duracionMinutos);
        }

        if (evento.EsOperacion && evento.Procedimiento is not null)
        {
            var movida = CopiarProcedimiento(evento.Procedimiento);
            movida.DuracionMinutos = duracionMinutos;
            return await ConfirmarYGuardarOperacionAsync(movida);
        }

        if (evento.Cita is not null)
        {
            var movida = CopiarCita(evento.Cita);
            movida.DuracionMinutos = duracionMinutos;
            return await ConfirmarYGuardarCitaAsync(movida);
        }

        return false;
    }

    private static T? BuscarPadre<T>(DependencyObject hijo) where T : DependencyObject
    {
        for (var actual = VisualTreeHelper.GetParent(hijo); actual is not null; actual = VisualTreeHelper.GetParent(actual))
        {
            if (actual is T encontrado)
            {
                return encontrado;
            }
        }

        return null;
    }

    private static Cita CopiarCita(Cita origen) => new()
    {
        Id = origen.Id,
        PacienteId = origen.PacienteId,
        FechaInicio = origen.FechaInicio,
        DuracionMinutos = origen.DuracionMinutos,
        Tipo = origen.Tipo,
        Estado = origen.Estado,
        Medico = origen.Medico,
        Notas = origen.Notas,
        Temperamento = origen.Temperamento,
        NombrePaciente = origen.NombrePaciente,
        NumeroExpediente = origen.NumeroExpediente,
        TelefonoPaciente = origen.TelefonoPaciente
    };

    private static Procedimiento CopiarProcedimiento(Procedimiento origen) => new()
    {
        Id = origen.Id,
        PacienteId = origen.PacienteId,
        Fecha = origen.Fecha,
        Tipo = origen.Tipo,
        AreaTratada = origen.AreaTratada,
        UnidadesFoliculares = origen.UnidadesFoliculares,
        NumeroInjertos = origen.NumeroInjertos,
        Medico = origen.Medico,
        Enfermera = origen.Enfermera,
        Anestesia = origen.Anestesia,
        DuracionMinutos = origen.DuracionMinutos,
        Estado = origen.Estado,
        Resultado = origen.Resultado,
        Complicaciones = origen.Complicaciones,
        Observaciones = origen.Observaciones,
        ChecklistPasos = origen.ChecklistPasos,
        ZonasOperadas = origen.ZonasOperadas,
        Costo = origen.Costo,
        NombrePaciente = origen.NombrePaciente
    };

    private void PintarDestinoCita()
    {
        LimpiarSeleccion();
        if (_celdas.Length == 0 || _colDestinoCita < 0 || _colDestinoCita >= _celdas.GetLength(1))
        {
            return;
        }

        var marca = new SolidColorBrush(Color.FromArgb(90, 14, 107, 107));
        var filaMax = Math.Min(_celdas.GetLength(0) - 1, _filaDestinoCita + _spanCita - 1);
        for (var f = Math.Max(0, _filaDestinoCita); f <= filaMax; f++)
        {
            var celda = _celdas[f, _colDestinoCita];
            if (celda is not null)
            {
                celda.Background = marca;
            }
        }
    }

    private void CeldaSemana_Pressed(object sender, PointerRoutedEventArgs e)
    {
        if (_reubicando || _redimensionando || sender is not Border { Tag: PuntoSemana punto } borde)
        {
            return;
        }

        _arrastrando = true;
        _colInicio = _colActual = punto.Columna;
        _filaInicio = _filaActual = punto.Fila;
        borde.CapturePointer(e.Pointer);
        _punteroCapturado = true;
        PintarSeleccion();
        e.Handled = true;
    }

    private void CeldaSemana_Moved(object sender, PointerRoutedEventArgs e)
    {
        if (!_arrastrando)
        {
            return;
        }

        if (CeldaBajoPuntero(e.GetCurrentPoint(SemanaCuerpo).Position) is { } punto && punto.Columna == _colInicio)
        {
            _filaActual = punto.Fila;
            PintarSeleccion();
        }
    }

    private void CeldaSemana_Released(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border borde && _punteroCapturado)
        {
            borde.ReleasePointerCapture(e.Pointer);
        }

        UiSafe.Run(XamlRoot, FinalizarArrastreAsync);
    }

    private void CeldaSemana_CaptureLost(object sender, PointerRoutedEventArgs e)
        => UiSafe.Run(XamlRoot, FinalizarArrastreAsync);

    private async Task FinalizarArrastreAsync()
    {
        if (!_arrastrando || _finalizandoArrastre)
        {
            return;
        }

        _finalizandoArrastre = true;
        _arrastrando = false;
        _punteroCapturado = false;
        var filaMin = Math.Min(_filaInicio, _filaActual);
        var filaMax = Math.Max(_filaInicio, _filaActual);
        LimpiarSeleccion();

        try
        {
            if (_colInicio < 0 || _colInicio >= ViewModel.Semana.Count || filaMin < 0 || filaMin >= _horas.Count)
            {
                return;
            }

            var fecha = ViewModel.Semana[_colInicio].Fecha;
            var hora = _horas[filaMin];
            var intervalo = Math.Max(5, AppSettings.Current.CitasIntervaloMinutos);
            var duracion = Math.Max(intervalo, (filaMax - filaMin + 1) * intervalo);
            await EditarAsync(null, fecha, hora, duracion);
        }
        finally
        {
            _finalizandoArrastre = false;
        }
    }

    private void PintarSeleccion()
    {
        LimpiarSeleccion();
        if (_colInicio < 0 || _celdas.Length == 0)
        {
            return;
        }

        var filaMin = Math.Min(_filaInicio, _filaActual);
        var filaMax = Math.Max(_filaInicio, _filaActual);
        if (_colInicio < 0 || _colInicio >= _celdas.GetLength(1))
        {
            return;
        }

        var marca = new SolidColorBrush(Color.FromArgb(70, 14, 107, 107));
        for (var f = Math.Max(0, filaMin); f <= filaMax && f < _celdas.GetLength(0); f++)
        {
            var celda = _celdas[f, _colInicio];
            if (celda is not null)
            {
                celda.Background = marca;
            }
        }
    }

    private void LimpiarSeleccion()
    {
        if (_celdas.Length == 0)
        {
            return;
        }

        var fondo = BrochaTema("CardBackgroundFillColorDefaultBrush");
        var fondoHoy = BrochaTema("CardBackgroundFillColorSecondaryBrush");
        for (var f = 0; f < _celdas.GetLength(0); f++)
        {
            for (var d = 0; d < _celdas.GetLength(1); d++)
            {
                var hoy = d < ViewModel.Semana.Count && ViewModel.Semana[d].EsHoy;
                var celda = _celdas[f, d];
                if (celda is not null)
                {
                    celda.Background = hoy ? fondoHoy : fondo;
                }
            }
        }
    }

    private PuntoSemana? CeldaBajoPuntero(Point posicion)
    {
        foreach (var hijo in SemanaCuerpo.Children.OfType<Border>())
        {
            if (hijo.Tag is not PuntoSemana punto)
            {
                continue;
            }

            var transform = hijo.TransformToVisual(SemanaCuerpo);
            var origen = transform.TransformPoint(new Point(0, 0));
            var rect = new Rect(origen, new Size(hijo.ActualWidth, hijo.ActualHeight));
            if (rect.Contains(posicion))
            {
                return punto;
            }
        }

        return null;
    }

    private static int FilaDeHora(TimeSpan hora, IReadOnlyList<TimeSpan> horas, int intervalo)
    {
        for (var i = 0; i < horas.Count; i++)
        {
            var fin = horas[i].Add(TimeSpan.FromMinutes(intervalo));
            if (hora >= horas[i] && hora < fin)
            {
                return i;
            }
        }

        return -1;
    }

    private static Brush BrochaTema(string clave)
        => Application.Current.Resources.TryGetValue(clave, out var valor) && valor is Brush brocha
            ? brocha
            : new SolidColorBrush(Colors.Transparent);

    private async Task EditarAsync(Cita? existente, DateTime? fecha = null, TimeSpan? hora = null, int? duracionMinutos = null)
    {
        var dialog = new CitaDialog { XamlRoot = XamlRoot };
        dialog.Cargar(existente, fecha, hora, duracionMinutos: duracionMinutos);
        var resultado = await dialog.ShowAsync();
        if (resultado == ContentDialogResult.Secondary && existente is not null)
        {
            if (await DialogHelper.ConfirmarAsync(XamlRoot, "Eliminar cita", $"¿Eliminar la cita de {existente.NombrePaciente} el {existente.FechaTexto} a las {existente.HoraTexto}?"))
            {
                AppDatabase.Instance.EliminarCita(existente.Id);
                ViewModel.Recargar();
                ActualizarCalendario();
            }

            return;
        }

        if (resultado != ContentDialogResult.Primary)
        {
            return;
        }

        if (!await ConfirmarYGuardarCitaAsync(dialog.Cita))
        {
            return;
        }

        ViewModel.Recargar();
        ActualizarCalendario();
    }

    private async Task<bool> ConfirmarYGuardarCitaAsync(Cita cita)
    {
        if (cita.Estado is not "Cancelada" and not "No asistió" &&
            AppDatabase.Instance.HayMedicoOcupado(cita))
        {
            await DialogHelper.AvisoAsync(
                XamlRoot,
                "Médico ocupado",
                $"{cita.Medico} ya tiene una cita u operación en ese horario. Elija otra hora.");
            return false;
        }

        if (cita.Estado is not "Cancelada" and not "No asistió" &&
            AppDatabase.Instance.HayPacienteSolapado(cita) &&
            !await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Horario ocupado",
                "El paciente ya tiene otra cita en ese horario. ¿Guardar de todos modos?",
                "Guardar"))
        {
            return false;
        }

        if (!AppSettings.Current.EsDiaLaborable(cita.FechaInicio.DateTime) &&
            !await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Día no laborable",
                "Ese día está marcado como no laborable en Ajustes. ¿Agendar de todos modos?",
                "Agendar"))
        {
            return false;
        }

        AppDatabase.Instance.GuardarCita(cita);
        return true;
    }

    private async Task<bool> ConfirmarYGuardarOperacionAsync(Procedimiento procedimiento)
    {
        var bloqueo = AppDatabase.Instance.BloqueoTramo(
            procedimiento.Id,
            procedimiento.Medico,
            procedimiento.Estado,
            procedimiento.Fecha,
            procedimiento.DuracionMinutos ?? 60,
            0,
            excluirPrincipal: true);
        if (bloqueo is not null)
        {
            await DialogHelper.AvisoAsync(XamlRoot, "Horario no disponible", bloqueo);
            return false;
        }

        if (!AppSettings.Current.EsDiaLaborable(procedimiento.Fecha.DateTime) &&
            !await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Día no laborable",
                "Ese día está marcado como no laborable en Ajustes. ¿Agendar de todos modos?",
                "Agendar"))
        {
            return false;
        }

        AppDatabase.Instance.GuardarProcedimiento(procedimiento);
        return true;
    }

    private async Task<bool> ReprogramarSesionAsync(EventoCalendario evento, DateTimeOffset inicio, int duracion)
    {
        if (evento.Procedimiento is null || evento.SesionId <= 0)
        {
            return false;
        }

        var actual = AppDatabase.Instance.GetSesion(evento.SesionId);
        if (actual is null)
        {
            return false;
        }

        var bloqueo = AppDatabase.Instance.BloqueoTramo(
            evento.Procedimiento.Id,
            evento.Procedimiento.Medico ?? actual.Medico,
            actual.Estado,
            inicio,
            duracion,
            actual.Id,
            excluirPrincipal: false);
        if (bloqueo is not null)
        {
            await DialogHelper.AvisoAsync(XamlRoot, "Horario no disponible", bloqueo);
            return false;
        }

        if (!AppSettings.Current.EsDiaLaborable(inicio.DateTime) &&
            !await DialogHelper.ConfirmarAsync(
                XamlRoot,
                "Día no laborable",
                "Ese día está marcado como no laborable en Ajustes. ¿Agendar de todos modos?",
                "Agendar"))
        {
            return false;
        }

        actual.FechaInicio = inicio;
        actual.DuracionMinutos = duracion;
        AppDatabase.Instance.GuardarSesion(actual);
        return true;
    }

    private async Task AbrirEventoAsync(EventoCalendario evento)
    {
        if (evento.EsOperacion && evento.Procedimiento is not null)
        {
            AppNavigation.Navigate(typeof(OperacionDetallePage), evento.Procedimiento.Id);
            return;
        }

        if (evento.Cita is not null)
        {
            await EditarAsync(evento.Cita);
        }
    }

    private void SeleccionarVista(string vista)
    {
        _cargandoVista = true;
        foreach (SelectorBarItem item in VistaBar.Items)
        {
            if (item.Tag as string == vista)
            {
                VistaBar.SelectedItem = item;
                break;
            }
        }

        _cargandoVista = false;
    }

    private sealed record PuntoSemana(int Columna, int Fila, DateTime Fecha, TimeSpan Hora);
}
