using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Models;

namespace IdermaCapilar_Historial.ViewModels;

public partial class EnfermerasViewModel : ObservableObject
{
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private Enfermera? seleccionada;

    public ObservableCollection<Enfermera> Enfermeras { get; } = [];

    public void Cargar()
    {
        Enfermeras.Clear();
        var texto = Busqueda.Trim();
        foreach (var enfermera in AppDatabase.Instance.GetEnfermeras())
        {
            if (!string.IsNullOrWhiteSpace(texto) &&
                enfermera.NombreMostrado.Contains(texto, StringComparison.OrdinalIgnoreCase) == false &&
                (enfermera.Especialidad?.Contains(texto, StringComparison.OrdinalIgnoreCase) != true) &&
                (enfermera.Cep?.Contains(texto, StringComparison.OrdinalIgnoreCase) != true))
            {
                continue;
            }

            Enfermeras.Add(enfermera);
        }

        OnPropertyChanged(nameof(HayEnfermeras));
        OnPropertyChanged(nameof(SinEnfermeras));
        OnPropertyChanged(nameof(Resumen));
    }

    partial void OnBusquedaChanged(string value) => Cargar();

    public bool HayEnfermeras => Enfermeras.Count > 0;
    public bool SinEnfermeras => Enfermeras.Count == 0;
    public string Resumen => Enfermeras.Count == 1 ? "1 enfermera" : $"{Enfermeras.Count} enfermeras";
}
