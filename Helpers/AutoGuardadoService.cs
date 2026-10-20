using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace IdermaCapilar_Historial.Helpers;

public partial class AutoGuardadoService : ObservableObject
{
    public static AutoGuardadoService Instance { get; } = new();

    private readonly DispatcherTimer _debounce = new() { Interval = TimeSpan.FromSeconds(4) };
    private readonly DispatcherTimer _periodico = new();
    private Func<bool>? _guardarFormulario;
    private DateTime _pausarFormularioHasta = DateTime.MinValue;
    private int _fallosSeguidos;

    [ObservableProperty] private string estado = "Autoguardado en espera";
    [ObservableProperty] private bool activo;

    private AutoGuardadoService()
    {
        _debounce.Tick += Debounce_Tick;
        _periodico.Tick += Periodico_Tick;
    }

    public void AplicarAjustes()
    {
        var settings = AppSettings.Current;
        Activo = settings.Autoguardado;
        _periodico.Stop();
        if (!Activo)
        {
            _debounce.Stop();
            Estado = "Autoguardado desactivado";
            return;
        }

        var minutos = Math.Clamp(settings.AutoguardadoMinutos, 10, 60);
        _periodico.Interval = TimeSpan.FromMinutes(minutos);
        _periodico.Start();
        Estado = $"Copia ZIP cada {minutos} min";
    }

    public void Registrar(Func<bool> guardarFormulario)
    {
        _guardarFormulario = guardarFormulario;
        _fallosSeguidos = 0;
        _pausarFormularioHasta = DateTime.MinValue;
    }

    public void Quitar(Func<bool> guardarFormulario)
    {
        if (_guardarFormulario == guardarFormulario)
        {
            _debounce.Stop();
            _guardarFormulario = null;
        }
    }

    public void PedirGuardado()
    {
        if (!Activo || _guardarFormulario is null || DateTime.Now < _pausarFormularioHasta)
        {
            return;
        }

        _debounce.Stop();
        _debounce.Start();
    }

    private void Debounce_Tick(object? sender, object e)
    {
        _debounce.Stop();
        GuardarFormulario();
    }

    private void Periodico_Tick(object? sender, object e)
    {
        if (_fallosSeguidos >= 2)
        {
            Estado = "Copia ZIP pausada hasta que se pueda guardar";
            return;
        }

        try
        {
            var zip = BackupService.CrearCopiaAutoguardado();
            var info = new FileInfo(zip);
            if (!info.Exists || info.Length < 1024)
            {
                if (info.Exists)
                {
                    info.Delete();
                }

                Estado = "No se pudo crear la copia automática";
                return;
            }

            Estado = $"Copia de seguridad {DateTime.Now:HH:mm}";
        }
        catch
        {
            Estado = "No se pudo crear la copia automática";
        }
    }

    private void GuardarFormulario()
    {
        if (DateTime.Now < _pausarFormularioHasta || _guardarFormulario is null)
        {
            return;
        }

        try
        {
            if (_guardarFormulario.Invoke())
            {
                _fallosSeguidos = 0;
                Estado = $"Guardado automáticamente {DateTime.Now:HH:mm}";
            }
        }
        catch
        {
            _fallosSeguidos++;
            _pausarFormularioHasta = DateTime.Now.AddMinutes(Math.Min(10, _fallosSeguidos * 2));
            _debounce.Stop();
            Estado = "Error al autoguardar; no se insistirá para no dañar la base";
        }
    }
}
