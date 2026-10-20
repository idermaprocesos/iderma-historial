using CommunityToolkit.Mvvm.ComponentModel;

namespace IdermaCapilar_Historial.Models;

public partial class DatosMedicos : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private int pacienteId;
    [ObservableProperty] private string? grupoSanguineo;
    [ObservableProperty] private string? alergias;
    [ObservableProperty] private string? medicamentosActuales;
    [ObservableProperty] private string? antecedentesPersonales;
    [ObservableProperty] private string? antecedentesFamiliares;
    [ObservableProperty] private string? enfermedadesCronicas;
    [ObservableProperty] private string? cirugiasPrevias;
    [ObservableProperty] private string? fuma;
    [ObservableProperty] private string? alcohol;
    [ObservableProperty] private double? pesoKg;
    [ObservableProperty] private double? tallaCm;
    [ObservableProperty] private string? presionArterial;
    [ObservableProperty] private string? tipoAlopecia;
    [ObservableProperty] private string? escalaNorwood;
    [ObservableProperty] private string? escalaLudwig;
    [ObservableProperty] private string? zonaAfectada;
    [ObservableProperty] private string? densidadZonaDonante;
    [ObservableProperty] private string? calidadCabello;
    [ObservableProperty] private string? colorCabello;
    [ObservableProperty] private string? grosorCabello;
    [ObservableProperty] private string? diagnostico;
    [ObservableProperty] private string? planTratamiento;
    [ObservableProperty] private string? medicoResponsable;
    [ObservableProperty] private string? observacionesMedicas;

    public string? ImcTexto
    {
        get
        {
            if (PesoKg is not double peso || TallaCm is not double talla || talla <= 0)
            {
                return null;
            }

            var imc = peso / Math.Pow(talla / 100.0, 2);
            return $"{imc:0.0}";
        }
    }

    partial void OnPesoKgChanged(double? value) => OnPropertyChanged(nameof(ImcTexto));
    partial void OnTallaCmChanged(double? value) => OnPropertyChanged(nameof(ImcTexto));
}
