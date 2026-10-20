using CommunityToolkit.Mvvm.ComponentModel;

namespace IdermaCapilar_Historial.Models;

public partial class ContactoEmergencia : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private int pacienteId;
    [ObservableProperty] private string nombre = string.Empty;
    [ObservableProperty] private string? relacion;
    [ObservableProperty] private string? telefono;
    [ObservableProperty] private string? telefonoAlterno;
    [ObservableProperty] private string? email;
    [ObservableProperty] private string? direccion;

    public string Detalle => string.Join("  ·  ", new[] { Relacion, Telefono }.Where(x => !string.IsNullOrWhiteSpace(x)));

    partial void OnTelefonoChanged(string? value)
    {
        if (AplicarTelefono(value, v => Telefono = v))
        {
            OnPropertyChanged(nameof(Detalle));
        }
    }

    partial void OnTelefonoAlternoChanged(string? value)
        => AplicarTelefono(value, v => TelefonoAlterno = v);

    private bool AplicarTelefono(string? value, Action<string?> asignar)
    {
        var formateado = Helpers.TelefonoFormato.FormatearONull(value);
        if (formateado != value)
        {
            asignar(formateado);
            return false;
        }

        return true;
    }
}
