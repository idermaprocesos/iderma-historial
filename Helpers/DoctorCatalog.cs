using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Helpers;

public partial class DoctorCatalog : ObservableObject
{
    public static DoctorCatalog Instance { get; } = new();

    public ObservableCollection<string> Nombres { get; } = [];

    public string? Predeterminado =>
        string.IsNullOrWhiteSpace(AppSettings.Current.MedicoPredeterminado)
            ? Nombres.FirstOrDefault()
            : AppSettings.Current.MedicoPredeterminado;

    public void Recargar()
    {
        Nombres.Clear();
        foreach (var doctor in AppDatabase.Instance.GetDoctores(soloActivos: true))
        {
            Nombres.Add(doctor.NombreMostrado);
        }

        Asegurar(AppSettings.Current.MedicoPredeterminado);
        OnPropertyChanged(nameof(Predeterminado));
    }

    public void Asegurar(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return;
        }

        var valor = nombre.Trim();
        if (!Nombres.Contains(valor))
        {
            Nombres.Add(valor);
        }
    }

    public void CargarCombo(ComboBox combo, string? seleccionado, bool usarPredeterminadoSiVacio = false)
    {
        Asegurar(seleccionado);
        combo.ItemsSource = Nombres;
        combo.PlaceholderText = Nombres.Count == 0 ? "Registre doctores en el menú Doctores" : "Seleccionar médico";

        var valor = seleccionado;
        if (string.IsNullOrWhiteSpace(valor) && usarPredeterminadoSiVacio)
        {
            valor = Predeterminado;
        }

        combo.SelectedItem = string.IsNullOrWhiteSpace(valor) ? null : valor;
    }
}
