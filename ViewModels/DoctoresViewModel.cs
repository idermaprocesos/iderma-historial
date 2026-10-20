using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Models;

namespace IdermaCapilar_Historial.ViewModels;

public partial class DoctoresViewModel : ObservableObject
{
    [ObservableProperty] private string busqueda = string.Empty;
    [ObservableProperty] private Doctor? seleccionado;

    public ObservableCollection<Doctor> Doctores { get; } = [];

    public IReadOnlyList<string> Titulos => Catalogos.TitulosMedicos;
    public IReadOnlyList<string> Especialidades => Catalogos.EspecialidadesMedicas;

    public void Cargar()
    {
        Doctores.Clear();
        var texto = Busqueda.Trim();
        foreach (var doctor in AppDatabase.Instance.GetDoctores())
        {
            if (!string.IsNullOrWhiteSpace(texto) &&
                doctor.NombreMostrado.Contains(texto, StringComparison.OrdinalIgnoreCase) == false &&
                (doctor.Especialidad?.Contains(texto, StringComparison.OrdinalIgnoreCase) != true) &&
                (doctor.Cmp?.Contains(texto, StringComparison.OrdinalIgnoreCase) != true))
            {
                continue;
            }

            Doctores.Add(doctor);
        }

        OnPropertyChanged(nameof(HayDoctores));
        OnPropertyChanged(nameof(SinDoctores));
        OnPropertyChanged(nameof(Resumen));
    }

    partial void OnBusquedaChanged(string value) => Cargar();

    public bool HayDoctores => Doctores.Count > 0;
    public bool SinDoctores => Doctores.Count == 0;
    public string Resumen => Doctores.Count == 1 ? "1 doctor" : $"{Doctores.Count} doctores";
}
