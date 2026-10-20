using CommunityToolkit.Mvvm.ComponentModel;

namespace IdermaCapilar_Historial.Models;

public partial class Doctor : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string titulo = "Dr.";
    [ObservableProperty] private string nombres = string.Empty;
    [ObservableProperty] private string apellidos = string.Empty;
    [ObservableProperty] private string? especialidad;
    [ObservableProperty] private string? cmp;
    [ObservableProperty] private string? telefono;
    [ObservableProperty] private string? email;
    [ObservableProperty] private bool activo = true;
    [ObservableProperty] private bool esPredeterminado;

    public string NombreMostrado
    {
        get
        {
            var nombre = $"{Nombres} {Apellidos}".Trim();
            return string.IsNullOrWhiteSpace(Titulo) ? nombre : $"{Titulo} {nombre}".Trim();
        }
    }

    public string Subtitulo
    {
        get
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(Especialidad))
            {
                partes.Add(Especialidad);
            }

            if (!string.IsNullOrWhiteSpace(Cmp))
            {
                partes.Add($"CMP {Cmp}");
            }

            if (EsPredeterminado)
            {
                partes.Add("Predeterminado");
            }

            return partes.Count == 0 ? "Médico de la clínica" : string.Join("  ·  ", partes);
        }
    }

    partial void OnTituloChanged(string value) => NotifyDisplay();
    partial void OnNombresChanged(string value) => NotifyDisplay();
    partial void OnApellidosChanged(string value) => NotifyDisplay();
    partial void OnEspecialidadChanged(string? value) => NotifyDisplay();
    partial void OnCmpChanged(string? value) => NotifyDisplay();
    partial void OnEsPredeterminadoChanged(bool value) => NotifyDisplay();
    partial void OnTelefonoChanged(string? value)
    {
        var formateado = Helpers.TelefonoFormato.FormatearONull(value);
        if (formateado != value)
        {
            Telefono = formateado;
        }
    }

    private void NotifyDisplay()
    {
        OnPropertyChanged(nameof(NombreMostrado));
        OnPropertyChanged(nameof(Subtitulo));
    }
}
