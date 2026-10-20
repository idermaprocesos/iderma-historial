using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Models;

namespace IdermaCapilar_Historial.ViewModels;

public partial class OperacionesViewModel : ObservableObject
{
    private int _carga;
    private CancellationTokenSource? _debounce;
    private int _total;

    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private bool hayMas;

    public ObservableCollection<Procedimiento> Operaciones { get; } = [];

    public void Cargar(bool acumular = false)
    {
        var token = acumular ? _carga : ++_carga;
        var offset = acumular ? Operaciones.Count : 0;
        var pagina = AppDatabase.Instance.BuscarProcedimientosPagina(Busqueda, AppDatabase.PaginaOperaciones, offset);
        if (token != _carga)
        {
            return;
        }

        if (!acumular)
        {
            Operaciones.Clear();
        }

        foreach (var operacion in pagina.Items)
        {
            Operaciones.Add(operacion);
        }

        _total = pagina.Total;
        HayMas = Operaciones.Count < _total;
        OnPropertyChanged(nameof(HayOperaciones));
        OnPropertyChanged(nameof(SinOperaciones));
        OnPropertyChanged(nameof(Resumen));
    }

    public void CargarMas() => Cargar(acumular: true);

    partial void OnBusquedaChanged(string value)
    {
        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();
        var token = _debounce.Token;
        _ = RecargarConEsperaAsync(token);
    }

    private async Task RecargarConEsperaAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(280, token);
            Cargar();
        }
        catch (TaskCanceledException)
        {
        }
    }

    public bool HayOperaciones => Operaciones.Count > 0;
    public bool SinOperaciones => Operaciones.Count == 0;

    public string Resumen
    {
        get
        {
            if (_total == 0)
            {
                return "Sin operaciones";
            }

            if (Operaciones.Count < _total)
            {
                return $"Mostrando {Operaciones.Count} de {_total} operaciones";
            }

            return _total == 1 ? "1 operación" : $"{_total} operaciones";
        }
    }
}
