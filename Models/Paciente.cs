using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;

namespace IdermaCapilar_Historial.Models;

public partial class Paciente : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string numeroExpediente = string.Empty;
    [ObservableProperty] private string nombres = string.Empty;
    [ObservableProperty] private string apellidos = string.Empty;
    [ObservableProperty] private string? tipoDocumento;
    [ObservableProperty] private string? numeroDocumento;
    [ObservableProperty] private DateTimeOffset? fechaNacimiento;
    [ObservableProperty] private string? sexo;
    [ObservableProperty] private string? estadoCivil;
    [ObservableProperty] private string? nacionalidad = "Peruana";
    [ObservableProperty] private string? ocupacion;
    [ObservableProperty] private string? direccion;
    [ObservableProperty] private string? distrito;
    [ObservableProperty] private string? ciudad = "Lima";
    [ObservableProperty] private string? telefono;
    [ObservableProperty] private string? telefonoAlterno;
    [ObservableProperty] private string? email;
    [ObservableProperty] private string? fotoRuta;
    [ObservableProperty] private BitmapImage? foto;
    [ObservableProperty] private DateTimeOffset fechaRegistro = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset? fechaActualizacion;
    [ObservableProperty] private bool activo = true;
    [ObservableProperty] private string? notasGenerales;

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();

    public string Iniciales
    {
        get
        {
            var n = string.IsNullOrWhiteSpace(Nombres) ? ' ' : char.ToUpperInvariant(Nombres.Trim()[0]);
            var a = string.IsNullOrWhiteSpace(Apellidos) ? ' ' : char.ToUpperInvariant(Apellidos.Trim()[0]);
            return $"{n}{a}".Trim();
        }
    }

    public int? Edad
    {
        get
        {
            if (FechaNacimiento is null)
            {
                return null;
            }

            try
            {
                var n = FechaNacimiento.Value;
                var nacimiento = new DateTime(n.Year, n.Month, n.Day);
                var hoy = DateTime.Today;
                if (nacimiento > hoy)
                {
                    return null;
                }

                var edad = hoy.Year - nacimiento.Year;
                if (edad < 0)
                {
                    return null;
                }

                if (edad > 0 && nacimiento > hoy.AddYears(-edad))
                {
                    edad--;
                }

                return Math.Clamp(edad, 0, 150);
            }
            catch
            {
                return null;
            }
        }
    }

    public string EdadTexto
    {
        get
        {
            if (FechaNacimiento is null)
            {
                return "Edad no registrada";
            }

            try
            {
                var n = FechaNacimiento.Value;
                var nacimiento = new DateTime(n.Year, n.Month, n.Day);
                if (nacimiento > DateTime.Today)
                {
                    return "Revise la fecha de nacimiento";
                }
            }
            catch
            {
                return "Edad no registrada";
            }

            return Edad is int edad ? $"{edad} años" : "Edad no registrada";
        }
    }

    public string Subtitulo
    {
        get
        {
            var partes = new List<string> { NumeroExpediente };
            if (Edad is int)
            {
                partes.Add(EdadTexto);
            }

            if (!string.IsNullOrWhiteSpace(Sexo))
            {
                partes.Add(Sexo);
            }

            if (!string.IsNullOrWhiteSpace(Telefono))
            {
                partes.Add(TelefonoFormato.Formatear(Telefono));
            }

            return string.Join("  ·  ", partes);
        }
    }

    partial void OnNombresChanged(string value) => NotifyDisplay();
    partial void OnApellidosChanged(string value) => NotifyDisplay();
    partial void OnNumeroExpedienteChanged(string value) => NotifyDisplay();
    partial void OnFechaNacimientoChanged(DateTimeOffset? value) => NotifyDisplay();
    partial void OnSexoChanged(string? value) => NotifyDisplay();
    partial void OnTelefonoChanged(string? value)
    {
        if (AplicarTelefono(value, v => Telefono = v))
        {
            NotifyDisplay();
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

    private void NotifyDisplay()
    {
        OnPropertyChanged(nameof(NombreCompleto));
        OnPropertyChanged(nameof(Iniciales));
        OnPropertyChanged(nameof(Edad));
        OnPropertyChanged(nameof(EdadTexto));
        OnPropertyChanged(nameof(Subtitulo));
    }
}
