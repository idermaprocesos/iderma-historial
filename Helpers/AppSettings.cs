using System.Text.Json;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;

namespace IdermaCapilar_Historial.Helpers;

public sealed class AppSettings
{
    public string NombreClinica { get; set; } = "Iderma Capilar";
    public string RucClinica { get; set; } = string.Empty;
    public string DireccionClinica { get; set; } = string.Empty;
    public string TelefonoClinica { get; set; } = string.Empty;
    public string CorreoClinica { get; set; } = string.Empty;
    public string SerieBoleta { get; set; } = "B001";
    public string SerieFactura { get; set; } = "F001";
    public int CorrelativoBoleta { get; set; } = 1;
    public int CorrelativoFactura { get; set; } = 1;
    public string MedicoPredeterminado { get; set; } = string.Empty;
    public string EnfermeraPredeterminada { get; set; } = string.Empty;
    public string Tema { get; set; } = "Default";
    public bool Autoguardado { get; set; } = true;
    public int AutoguardadoMinutos { get; set; } = 15;
    public string CitasHoraInicio { get; set; } = "08:00";
    public string CitasHoraFin { get; set; } = "19:00";
    public int CitasDuracionMinutos { get; set; } = 30;
    public int CitasIntervaloMinutos { get; set; } = 30;
    public string CitasVistaPredeterminada { get; set; } = "Mes";
    public bool CitasAtenderSabado { get; set; } = true;
    public bool CitasAtenderDomingo { get; set; } = false;
    public List<TipoAgendaConfig> MotivosCita { get; set; } = [];
    public List<TipoAgendaConfig> MotivosOperacion { get; set; } = [];

    public TimeSpan HoraInicioAgenda => ParseHora(CitasHoraInicio, new TimeSpan(8, 0, 0));
    public TimeSpan HoraFinAgenda => ParseHora(CitasHoraFin, new TimeSpan(19, 0, 0));

    public bool EsDiaLaborable(DateTime fecha) => fecha.DayOfWeek switch
    {
        DayOfWeek.Saturday => CitasAtenderSabado,
        DayOfWeek.Sunday => CitasAtenderDomingo,
        _ => true
    };

    public IReadOnlyList<TimeSpan> HorariosCita()
    {
        var inicio = HoraInicioAgenda;
        var fin = HoraFinAgenda;
        if (fin <= inicio)
        {
            fin = inicio.Add(TimeSpan.FromHours(8));
        }

        var intervalo = Math.Clamp(CitasIntervaloMinutos, 5, 120);
        var horas = new List<TimeSpan>();
        for (var hora = inicio; hora < fin; hora = hora.Add(TimeSpan.FromMinutes(intervalo)))
        {
            horas.Add(hora);
        }

        return horas;
    }

    private static TimeSpan ParseHora(string? texto, TimeSpan respaldo)
        => TimeSpan.TryParse(texto, out var hora) ? hora : respaldo;

    public static AppSettings Current { get; private set; } = Load();

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IdermaCapilar",
        "Historial",
        "ajustes.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                Current.AsegurarMotivos();
                if (Current.AutoguardadoMinutos < 10)
                {
                    Current.AutoguardadoMinutos = 15;
                }

                return Current;
            }
        }
        catch
        {
            // Se usan valores por defecto si el archivo está dañado.
        }

        Current = new AppSettings();
        Current.AsegurarMotivos();
        return Current;
    }

    public void AsegurarMotivos()
    {
        MotivosCita ??= [];
        MotivosOperacion ??= [];
        if (MotivosCita.Count == 0)
        {
            MotivosCita = Catalogos.MotivosCitaPredeterminados.Select(TipoAgendaConfig.From).ToList();
        }

        if (MotivosOperacion.Count == 0)
        {
            MotivosOperacion = Catalogos.MotivosOperacionPredeterminados.Select(TipoAgendaConfig.From).ToList();
        }
        else
        {
            var existentes = MotivosOperacion
                .Select(m => m.Nombre)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var pred in Catalogos.MotivosOperacionPredeterminados)
            {
                if (Catalogos.MotivosOperacionCorporal.Contains(pred.Nombre, StringComparer.OrdinalIgnoreCase)
                    && !existentes.Contains(pred.Nombre))
                {
                    MotivosOperacion.Add(TipoAgendaConfig.From(pred));
                    existentes.Add(pred.Nombre);
                }
            }
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            AsegurarMotivos();
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            var temporal = FilePath + ".tmp";
            File.WriteAllText(temporal, json);
            File.Copy(temporal, FilePath, overwrite: true);
            File.Delete(temporal);
            Current = this;
        }
        catch
        {
            // No se cierra la aplicación si no se pueden guardar los ajustes.
        }
    }

    public ElementTheme ToTheme() => Tema switch
    {
        "Light" => ElementTheme.Light,
        "Dark" => ElementTheme.Dark,
        _ => ElementTheme.Default
    };
}
