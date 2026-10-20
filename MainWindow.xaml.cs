using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Pages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI;

namespace IdermaCapilar_Historial;

public sealed partial class MainWindow : Window
{
    private bool _maximizado;
    private bool _sincronizandoMenu;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Iderma.Shell.BotonLauncher.Colocar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        Iderma.Shell.VentanaIcono.AplicarAlActivar(
            this,
            Path.Combine("img", "logo-historial.ico"),
            Path.Combine("Assets", "AppIcon.ico"));
        AplicarColoresBarraTitulo();

        AppNavigation.Frame = NavFrame;
        NavFrame.Navigated += NavFrame_Navigated;
        NavView.Loaded += NavView_Loaded;
        Activated += MainWindow_Activated;
        AutoGuardadoService.Instance.AplicarAjustes();
    }

    private void AplicarColoresBarraTitulo()
    {
        var barra = AppWindow.TitleBar;
        var oscuro = Application.Current.RequestedTheme == ApplicationTheme.Dark;

        barra.ButtonBackgroundColor = Colors.Transparent;
        barra.ButtonInactiveBackgroundColor = Colors.Transparent;
        barra.ButtonForegroundColor = oscuro ? Colors.White : Colors.Black;
        barra.ButtonHoverForegroundColor = oscuro ? Colors.White : Colors.Black;
        barra.ButtonPressedForegroundColor = oscuro ? Colors.White : Colors.Black;
        barra.ButtonInactiveForegroundColor = oscuro
            ? Color.FromArgb(255, 158, 158, 158)
            : Color.FromArgb(255, 96, 96, 96);
        barra.ButtonHoverBackgroundColor = oscuro
            ? Color.FromArgb(40, 255, 255, 255)
            : Color.FromArgb(32, 0, 0, 0);
        barra.ButtonPressedBackgroundColor = oscuro
            ? Color.FromArgb(70, 255, 255, 255)
            : Color.FromArgb(56, 0, 0, 0);
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_maximizado)
        {
            return;
        }

        _maximizado = true;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        if (NavFrame.Content is null)
        {
            NavegarDesdeMenu(typeof(InicioPage));
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        AppNavigation.GoBack();
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (_sincronizandoMenu)
        {
            return;
        }

        if (args.IsSettingsInvoked)
        {
            NavegarDesdeMenu(typeof(SettingsPage));
            return;
        }

        if (args.InvokedItemContainer is NavigationViewItem { Tag: string tag })
        {
            var pagina = PaginaDeEtiqueta(tag);
            if (pagina is not null)
            {
                NavegarDesdeMenu(pagina);
            }
        }
    }

    private void NavFrame_Navigated(object sender, NavigationEventArgs e)
    {
        AppTitleBar.IsBackButtonVisible = NavFrame.CanGoBack;
        AppTitleBar.IsBackButtonEnabled = NavFrame.CanGoBack;
        SincronizarSeleccion(e.SourcePageType);
    }

    private void NavegarDesdeMenu(Type pageType)
    {
        if (NavFrame.Content?.GetType() == pageType)
        {
            NavFrame.BackStack.Clear();
            AppTitleBar.IsBackButtonVisible = false;
            AppTitleBar.IsBackButtonEnabled = false;
            SincronizarSeleccion(pageType);
            return;
        }

        NavFrame.Navigate(pageType);
        NavFrame.BackStack.Clear();
    }

    private void SincronizarSeleccion(Type pageType)
    {
        _sincronizandoMenu = true;
        try
        {
            if (pageType == typeof(SettingsPage))
            {
                NavView.SelectedItem = NavView.SettingsItem;
                return;
            }

            var etiqueta = EtiquetaDePagina(pageType);
            if (etiqueta is null)
            {
                return;
            }

            foreach (var item in ItemsMenu())
            {
                if (item.Tag as string == etiqueta)
                {
                    NavView.SelectedItem = item;
                    return;
                }
            }
        }
        finally
        {
            _sincronizandoMenu = false;
        }
    }

    private IEnumerable<NavigationViewItem> ItemsMenu()
    {
        foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            yield return item;
        }

        foreach (var item in NavView.FooterMenuItems.OfType<NavigationViewItem>())
        {
            yield return item;
        }
    }

    private static Type? PaginaDeEtiqueta(string tag) => tag switch
    {
        "inicio" => typeof(InicioPage),
        "pacientes" => typeof(PacientesPage),
        "nuevo" => typeof(PacienteFormPage),
        "citas" => typeof(CitasPage),
        "operaciones" => typeof(OperacionesPage),
        "doctores" => typeof(DoctoresPage),
        "enfermeras" => typeof(EnfermerasPage),
        "about" => typeof(AboutPage),
        _ => null
    };

    private static string? EtiquetaDePagina(Type pageType)
    {
        if (pageType == typeof(InicioPage)) return "inicio";
        if (pageType == typeof(PacientesPage) || pageType == typeof(PacienteDetallePage)) return "pacientes";
        if (pageType == typeof(PacienteFormPage)) return "nuevo";
        if (pageType == typeof(CitasPage)) return "citas";
        if (pageType == typeof(OperacionesPage)) return "operaciones";
        if (pageType == typeof(DoctoresPage)) return "doctores";
        if (pageType == typeof(EnfermerasPage)) return "enfermeras";
        if (pageType == typeof(AboutPage)) return "about";
        return null;
    }
}
