using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml.Media.Imaging;

namespace IdermaCapilar_Historial.ViewModels;

public partial class PacienteDetalleViewModel : ObservableObject
{
    [ObservableProperty] private Paciente paciente = new();
    [ObservableProperty] private DatosMedicos datosMedicos = new();
    [ObservableProperty] private BitmapImage? foto;
    [ObservableProperty] private string? mensaje;
    [ObservableProperty] private Microsoft.UI.Xaml.Controls.InfoBarSeverity severidad = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
    [ObservableProperty] private bool mensajeVisible;

    [ObservableProperty] private string zonasResumen = string.Empty;
    [ObservableProperty] private string filtroOperaciones = "Nuevas a antiguas";

    public ObservableCollection<ContactoEmergencia> Contactos { get; } = [];
    public ObservableCollection<Procedimiento> Procedimientos { get; } = [];
    public ObservableCollection<Procedimiento> ProcedimientosVisibles { get; } = [];
    public ObservableCollection<Observacion> Observaciones { get; } = [];
    public ObservableCollection<Cita> Citas { get; } = [];

    private readonly AlertaTemporal _alerta;

    public PacienteDetalleViewModel()
    {
        _alerta = new AlertaTemporal(() => MensajeVisible = false);
    }

    public IReadOnlyList<string> FiltrosOperacion { get; } = ["Nuevas a antiguas", "Antiguas a nuevas"];
    public IReadOnlyList<string> TiposDocumento => Catalogos.TiposDocumento;
    public IReadOnlyList<string> Sexos => Catalogos.Sexos;
    public IReadOnlyList<string> EstadosCiviles => Catalogos.EstadosCiviles;
    public IReadOnlyList<string> GruposSanguineos => Catalogos.GruposSanguineos;
    public IReadOnlyList<string> Habitos => Catalogos.Habitos;
    public IReadOnlyList<string> TiposAlopecia => Catalogos.TiposAlopecia;
    public IReadOnlyList<string> EscalasNorwood => Catalogos.EscalasNorwood;
    public IReadOnlyList<string> EscalasLudwig => Catalogos.EscalasLudwig;

    public bool HayContactos => Contactos.Count > 0;
    public bool SinContactos => Contactos.Count == 0;
    public bool HayProcedimientos => Procedimientos.Count > 0;
    public bool SinProcedimientos => Procedimientos.Count == 0;
    public bool HayProcedimientosVisibles => ProcedimientosVisibles.Count > 0;
    public bool SinProcedimientosVisibles => Procedimientos.Count > 0 && ProcedimientosVisibles.Count == 0;
    public string VacioFiltroTexto => "Aún no hay operaciones ni tratamientos registrados.";
    public bool HayObservaciones => Observaciones.Count > 0;
    public bool SinObservaciones => Observaciones.Count == 0;
    public bool HayCitas => Citas.Count > 0;
    public bool SinCitas => Citas.Count == 0;
    public string ZonasResumenTexto => string.IsNullOrWhiteSpace(MapaZonasCabeza.Nombres(ZonasResumen))
        ? "Aún no hay zonas marcadas en las operaciones de este paciente."
        : "Zonas acumuladas: " + MapaZonasCabeza.Nombres(ZonasResumen);

