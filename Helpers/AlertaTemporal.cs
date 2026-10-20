using Microsoft.UI.Xaml;

namespace IdermaCapilar_Historial.Helpers;

public sealed class AlertaTemporal
{
    private readonly DispatcherTimer _timer = new();
    private readonly Action _ocultar;

    public AlertaTemporal(Action ocultar)
    {
        _ocultar = ocultar;
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            _ocultar();
        };
    }

    public void Detener()
    {
        _timer.Stop();
    }

    public void Programar(bool exito)
    {
        _timer.Stop();
        _timer.Interval = TimeSpan.FromSeconds(exito ? 2.5 : 4);
        _timer.Start();
    }
}
