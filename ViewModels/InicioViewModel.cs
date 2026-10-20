using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Models;

namespace IdermaCapilar_Historial.ViewModels;

public partial class InicioViewModel : ObservableObject
{
    [ObservableProperty] private DashboardStats stats = new();
    [ObservableProperty] private string nombreClinica = "Iderma Capilar";

    public ObservableCollection<Paciente> Recientes { get; } = [];
    public ObservableCollection<Cita> Citas { get; } = [];

    public async Task CargarAsync()
    {
        NombreClinica = Helpers.AppSettings.Current.NombreClinica;
        try
        {
            Stats = AppDatabase.Instance.GetDashboardStats();
        }
        catch
        {
            Stats = new DashboardStats();
        }

        var recientes = new List<Paciente>();
        try
        {
            recientes = AppDatabase.Instance.GetPacientesRecientes();
        }
        catch
        {
            recientes = [];
        }

        var citas = new List<Cita>();
        try
        {
            citas = AppDatabase.Instance.GetProximasCitas();
        }
        catch
        {
            citas = [];
        }

        Recientes.Clear();
        foreach (var paciente in recientes)
        {
            Recientes.Add(paciente);
        }

        Citas.Clear();
        foreach (var cita in citas)
        {
            Citas.Add(cita);
        }

        OnPropertyChanged(nameof(HayRecientes));
        OnPropertyChanged(nameof(HayCitas));
        OnPropertyChanged(nameof(SinRecientes));
        OnPropertyChanged(nameof(SinCitas));

        try
        {
            await Helpers.FotoPaciente.AsignarAsync(recientes);
        }
        catch
        {
        }
    }

    public bool HayRecientes => Recientes.Count > 0;
    public bool SinRecientes => Recientes.Count == 0;
    public bool HayCitas => Citas.Count > 0;
    public bool SinCitas => Citas.Count == 0;
}
