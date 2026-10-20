using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;

namespace IdermaCapilar_Historial.ViewModels;

public partial class PacientesViewModel : ObservableObject
{
    private int _carga;
    private bool _silenciarFiltro;
    private CancellationTokenSource? _debounce;
    private int _total;

    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private Paciente? seleccionado;
    [ObservableProperty] private bool soloActivos;
    [ObservableProperty] private bool hayMas;

    public ObservableCollection<Paciente> Pacientes { get; } = [];

    public void AplicarNavegacion(bool soloActivos)
    {
        _silenciarFiltro = true;
        SoloActivos = soloActivos;
        _silenciarFiltro = false;
    }

    public async Task CargarAsync(bool acumular = false)
    {
        var token = acumular ? _carga : ++_carga;
        var busqueda = Busqueda;
        var solo = SoloActivos;
        var offset = acumular ? Pacientes.Count : 0;
        PaginaConsulta<Paciente> pagina;
        try
        {
            pagina = await Task.Run(() =>
                AppDatabase.Instance.BuscarPacientesPagina(busqueda, solo, AppDatabase.PaginaPacientes, offset));
        }
        catch
        {
            if (token != _carga)
            {
                return;
            }

            throw;
        }

        if (token != _carga)
        {
            return;
        }

        if (!acumular)
        {
            Pacientes.Clear();
        }

        foreach (var paciente in pagina.Items)
        {
            Pacientes.Add(paciente);
        }

        _total = pagina.Total;
        HayMas = Pacientes.Count < _total;
        NotificarLista();

        try
        {
            await FotoPaciente.AsignarAsync(pagina.Items);
        }
        catch
        {
            // Las fotos no deben impedir ver los expedientes.
        }
    }

    public Task CargarMasAsync() => CargarAsync(acumular: true);

    private void NotificarLista()
    {
        OnPropertyChanged(nameof(HayPacientes));
        OnPropertyChanged(nameof(SinPacientes));
        OnPropertyChanged(nameof(Resumen));
    }

    partial void OnBusquedaChanged(string value)
    {
        if (!_silenciarFiltro)
        {
            _ = RecargarConEsperaAsync();
        }
    }

    partial void OnSoloActivosChanged(bool value)
    {
        if (!_silenciarFiltro)
        {
            _ = CargarAsync();
        }
    }

    private async Task RecargarConEsperaAsync()
    {
        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();
        var token = _debounce.Token;
        try
        {
            await Task.Delay(280, token);
            await CargarAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    public bool HayPacientes => Pacientes.Count > 0;
    public bool SinPacientes => Pacientes.Count == 0;

    public string Resumen
    {
        get
        {
            var etiqueta = SoloActivos ? "expedientes activos" : "expedientes";
            if (_total == 0)
            {
                return SoloActivos ? "Sin expedientes activos" : "Sin expedientes";
            }

            if (Pacientes.Count < _total)
            {
                return $"Mostrando {Pacientes.Count} de {_total} {etiqueta}";
            }

            return _total == 1
                ? (SoloActivos ? "1 expediente activo" : "1 expediente")
                : $"{_total} {etiqueta}";
        }
    }
}