    public bool Cargar(int id)
    {
        try
        {
            var existente = AppDatabase.Instance.GetPaciente(id);
            if (existente is null)
            {
                return false;
            }

            Paciente = existente;
            DatosMedicos = AppDatabase.Instance.GetDatosMedicos(id);
            AlinearCombos();
            RecargarListas();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void RecargarListas()
    {
        Contactos.Clear();
        Procedimientos.Clear();
        Observaciones.Clear();
        Citas.Clear();
        try
        {
            foreach (var item in AppDatabase.Instance.GetContactos(Paciente.Id))
            {
                Contactos.Add(item);
            }
        }
        catch
        {
        }

        try
        {
            foreach (var item in AppDatabase.Instance.GetProcedimientos(Paciente.Id))
            {
                Procedimientos.Add(item);
            }
        }
        catch
        {
        }

        try
        {
            foreach (var item in AppDatabase.Instance.GetObservaciones(Paciente.Id))
            {
                Observaciones.Add(item);
            }
        }
        catch
        {
        }

        try
        {
            foreach (var item in AppDatabase.Instance.GetCitasPaciente(Paciente.Id))
            {
                Citas.Add(item);
            }
        }
        catch
        {
        }

        OnPropertyChanged(nameof(HayContactos));
        OnPropertyChanged(nameof(SinContactos));
        OnPropertyChanged(nameof(HayProcedimientos));
        OnPropertyChanged(nameof(SinProcedimientos));
        OnPropertyChanged(nameof(HayObservaciones));
        OnPropertyChanged(nameof(SinObservaciones));
        OnPropertyChanged(nameof(HayCitas));
        OnPropertyChanged(nameof(SinCitas));
        ActualizarZonasResumen();
        NumerarProcedimientos();
        AplicarFiltroOperaciones();
    }

    private void NumerarProcedimientos()
    {
        var orden = Procedimientos.OrderBy(p => p.Fecha).ThenBy(p => p.Id).ToList();
        for (var i = 0; i < orden.Count; i++)
        {
            orden[i].NumeroHistorial = i + 1;
        }
    }

    partial void OnFiltroOperacionesChanged(string value) => AplicarFiltroOperaciones();

    private void AplicarFiltroOperaciones()
    {
        IEnumerable<Procedimiento> lista = FiltroOperaciones == "Antiguas a nuevas"
            ? Procedimientos.OrderBy(p => p.Fecha).ThenBy(p => p.Id)
            : Procedimientos.OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id);

        ProcedimientosVisibles.Clear();
        foreach (var item in lista)
        {
            ProcedimientosVisibles.Add(item);
        }

        OnPropertyChanged(nameof(HayProcedimientosVisibles));
        OnPropertyChanged(nameof(SinProcedimientosVisibles));
        OnPropertyChanged(nameof(VacioFiltroTexto));
    }

    private void ActualizarZonasResumen()
    {
        ZonasResumen = MapaZonasCabeza.Unir(
            Procedimientos
                .Where(p => !string.Equals(p.Estado, "Cancelado", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.ZonasOperadas));
        OnPropertyChanged(nameof(ZonasResumenTexto));
    }

    private void AlinearCombos()
    {
        Paciente.TipoDocumento = Catalogos.Alinear(TiposDocumento, Paciente.TipoDocumento);
        Paciente.Sexo = Catalogos.Alinear(Sexos, Paciente.Sexo);
        Paciente.EstadoCivil = Catalogos.Alinear(EstadosCiviles, Paciente.EstadoCivil);
        DatosMedicos.GrupoSanguineo = Catalogos.Alinear(GruposSanguineos, DatosMedicos.GrupoSanguineo);
        DatosMedicos.Fuma = Catalogos.Alinear(Habitos, DatosMedicos.Fuma);
        DatosMedicos.Alcohol = Catalogos.Alinear(Habitos, DatosMedicos.Alcohol);
        DatosMedicos.TipoAlopecia = Catalogos.Alinear(TiposAlopecia, DatosMedicos.TipoAlopecia);
        DatosMedicos.EscalaNorwood = Catalogos.Alinear(EscalasNorwood, DatosMedicos.EscalaNorwood);
        DatosMedicos.EscalaLudwig = Catalogos.Alinear(EscalasLudwig, DatosMedicos.EscalaLudwig);
    }

    public bool GuardarPersonales(bool silencioso = false)
    {
        if (string.IsNullOrWhiteSpace(Paciente.Nombres) || string.IsNullOrWhiteSpace(Paciente.Apellidos))
        {
            if (!silencioso)
            {
                Mostrar("Nombres y apellidos son obligatorios.", false);
            }

            return false;
        }

        AppDatabase.Instance.GuardarPaciente(Paciente);
        if (!silencioso)
        {
            Mostrar("Datos personales guardados.");
        }

        return true;
    }

    public void GuardarMedicos(bool silencioso = false)
    {
        DatosMedicos.PacienteId = Paciente.Id;
        AppDatabase.Instance.GuardarDatosMedicos(DatosMedicos);
        if (!silencioso)
        {
            Mostrar("Datos médicos y evaluación capilar guardados.");
        }
    }

    public bool GuardarSilencioso()
    {
        if (Paciente.Id == 0)
        {
            return false;
        }

        GuardarPersonales(silencioso: true);
        if (Paciente.Id == 0)
        {
            return false;
        }

        GuardarMedicos(silencioso: true);
        return true;
    }

    public void GuardarContacto(ContactoEmergencia contacto)
    {
        contacto.PacienteId = Paciente.Id;
        AppDatabase.Instance.GuardarContacto(contacto);
        RecargarListas();
        Mostrar("Contacto de emergencia actualizado.");
    }

    public void EliminarContacto(ContactoEmergencia contacto)
    {
        AppDatabase.Instance.EliminarContacto(contacto.Id);
        RecargarListas();
        Mostrar("Contacto eliminado.");
    }

    public void GuardarProcedimiento(Procedimiento procedimiento)
    {
        procedimiento.PacienteId = Paciente.Id;
        AppDatabase.Instance.GuardarProcedimiento(procedimiento);
        RecargarListas();
        Mostrar("Procedimiento actualizado.");
    }

    public void EliminarProcedimiento(Procedimiento procedimiento)
    {
        AppDatabase.Instance.EliminarProcedimiento(procedimiento.Id);
        RecargarListas();
        Mostrar("Procedimiento eliminado.");
    }

    public void GuardarObservacion(Observacion observacion)
    {
        observacion.PacienteId = Paciente.Id;
        AppDatabase.Instance.GuardarObservacion(observacion);
        RecargarListas();
        Mostrar("Observación registrada.");
    }

    public void EliminarObservacion(Observacion observacion)
    {
        AppDatabase.Instance.EliminarObservacion(observacion.Id);
        RecargarListas();
        Mostrar("Observación eliminada.");
    }

    public void GuardarCita(Cita cita)
    {
        cita.PacienteId = Paciente.Id;
        AppDatabase.Instance.GuardarCita(cita);
        RecargarListas();
        Mostrar("Cita actualizada.");
    }

    public void EliminarCita(Cita cita)
    {
        AppDatabase.Instance.EliminarCita(cita.Id);
        RecargarListas();
        Mostrar("Cita eliminada.");
    }

    public void EliminarExpediente()
    {
        AppDatabase.Instance.EliminarPaciente(Paciente.Id);
    }

    public async Task ActualizarFotoAsync(string ruta)
    {
        Paciente.FotoRuta = ruta;
        AppDatabase.Instance.GuardarPaciente(Paciente);
        await CargarFotoAsync();
        Mostrar("Fotografía actualizada.");
    }

    public async Task CargarFotoAsync()
    {
        Foto = await FotoPaciente.CargarAsync(Paciente.FotoRuta);
    }

    private void Mostrar(string texto, bool ok = true)
    {
        Mensaje = texto;
        Severidad = ok
            ? Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success
            : Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
        MensajeVisible = true;
        _alerta.Programar(ok);
    }
}
