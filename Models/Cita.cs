using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace IdermaCapilar_Historial.Models;

public partial class Cita : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private int pacienteId;
    [ObservableProperty] private DateTimeOffset fechaInicio = DateTimeOffset.Now;
    [ObservableProperty] private int duracionMinutos = 30;
    [ObservableProperty] private string tipo = "Consulta";
    [ObservableProperty] private string estado = "Programada";
    [ObservableProperty] private string? medico;
    [ObservableProperty] private string? notas;
    [ObservableProperty] private string temperamento = "Amable";
    [ObservableProperty] private string? nombrePaciente;
    [ObservableProperty] private string? numeroExpediente;
    [ObservableProperty] private string? telefonoPaciente;
    [ObservableProperty] private string? comprobanteTipo;
    [ObservableProperty] private string? comprobanteNumero;
    [ObservableProperty] private string? comprobanteConcepto;
    [ObservableProperty] private double? comprobanteMonto;
    [ObservableProperty] private string? comprobanteFormaPago;
    [ObservableProperty] private string? comprobanteCliente;
    [ObservableProperty] private string? comprobanteDocumento;
    [ObservableProperty] private string? comprobanteCorreo;

    public DateTimeOffset FechaFin => FechaInicio.AddMinutes(Math.Max(5, DuracionMinutos));

    public string HoraTexto => FechaInicio.ToString("HH:mm");

    public string FechaTexto => FechaInicio.ToString("dd/MM/yyyy");

    public string HorarioTexto => $"{HoraTexto}–{FechaFin:HH:mm}";

    public string TituloCorto => string.IsNullOrWhiteSpace(NombrePaciente) ? Tipo : NombrePaciente!;

    public string ChipTexto => $"{HoraTexto} {TituloCorto}";

    public string Resumen => string.Join("  ·  ", new[]
    {
        FechaTexto,
        HorarioTexto,
        Tipo,
        Temperamento,
        Medico
    }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public string Subtitulo => string.Join("  ·  ", new[]
    {
        NumeroExpediente,
        Tipo,
        Temperamento,
        Estado,
        TelefonoPaciente
    }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public Brush ColorEstado => Estado is "Cancelada" or "No asistió"
        ? new SolidColorBrush(Color.FromArgb(255, 120, 120, 120))
        : Catalogos.TipoCitaPorNombre(Tipo).Brocha;

    partial void OnFechaInicioChanged(DateTimeOffset value) => NotifyDisplay();
    partial void OnDuracionMinutosChanged(int value) => NotifyDisplay();
    partial void OnTipoChanged(string value) => NotifyDisplay();
    partial void OnEstadoChanged(string value) => NotifyDisplay();
    partial void OnMedicoChanged(string? value) => NotifyDisplay();
    partial void OnTemperamentoChanged(string value) => NotifyDisplay();
    partial void OnNombrePacienteChanged(string? value) => NotifyDisplay();
    partial void OnNumeroExpedienteChanged(string? value) => NotifyDisplay();
    partial void OnTelefonoPacienteChanged(string? value)
    {
        var formateado = Helpers.TelefonoFormato.FormatearONull(value);
        if (formateado != value)
        {
            TelefonoPaciente = formateado;
            return;
        }

        NotifyDisplay();
    }

    private void NotifyDisplay()
    {
        OnPropertyChanged(nameof(FechaFin));
        OnPropertyChanged(nameof(HoraTexto));
        OnPropertyChanged(nameof(FechaTexto));
        OnPropertyChanged(nameof(HorarioTexto));
        OnPropertyChanged(nameof(TituloCorto));
        OnPropertyChanged(nameof(ChipTexto));
        OnPropertyChanged(nameof(Resumen));
        OnPropertyChanged(nameof(Subtitulo));
        OnPropertyChanged(nameof(ColorEstado));
    }
}

public sealed class EventoCalendario
{
    public bool EsOperacion { get; init; }
    public bool EsSesion { get; init; }
    public bool EsCita => Cita is not null;
    public int SesionId { get; init; }
    public Cita? Cita { get; init; }
    public Procedimiento? Procedimiento { get; init; }
    public int PacienteId { get; init; }
    public DateTimeOffset FechaInicio { get; init; }
    public int DuracionMinutos { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string? NombrePaciente { get; init; }

    public DateTimeOffset FechaFin => FechaInicio.AddMinutes(Math.Max(5, DuracionMinutos));
    public string HoraTexto => FechaInicio.ToString("HH:mm");
    public string HorarioTexto => $"{HoraTexto}–{FechaFin:HH:mm}";
    public string TituloCorto => string.IsNullOrWhiteSpace(NombrePaciente) ? Tipo : NombrePaciente!;
    public string ChipTexto => EsOperacion
        ? EsSesion
            ? $"{HoraTexto} {TituloCorto} · sesión"
            : $"{HoraTexto} {TituloCorto} · {Tipo}"
        : $"{HoraTexto} {TituloCorto}";
    public string Resumen => string.Join("  ·  ", new[]
    {
        HorarioTexto,
        EsSesion ? "Sesión" : EsOperacion ? "Operación" : null,
        Tipo,
        NombrePaciente
    }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public Brush ColorEstado => Estado is "Cancelada" or "Cancelado" or "No asistió"
        ? new SolidColorBrush(Color.FromArgb(255, 120, 120, 120))
        : EsOperacion
            ? Catalogos.TipoProcedimientoPorNombre(Tipo).Brocha
            : Catalogos.TipoCitaPorNombre(Tipo).Brocha;

    public static EventoCalendario Desde(Cita cita) => new()
    {
        EsOperacion = false,
        Cita = cita,
        PacienteId = cita.PacienteId,
        FechaInicio = cita.FechaInicio,
        DuracionMinutos = Math.Max(5, cita.DuracionMinutos),
        Tipo = cita.Tipo,
        Estado = cita.Estado,
        NombrePaciente = cita.NombrePaciente
    };

    public static EventoCalendario Desde(Procedimiento procedimiento) => new()
    {
        EsOperacion = true,
        Procedimiento = procedimiento,
        PacienteId = procedimiento.PacienteId,
        FechaInicio = procedimiento.Fecha,
        DuracionMinutos = Math.Max(5, procedimiento.DuracionMinutos ?? 60),
        Tipo = procedimiento.Tipo,
        Estado = procedimiento.Estado,
        NombrePaciente = procedimiento.NombrePaciente
    };

    public static EventoCalendario Desde(SesionOperacion sesion)
    {
        var tipo = string.IsNullOrWhiteSpace(sesion.Tipo) ? "Operación" : sesion.Tipo;
        var cancelada = sesion.Estado is "Cancelada" || sesion.EstadoOperacion is "Cancelado";
        return new()
        {
            EsOperacion = true,
            EsSesion = true,
            SesionId = sesion.Id,
            Procedimiento = new Procedimiento
            {
                Id = sesion.ProcedimientoId,
                PacienteId = sesion.PacienteId,
                Fecha = sesion.FechaInicio,
                DuracionMinutos = Math.Max(5, sesion.DuracionMinutos),
                Tipo = tipo,
                Estado = string.IsNullOrWhiteSpace(sesion.EstadoOperacion) ? "Programado" : sesion.EstadoOperacion,
                Medico = sesion.Medico,
                NombrePaciente = sesion.NombrePaciente
            },
            PacienteId = sesion.PacienteId,
            FechaInicio = sesion.FechaInicio,
            DuracionMinutos = Math.Max(5, sesion.DuracionMinutos),
            Tipo = tipo,
            Estado = cancelada ? "Cancelado" : sesion.EstadoOperacion,
            NombrePaciente = sesion.NombrePaciente
        };
    }
}

public sealed class CalendarioDiaCelda
{
    public DateTime Fecha { get; init; }
    public bool EsMesActual { get; init; }
    public bool EsHoy { get; init; }
    public bool EsSeleccionado { get; init; }
    public bool EsLaborable { get; init; }
    public List<EventoCalendario> Eventos { get; init; } = [];
    public string Numero => Fecha.Day.ToString();
    public List<EventoCalendario> CitasVisibles => Eventos.Take(4).ToList();
    public bool HayMas => Eventos.Count > 4;
    public string TextoMas => HayMas ? $"{Eventos.Count - 4} más" : string.Empty;
    public double OpacidadMes => !EsLaborable ? 0.38 : EsMesActual ? 1 : 0.5;
}

public sealed class TipoCitaOpcion
{
    public TipoCitaOpcion(string nombre, byte r, byte g, byte b)
    {
        Nombre = nombre;
        Color = Color.FromArgb(255, r, g, b);
    }

    public string Nombre { get; }
    public Color Color { get; }
    public Brush Brocha => new SolidColorBrush(Color);
    public byte R => Color.R;
    public byte G => Color.G;
    public byte B => Color.B;
    public override string ToString() => Nombre;
}

public sealed class TipoAgendaConfig
{
    public string Nombre { get; set; } = "Otro";
    public byte R { get; set; } = 154;
    public byte G { get; set; } = 140;
    public byte B { get; set; } = 118;

    public TipoCitaOpcion ToOpcion() => new(Nombre, R, G, B);
    public Microsoft.UI.Xaml.Media.Brush Brocha => ToOpcion().Brocha;

    public static TipoAgendaConfig From(TipoCitaOpcion opcion) => new()
    {
        Nombre = opcion.Nombre,
        R = opcion.Color.R,
        G = opcion.Color.G,
        B = opcion.Color.B
    };

    public static TipoAgendaConfig From(string nombre, byte r, byte g, byte b) => new()
    {
        Nombre = nombre,
        R = r,
        G = g,
        B = b
    };
}

public sealed class CalendarioColumna
{
    public DateTime Fecha { get; init; }
    public bool EsHoy { get; init; }
    public string Encabezado => Fecha.ToString("ddd d").Replace(".", string.Empty);
    public List<EventoCalendario> Eventos { get; init; } = [];
}

public sealed class CalendarioSlot
{
    public TimeSpan Hora { get; init; }
    public DateTime FechaHora { get; init; }
    public string HoraTexto => DateTime.Today.Add(Hora).ToString("HH:mm");
    public List<EventoCalendario> Citas { get; init; } = [];
    public bool HayCitas => Citas.Count > 0;
    public bool SinCitas => Citas.Count == 0;
}

public sealed class AgendaGrupo
{
    public DateTime Fecha { get; init; }
    public string Encabezado => Fecha.ToString("dddd d 'de' MMMM").Replace(".", string.Empty);
    public List<EventoCalendario> Citas { get; init; } = [];
}
