using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Helpers;
using Microsoft.UI.Xaml;

namespace IdermaCapilar_Historial;

public partial class App : Application
{
    public static Window? MainAppWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, e) => e.SetObserved();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Exception? arranque = null;
        try
        {
            AppSettings.Load();
            AppDatabase.Instance.Initialize();
            DoctorCatalog.Instance.Recargar();
            EnfermeraCatalog.Instance.Recargar();
        }
        catch (Exception ex)
        {
            arranque = ex;
        }

        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
        if (arranque is not null)
        {
            var mensaje = arranque.Message;
            MainAppWindow.DispatcherQueue.TryEnqueue(async () =>
            {
                await UiSafe.MostrarAsync(MainAppWindow?.Content?.XamlRoot, "No se pudo iniciar el historial", mensaje);
            });
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        var mensaje = e.Message;
        var root = MainAppWindow?.Content?.XamlRoot;
        MainAppWindow?.DispatcherQueue.TryEnqueue(async () =>
        {
            await UiSafe.MostrarAsync(root, "Error inesperado", mensaje);
        });
    }
}
