using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace IdermaCapilar_Historial.Controls;

public sealed partial class MapaCabezaControl : UserControl
{
    public static readonly DependencyProperty ZonasProperty = DependencyProperty.Register(
        nameof(Zonas),
        typeof(string),
        typeof(MapaCabezaControl),
        new PropertyMetadata(null, (d, e) => ((MapaCabezaControl)d).Cargar(e.NewValue as string)));

    public static readonly DependencyProperty SoloLecturaProperty = DependencyProperty.Register(
        nameof(SoloLectura),
        typeof(bool),
        typeof(MapaCabezaControl),
        new PropertyMetadata(false, (d, _) => ((MapaCabezaControl)d).AplicarModo()));

    public static readonly DependencyProperty ForzarTresVistasProperty = DependencyProperty.Register(
        nameof(ForzarTresVistas),
        typeof(bool),
        typeof(MapaCabezaControl),
        new PropertyMetadata(false, (d, _) => ((MapaCabezaControl)d).Pintar()));

    private static readonly SolidColorBrush SeleccionRelleno = new(Color.FromArgb(255, 101, 166, 210));
    private static readonly SolidColorBrush SeleccionTexto = new(Colors.White);
    private const string GlifoVacio = "\uE739";
    private const string GlifoMarcado = "\uE73E";

    private readonly HashSet<string> _seleccion = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ToggleButton> _chips = [];
    private readonly Dictionary<string, FontIcon> _iconos = [];
    private readonly Dictionary<string, TextBlock> _estados = [];
    private readonly Dictionary<string, Image> _capas = [];
    private VistaMapa _vista = VistaMapa.Cabeza;
    private bool _cargando;
    private bool _cambiandoVista;

    public event EventHandler? ZonasChanged;

    public MapaCabezaControl()
    {
        InitializeComponent();
        Loaded += Mapa_Loaded;
        ActualThemeChanged += (_, _) => AplicarTema();
        AplicarModo();
        ConstruirChips();
    }

    public string? Zonas
    {
        get => GetValue(ZonasProperty) as string;
        set => SetValue(ZonasProperty, value);
    }

    public bool SoloLectura
    {
        get => (bool)GetValue(SoloLecturaProperty);
        set => SetValue(SoloLecturaProperty, value);
    }

    public bool ForzarTresVistas
    {
        get => (bool)GetValue(ForzarTresVistasProperty);
        set => SetValue(ForzarTresVistasProperty, value);
    }

    public string Valor => MapaZonasCabeza.Serializar(_seleccion);

    public string Nombres => MapaZonasCabeza.Nombres(Valor);

    public void Cargar(string? guardado)
    {
        _cargando = true;
        _seleccion.Clear();
        foreach (var id in MapaZonasCabeza.Ids(guardado))
        {
            _seleccion.Add(id);
        }

        ElegirVistaInicial();
        _cargando = false;
        Pintar();
    }

    private async void Mapa_Loaded(object sender, RoutedEventArgs e)
    {
        if (VistaBox.SelectedItem is null)
        {
            _cambiandoVista = true;
            VistaBox.SelectedItem = VistaBox.Items.OfType<RadioButton>()
                .FirstOrDefault(r => string.Equals(r.Tag as string, _vista.ToString(), StringComparison.OrdinalIgnoreCase));
            _cambiandoVista = false;
        }

        AplicarTema();
        AplicarModo();
        if (!SoloLectura)
        {
            await MapaCabezaHitMap.AsegurarAsync();
        }
    }

