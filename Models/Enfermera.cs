using CommunityToolkit.Mvvm.ComponentModel;

namespace IdermaCapilar_Historial.Models;

public partial class Enfermera : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string titulo = "Enf.";
    [ObservableProperty] private string nombres = string.Empty;
    [ObservableProperty] private string apellidos = string.Empty;
    [ObservableProperty] private string? especialidad;
    [ObservableProperty] private string? cep;
    [ObservableProperty] private string? telefono;
    [ObservableProperty] private string? email;
    [ObservableProperty] private bool activo = true;
    [ObservableProperty] private bool esPredeterminada;

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

            if (!string.IsNullOrWhiteSpace(Cep))
            {
                partes.Add($"CEP {Cep}");
            }

            if (EsPredeterminada)
            {
                partes.Add("Predeterminada");
            }

            return partes.Count == 0 ? "Enfermera de la clínica" : string.Join("  ·  ", partes);
        }
    }

    partial void OnTituloChanged(string value) => NotifyDisplay();
    partial void OnNombresChanged(string value) => NotifyDisplay();
    partial void OnApellidosChanged(string value) => NotifyDisplay();
    partial void OnEspecialidadChanged(string? value) => NotifyDisplay();
    partial void OnCepChanged(string? value) => NotifyDisplay();
    partial void OnEsPredeterminadaChanged(bool value) => NotifyDisplay();
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
