using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;

namespace IdermaCapilar_Historial.ViewModels;

public partial class CitasViewModel : ObservableObject
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");

    [ObservableProperty] private DateTime fechaReferencia = DateTime.Today;
    [ObservableProperty] private string vista = "Mes";
    [ObservableProperty] private string tituloPeriodo = string.Empty;
    [ObservableProperty] private string resumen = string.Empty;
    [ObservableProperty] private CalendarioDiaCelda? diaSeleccionado;

    public ObservableCollection<string> NombresDias { get; } = [];
    public ObservableCollection<CalendarioDiaCelda> DiasMes { get; } = [];
    public ObservableCollection<CalendarioColumna> Semana { get; } = [];
    public ObservableCollection<CalendarioSlot> SlotsDia { get; } = [];
    public ObservableCollection<AgendaGrupo> Agenda { get; } = [];
    public ObservableCollection<EventoCalendario> EventosDia { get; } = [];

    public bool EsVistaMes => Vista == "Mes";
    public bool EsVistaSemana => Vista == "Semana";
    public bool EsVistaDia => Vista == "Día";
    public bool EsVistaAgenda => Vista == "Agenda";
    public bool HayCitasDia => EventosDia.Count > 0;
    public bool SinCitasDia => EventosDia.Count == 0;
    public bool HayAgenda => Agenda.Count > 0;
    public bool SinAgenda => Agenda.Count == 0;

    public void Inicializar()
    {
        Vista = AppSettings.Current.CitasVistaPredeterminada;
        if (!Catalogos.VistasCalendario.Contains(Vista))
        {
            Vista = "Mes";
        }

        Recargar();
    }

    public void CambiarVista(string nueva)
    {
        Vista = nueva;
        Recargar();
    }

    public void IrHoy()
    {
        FechaReferencia = DateTime.Today;
        Recargar();
    }

    public void Anterior()
    {
        FechaReferencia = Vista switch
        {
            "Día" => FechaReferencia.AddDays(-1),
            "Semana" => FechaReferencia.AddDays(-7),
            "Agenda" => FechaReferencia.AddDays(-14),
            _ => FechaReferencia.AddMonths(-1)
        };
        Recargar();
    }

    public void Siguiente()
    {
        FechaReferencia = Vista switch
        {
            "Día" => FechaReferencia.AddDays(1),
            "Semana" => FechaReferencia.AddDays(7),
            "Agenda" => FechaReferencia.AddDays(14),
            _ => FechaReferencia.AddMonths(1)
        };
        Recargar();
    }

    public void IrA(DateTime fecha)
    {
        FechaReferencia = fecha.Date;
        Recargar();
    }

    public void Recargar()
    {
        var fecha = FechaReferencia.Date;
        NombresDias.Clear();
        foreach (var nombre in new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" })
        {
            NombresDias.Add(nombre);
        }

        switch (Vista)
        {
            case "Día":
                CargarDia(fecha);
                TituloPeriodo = Capitalizar(fecha.ToString("dddd d 'de' MMMM yyyy", Cultura));
                break;
            case "Semana":
                CargarSemana(fecha);
                var lunes = InicioSemana(fecha);
                TituloPeriodo = $"{lunes.ToString("d MMM", Cultura)} – {lunes.AddDays(6).ToString("d MMM yyyy", Cultura)}";
                break;
            case "Agenda":
                CargarAgenda(fecha);
                TituloPeriodo = $"Desde {fecha.ToString("d 'de' MMMM", Cultura)}";
                break;
            default:
                CargarMes(fecha);
                TituloPeriodo = Capitalizar(fecha.ToString("MMMM yyyy", Cultura));
                break;
        }

        OnPropertyChanged(nameof(EsVistaMes));
        OnPropertyChanged(nameof(EsVistaSemana));
        OnPropertyChanged(nameof(EsVistaDia));
        OnPropertyChanged(nameof(EsVistaAgenda));
        OnPropertyChanged(nameof(HayCitasDia));
        OnPropertyChanged(nameof(SinCitasDia));
        OnPropertyChanged(nameof(HayAgenda));
        OnPropertyChanged(nameof(SinAgenda));
    }

    private void CargarMes(DateTime fecha)
    {
        var inicioMes = new DateTime(fecha.Year, fecha.Month, 1);
        var primerCelda = InicioSemana(inicioMes);
        var ultimoMes = inicioMes.AddMonths(1).AddDays(-1);
        var ultimaCelda = InicioSemana(ultimoMes).AddDays(6);
        var total = (ultimaCelda - primerCelda).Days + 1;
        var eventos = EventosEnRango(primerCelda, ultimaCelda.AddDays(1));
        var agrupadas = eventos
            .GroupBy(c => c.FechaInicio.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        DiasMes.Clear();
        for (var i = 0; i < total; i++)
        {
            var dia = primerCelda.AddDays(i);
            agrupadas.TryGetValue(dia, out var delDia);
            var celda = new CalendarioDiaCelda
            {
                Fecha = dia,
                EsMesActual = dia.Month == fecha.Month,
                EsHoy = dia == DateTime.Today,
                EsSeleccionado = dia == fecha,
                EsLaborable = AppSettings.Current.EsDiaLaborable(dia),
                Eventos = delDia ?? []
            };
            DiasMes.Add(celda);
        }

        DiaSeleccionado = DiasMes.FirstOrDefault(d => d.Fecha == fecha);
        var delMes = eventos.Where(c => c.FechaInicio.Month == fecha.Month && c.FechaInicio.Year == fecha.Year).ToList();
        Resumen = ResumenDe(delMes, "en el mes");
    }

    private void CargarSemana(DateTime fecha)
    {
        var lunes = InicioSemana(fecha);
        var eventos = EventosEnRango(lunes, lunes.AddDays(7));
        var agrupadas = eventos
            .GroupBy(c => c.FechaInicio.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        Semana.Clear();
        for (var i = 0; i < 7; i++)
        {
            var dia = lunes.AddDays(i);
            agrupadas.TryGetValue(dia, out var delDia);
            Semana.Add(new CalendarioColumna
            {
                Fecha = dia,
                EsHoy = dia == DateTime.Today,
                Eventos = delDia ?? []
            });
        }

        Resumen = ResumenDe(eventos, "esta semana");
    }

    private void CargarDia(DateTime fecha)
    {
        var settings = AppSettings.Current;
        var eventos = EventosEnRango(fecha, fecha.AddDays(1));
        EventosDia.Clear();
        foreach (var evento in eventos)
        {
            EventosDia.Add(evento);
        }

        SlotsDia.Clear();
        foreach (var hora in settings.HorariosCita())
        {
            var finHora = hora.Add(TimeSpan.FromMinutes(Math.Max(5, settings.CitasIntervaloMinutos)));
            SlotsDia.Add(new CalendarioSlot
            {
                Hora = hora,
                FechaHora = fecha.Add(hora),
                Citas = eventos
                    .Where(c =>
                    {
                        var t = HoraVisible(c.FechaInicio.TimeOfDay, hora);
                        return t >= hora && t < finHora;
                    })
                    .ToList()
            });
        }

        Resumen = eventos.Count == 0
            ? "Sin citas ni operaciones este día"
            : ResumenDe(eventos, "este día");
    }

    private void CargarAgenda(DateTime fecha)
    {
        var eventos = EventosEnRango(fecha, fecha.AddDays(45))
            .Where(c => c.Estado is not "Cancelada" and not "Cancelado")
            .ToList();
        Agenda.Clear();
        foreach (var grupo in eventos.GroupBy(c => c.FechaInicio.Date))
        {
            Agenda.Add(new AgendaGrupo
            {
                Fecha = grupo.Key,
                Citas = grupo.ToList()
            });
        }

        Resumen = eventos.Count == 0
            ? "No hay citas ni operaciones en los próximos 45 días"
            : ResumenDe(eventos, "próximas");
    }

    private static List<EventoCalendario> EventosEnRango(DateTime inicio, DateTime fin)
        => AppDatabase.Instance.GetCitasEnRango(inicio, fin).Select(EventoCalendario.Desde)
            .Concat(AppDatabase.Instance.GetProcedimientosEnRango(inicio, fin).Select(EventoCalendario.Desde))
            .Concat(AppDatabase.Instance.GetSesionesEnRango(inicio, fin).Select(EventoCalendario.Desde))
            .OrderBy(e => e.FechaInicio)
            .ThenBy(e => e.NombrePaciente)
            .ToList();

    private static TimeSpan HoraVisible(TimeSpan hora, TimeSpan primeraHora)
        => hora == TimeSpan.Zero ? primeraHora : hora;

    private static string ResumenDe(IReadOnlyList<EventoCalendario> eventos, string periodo)
        => TextoResumen(
            eventos.Count(e => !e.EsOperacion),
            eventos.Count(e => e.EsOperacion && !e.EsSesion),
            eventos.Count(e => e.EsSesion),
            periodo);

    private static string TextoResumen(int citas, int operaciones, int sesiones, string periodo)
    {
        var partes = new List<string>();
        if (citas > 0)
        {
            partes.Add($"{citas} cita{(citas == 1 ? string.Empty : "s")}");
        }

        if (operaciones > 0)
        {
            partes.Add($"{operaciones} operaci{(operaciones == 1 ? "ón" : "ones")}");
        }

        if (sesiones > 0)
        {
            partes.Add($"{sesiones} sesi{(sesiones == 1 ? "ón" : "ones")}");
        }

        return partes.Count == 0 ? $"Sin agenda {periodo}" : $"{string.Join(" y ", partes)} {periodo}";
    }

    private static DateTime InicioSemana(DateTime fecha)
    {
        var delta = ((int)fecha.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return fecha.Date.AddDays(-delta);
    }

    private static string Capitalizar(string texto)
        => string.IsNullOrWhiteSpace(texto) ? texto : char.ToUpper(texto[0], Cultura) + texto[1..];
}
