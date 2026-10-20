using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Helpers;

public partial class EnfermeraCatalog : ObservableObject
{
    public static EnfermeraCatalog Instance { get; } = new();

    public ObservableCollection<string> Nombres { get; } = [];

    public string? Predeterminada =>
        string.IsNullOrWhiteSpace(AppSettings.Current.EnfermeraPredeterminada)
            ? Nombres.FirstOrDefault()
            : AppSettings.Current.EnfermeraPredeterminada;

    public void Recargar()
    {
        Nombres.Clear();
        foreach (var enfermera in AppDatabase.Instance.GetEnfermeras(soloActivas: true))
        {
            Nombres.Add(enfermera.NombreMostrado);
        }

        Asegurar(AppSettings.Current.EnfermeraPredeterminada);
        OnPropertyChanged(nameof(Predeterminada));
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

    public void CargarCombo(ComboBox combo, string? seleccionado, bool usarPredeterminadaSiVacio = false)
    {
        Asegurar(seleccionado);
        combo.ItemsSource = Nombres;
        combo.PlaceholderText = Nombres.Count == 0 ? "Registre enfermeras en el menú Enfermeras" : "Seleccionar enfermera";

        var valor = seleccionado;
        if (string.IsNullOrWhiteSpace(valor) && usarPredeterminadaSiVacio)
        {
            valor = Predeterminada;
        }

        combo.SelectedItem = string.IsNullOrWhiteSpace(valor) ? null : valor;
    }
}