    private void Vista_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cambiandoVista || TagVista(VistaBox.SelectedItem) is not string tag)
        {
            return;
        }

        var vista = Enum.TryParse<VistaMapa>(tag, out var valor) ? valor : VistaMapa.Cabeza;
        if (vista == _vista && _capas.Count > 0)
        {
            return;
        }

        _vista = vista;
        ConstruirChips();
        AplicarTema();
        Pintar();
    }

    private static string? TagVista(object? item) => item switch
    {
        RadioButton { Tag: string tag } => tag,
        string texto when Enum.TryParse<VistaMapa>(texto, out _) => texto,
        FrameworkElement { Tag: string tag } => tag,
        _ => null
    };

    private void ElegirVistaInicial()
    {
        if (_seleccion.Count == 0)
        {
            return;
        }

        foreach (var vista in MapaZonasCabeza.Vistas)
        {
            if (MapaZonasCabeza.TieneVista(Valor, vista))
            {
                SeleccionarVista(vista);
                return;
            }
        }
    }

    private void SeleccionarVista(VistaMapa vista)
    {
        _vista = vista;
        _cambiandoVista = true;
        var destino = VistaBox.Items.OfType<RadioButton>()
            .FirstOrDefault(r => string.Equals(TagVista(r), vista.ToString(), StringComparison.OrdinalIgnoreCase));
        if (destino is not null)
        {
            destino.IsChecked = true;
            VistaBox.SelectedItem = destino;
        }
        _cambiandoVista = false;
        ConstruirChips();
        AplicarTema();
    }

    private void AplicarTema()
    {
        var oscuro = ActualTheme == ElementTheme.Dark
            || (ActualTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
        BaseImage.Source = new BitmapImage(MapaZonasCabeza.UriCompleto(_vista, oscuro));
        foreach (var zona in MapaZonasCabeza.De(_vista))
        {
            if (_capas.TryGetValue(zona.Id, out var imagen))
            {
                imagen.Source = new BitmapImage(MapaZonasCabeza.UriZona(zona, oscuro));
            }
        }
    }

    private void AplicarModo()
    {
        var lectura = SoloLectura;
        TituloText.Visibility = lectura ? Visibility.Collapsed : Visibility.Visible;
        LeyendaText.Visibility = lectura ? Visibility.Collapsed : Visibility.Visible;
        ChipsGrid.Visibility = lectura ? Visibility.Collapsed : Visibility.Visible;
        VistaBox.Visibility = lectura ? Visibility.Collapsed : Visibility.Visible;
        LecturaMapas.Visibility = lectura ? Visibility.Visible : Visibility.Collapsed;
        MapaYTabla.Visibility = lectura ? Visibility.Collapsed : Visibility.Visible;
        MapaViewbox.MaxWidth = lectura ? 200 : 360;
        MapaViewbox.MaxHeight = lectura ? 200 : 360;
        RaizPanel.Spacing = lectura ? 8 : 12;
        MapaGrid.IsHitTestVisible = !lectura;
        Pintar();
    }

    private void ConstruirChips()
    {
        _chips.Clear();
        _iconos.Clear();
        _estados.Clear();
        _capas.Clear();
        ChipsGrid.Children.Clear();
        ChipsGrid.RowDefinitions.Clear();
        CapasZonas.Children.Clear();

        var zonas = MapaZonasCabeza.De(_vista);
        var filas = (zonas.Count + 1) / 2;
        for (var i = 0; i < filas; i++)
        {
            ChipsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (var i = 0; i < zonas.Count; i++)
        {
            var zona = zonas[i];
            var imagen = new Image
            {
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed,
                Tag = zona.Id
            };
            _capas[zona.Id] = imagen;
            CapasZonas.Children.Add(imagen);

            var icono = new FontIcon { FontSize = 18, Glyph = GlifoVacio, VerticalAlignment = VerticalAlignment.Center };
            var estado = new TextBlock
            {
                Opacity = 0.75,
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                Text = "Pulse para marcar"
            };
            var boton = new ToggleButton
            {
                MinHeight = 56,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 8, 12, 8),
                Tag = zona.Id,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children =
                    {
                        icono,
                        new StackPanel
                        {
                            Children =
                            {
                                new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Text = zona.Nombre, TextWrapping = TextWrapping.Wrap },
                                estado
                            }
                        }
                    }
                }
            };
            ToolTipService.SetToolTip(boton, "Marca o quita: " + zona.Nombre);
            boton.Click += Chip_Click;
            Grid.SetRow(boton, i / 2);
            Grid.SetColumn(boton, i % 2);
            ChipsGrid.Children.Add(boton);
            _chips[zona.Id] = boton;
            _iconos[zona.Id] = icono;
            _estados[zona.Id] = estado;
        }
    }

    private void Mapa_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (SoloLectura)
        {
            return;
        }

        var punto = e.GetCurrentPoint(MapaGrid).Position;
        var id = MapaCabezaHitMap.ZonaEn(_vista, punto.X, punto.Y, MapaGrid.ActualWidth, MapaGrid.ActualHeight);
        if (id is null)
        {
            return;
        }

        e.Handled = true;
        Alternar(id);
    }

    private void Chip_Click(object sender, RoutedEventArgs e)
    {
        if (SoloLectura || sender is not ToggleButton { Tag: string id } boton)
        {
            return;
        }

        if (boton.IsChecked == true)
        {
            _seleccion.Add(id);
        }
        else
        {
            _seleccion.Remove(id);
        }

        Pintar();
        if (!_cargando)
        {
            ZonasChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Alternar(string id)
    {
        if (!_seleccion.Add(id))
        {
            _seleccion.Remove(id);
        }

        Pintar();
        if (!_cargando)
        {
            ZonasChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Pintar()
    {
        if (SoloLectura)
        {
            if (LecturaMapas is null)
            {
                return;
            }

            PintarLectura();
            return;
        }

        Visibility = Visibility.Visible;
        foreach (var zona in MapaZonasCabeza.De(_vista))
        {
            if (_capas.TryGetValue(zona.Id, out var capa))
            {
                capa.Visibility = _seleccion.Contains(zona.Id)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        LeyendaText.Text = _seleccion.Count == 0
            ? "Elija cabeza, frente o dorso. Pulse el mapa o un botón para marcar o quitar una zona."
            : "Marcadas: " + Nombres + ". Pulse de nuevo para quitar.";
        TablaTitulo.Text = "Zonas · " + MapaZonasCabeza.Titulo(_vista);

        foreach (var (id, chip) in _chips)
        {
            var activa = _seleccion.Contains(id);
            chip.IsChecked = activa;
            _iconos[id].Glyph = activa ? GlifoMarcado : GlifoVacio;
            _estados[id].Text = activa ? "Operada · pulse para quitar" : "Pulse para marcar";
            if (activa)
            {
                chip.Background = SeleccionRelleno;
                chip.Foreground = SeleccionTexto;
                _iconos[id].Foreground = SeleccionTexto;
                _estados[id].Foreground = SeleccionTexto;
                _estados[id].Opacity = 0.9;
            }
            else
            {
                chip.ClearValue(Control.BackgroundProperty);
                chip.ClearValue(Control.ForegroundProperty);
                _iconos[id].ClearValue(Control.ForegroundProperty);
                _estados[id].ClearValue(Control.ForegroundProperty);
                _estados[id].Opacity = 0.75;
            }
        }

        PintarTabla(TablaZonas, _vista, SoloLectura, todas: true);
    }

    private void PintarLectura()
    {
        LecturaMapas.Children.Clear();
        var vistas = ForzarTresVistas
            ? MapaZonasCabeza.Vistas.ToList()
            : MapaZonasCabeza.Vistas.Where(v => MapaZonasCabeza.TieneVista(Valor, v)).ToList();
        Visibility = ForzarTresVistas || vistas.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (vistas.Count == 0)
        {
            return;
        }

        var oscuro = ActualTheme == ElementTheme.Dark
            || (ActualTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
        foreach (var vista in vistas)
        {
            LecturaMapas.Children.Add(CrearBloqueLectura(vista, oscuro));
        }
    }

    private FrameworkElement CrearBloqueLectura(VistaMapa vista, bool oscuro)
    {
        var capas = new Grid { IsHitTestVisible = false };
        capas.Children.Add(new Image
        {
            Stretch = Stretch.Uniform,
            Source = new BitmapImage(MapaZonasCabeza.UriCompleto(vista, oscuro))
        });
        foreach (var zona in MapaZonasCabeza.De(vista).Where(z => _seleccion.Contains(z.Id)))
        {
            capas.Children.Add(new Image
            {
                Stretch = Stretch.Uniform,
                Source = new BitmapImage(MapaZonasCabeza.UriZona(zona, oscuro))
            });
        }

        var tabla = new Grid { ColumnSpacing = 12, RowSpacing = 6 };
        PintarTabla(tabla, vista, true, todas: false);

        var bloque = new Grid { ColumnSpacing = 20, HorizontalAlignment = HorizontalAlignment.Left };
        bloque.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bloque.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 180 });
        var lienzo = new Viewbox
        {
            MaxWidth = 200,
            MaxHeight = 200,
            Stretch = Stretch.Uniform,
            Child = new Grid { Width = 1000, Height = 1000, Children = { capas } }
        };
        var tarjeta = new Border
        {
            MinWidth = 180,
            Padding = new Thickness(12, 10, 12, 10),
            VerticalAlignment = VerticalAlignment.Center,
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Text = MapaZonasCabeza.Titulo(vista) },
                    tabla
                }
            }
        };
        Grid.SetColumn(tarjeta, 1);
        bloque.Children.Add(lienzo);
        bloque.Children.Add(tarjeta);
        return bloque;
    }

    private void PintarTabla(Grid tabla, VistaMapa vista, bool lectura, bool todas)
    {
        tabla.Children.Clear();
        tabla.RowDefinitions.Clear();
        tabla.ColumnDefinitions.Clear();
        tabla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tabla.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AgregarCelda(tabla, "Zona", 0, 0, true, false, lectura);
        AgregarCelda(tabla, "Operada", 0, 1, true, false, lectura);

        var fila = 1;
        foreach (var zona in MapaZonasCabeza.De(vista))
        {
            var operada = _seleccion.Contains(zona.Id);
            if (!todas && !operada)
            {
                continue;
            }

            tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AgregarCelda(tabla, zona.Nombre, fila, 0, false, operada, lectura);
            AgregarCelda(tabla, operada ? "Sí" : "No", fila, 1, false, operada, lectura);
            fila++;
        }

        if (fila == 1)
        {
            tabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AgregarCelda(tabla, "Ninguna en esta vista", 1, 0, false, false, lectura);
            AgregarCelda(tabla, "—", 1, 1, false, false, lectura);
        }
    }

    private void AgregarCelda(Grid tabla, string texto, int fila, int columna, bool encabezado, bool activa, bool lectura)
    {
        var bloque = new TextBlock
        {
            Text = texto,
            FontWeight = encabezado || activa ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
            Opacity = encabezado ? 0.7 : activa ? 1 : 0.75,
            FontSize = lectura ? 12 : 13,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        if (activa && columna == 1)
        {
            bloque.Foreground = SeleccionRelleno;
        }

        Grid.SetRow(bloque, fila);
        Grid.SetColumn(bloque, columna);
        tabla.Children.Add(bloque);
    }
}
