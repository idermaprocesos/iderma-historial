using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;

namespace IdermaCapilar_Historial.ViewModels;

public partial class PacienteFormViewModel : ObservableObject
{
    [ObservableProperty] private Paciente paciente = new();
    [ObservableProperty] private ContactoEmergencia contacto = new();
    [ObservableProperty] private string titulo = "Nuevo expediente";
    [ObservableProperty] private string? error;
    [ObservableProperty] private string? aviso;

    private readonly AlertaTemporal _alerta;

    public PacienteFormViewModel()
    {
        _alerta = new AlertaTemporal(() =>
        {
            Error = null;
            Aviso = null;
        });
    }

    public bool HayError => !string.IsNullOrWhiteSpace(Error);
    public bool HayAviso => !string.IsNullOrWhiteSpace(Aviso);

    public IReadOnlyList<string> TiposDocumento => Catalogos.TiposDocumento;
    public IReadOnlyList<string> Sexos => Catalogos.Sexos;
    public IReadOnlyList<string> EstadosCiviles => Catalogos.EstadosCiviles;
    public IReadOnlyList<string> RelacionesEmergencia => Catalogos.RelacionesEmergencia;

    public void CargarNuevo()
    {
        Titulo = "Nuevo expediente";
        Paciente = new Paciente
        {
            NumeroExpediente = AppDatabase.Instance.SiguienteExpediente(),
            Nacionalidad = "Peruana",
            Ciudad = "Lima"
        };
        Contacto = new ContactoEmergencia();
        Error = null;
        Aviso = null;
    }

    public bool CargarExistente(int id)
    {
        var existente = AppDatabase.Instance.GetPaciente(id);
        if (existente is null)
        {
            Error = "No se encontró el expediente.";
            Aviso = null;
            return false;
        }

        Paciente = existente;
        Contacto = AppDatabase.Instance.GetContactos(id).FirstOrDefault() ?? new ContactoEmergencia { PacienteId = id };
        Titulo = $"Editar {existente.NombreCompleto}";
        Error = null;
        Aviso = null;
        return true;
    }

    public bool Guardar(out int id, bool silencioso = false)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(Paciente.Nombres) || string.IsNullOrWhiteSpace(Paciente.Apellidos))
        {
            if (!silencioso)
            {
                Error = "Nombres y apellidos son obligatorios.";
                Aviso = null;
                _alerta.Programar(false);
            }

            return false;
        }

        Error = null;
        id = AppDatabase.Instance.GuardarPaciente(Paciente);
        if (!string.IsNullOrWhiteSpace(Contacto.Nombre) && !string.IsNullOrWhiteSpace(Contacto.Telefono))
        {
            Contacto.PacienteId = id;
            AppDatabase.Instance.GuardarContacto(Contacto);
        }

        if (silencioso)
        {
            return true;
        }

        Aviso = $"Expediente {Paciente.NumeroExpediente} guardado.";
        _alerta.Programar(true);
        return true;
    }

    partial void OnErrorChanged(string? value) => OnPropertyChanged(nameof(HayError));
    partial void OnAvisoChanged(string? value) => OnPropertyChanged(nameof(HayAviso));
}
