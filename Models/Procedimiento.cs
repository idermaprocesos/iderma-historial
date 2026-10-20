using CommunityToolkit.Mvvm.ComponentModel;

namespace IdermaCapilar_Historial.Models;

public partial class Procedimiento : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private int pacienteId;
    [ObservableProperty] private DateTimeOffset fecha = DateTimeOffset.Now;
    [ObservableProperty] private string tipo = "Consulta";
    [ObservableProperty] private string? areaTratada;
    [ObservableProperty] private int? unidadesFoliculares;
    [ObservableProperty] private int? numeroInjertos;
    [ObservableProperty] private string? medico;
    [ObservableProperty] private string? enfermera;
    [ObservableProperty] private string? anestesia;
    [ObservableProperty] private int? duracionMinutos;
    [ObservableProperty] private string estado = "Programado";
    [ObservableProperty] private string? resultado;
    [ObservableProperty] private string? complicaciones;
    [ObservableProperty] private string? observaciones;
    [ObservableProperty] private string? checklistPasos;
    [ObservableProperty] private string? zonasOperadas;
    [ObservableProperty] private int numeroHistorial;
    [ObservableProperty] private double? costo;
    [ObservableProperty] private string? nombrePaciente;

    public List<SesionOperacion> Sesiones { get; set; } = [];

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy HH:mm");
    public string HoraTexto => Fecha.ToString("HH:mm");
    public string ChecklistResumen => ChecklistOperacion.Resumen(Tipo, ChecklistPasos);
    public string ZonasTexto => MapaZonasCabeza.Nombres(ZonasOperadas);
    public string DescripcionProcedimiento => Catalogos.DescripcionProcedimiento(Tipo);
    public string ObservacionesMedicoTexto =>
        string.IsNullOrWhiteSpace(Observaciones) ? "Sin observaciones del médico." : Observaciones;
    public string TituloLista => NumeroHistorial > 0
        ? $"Operación {NumeroHistorial}  ·  {Tipo}"
        : Tipo;
    public bool EsNueva => Fecha.Date >= DateTimeOffset.Now.Date.AddDays(-90);
    public string EtiquetaTiempo => EsNueva ? "Nueva" : "Antigua";
    public Microsoft.UI.Xaml.Media.Brush EstadoBrocha => Estado switch
    {
        "Realizado" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 14, 107, 107)),
        "Seguimiento" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 86, 122, 156)),
        "Cancelado" => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 140, 120, 120)),
        _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 196, 163, 90))
    };
    public string Resumen => string.Join("  ·  ", new[]
    {
        FechaTexto,
        AreaTratada,
        UnidadesFoliculares is int uf ? $"{uf} UF" : null,
        Medico,
        Enfermera
    }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public string DetalleLista
    {
        get
        {
            var partes = new List<string?>
            {
                FechaTexto,
                Estado,
                Sesiones.Count > 0 ? ResumenSesiones() : null,
                string.IsNullOrWhiteSpace(ZonasTexto) ? (string.IsNullOrWhiteSpace(AreaTratada) ? null : AreaTratada) : ZonasTexto,
                UnidadesFoliculares is int uf ? $"{uf} UF" : null,
                NumeroInjertos is int inj ? $"{inj} injertos" : null,
                string.IsNullOrWhiteSpace(Anestesia) ? null : Anestesia,
                DuracionMinutos is int min && min > 0 ? $"{min} min" : null,
                Medico,
                Enfermera,
                ChecklistResumen,
                Costo is double costo ? $"S/ {costo:N0}" : null
            };
            var nota = Recorte(Observaciones) ?? Recorte(Resultado);
            if (!string.IsNullOrWhiteSpace(nota))
            {
                partes.Add(nota);
            }

            if (!string.IsNullOrWhiteSpace(Complicaciones))
            {
                partes.Add("Complicaciones: " + Recorte(Complicaciones));
            }

            return string.Join("  ·  ", partes.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }

    public List<TramoOperacion> Tramos()
    {
        var tramos = new List<TramoOperacion>
        {
            new()
            {
                EsPrincipal = true,
                Inicio = Fecha,
                DuracionMinutos = Math.Max(5, DuracionMinutos ?? 60),
                Estado = Estado
            }
        };

        foreach (var sesion in Sesiones)
        {
            tramos.Add(new TramoOperacion
            {
                SesionId = sesion.Id,
                Inicio = sesion.FechaInicio,
                DuracionMinutos = Math.Max(5, sesion.DuracionMinutos),
                Estado = sesion.Estado,
                Notas = sesion.Notas
            });
        }

        tramos.Sort((a, b) =>
        {
            var comparacion = a.Inicio.CompareTo(b.Inicio);
            if (comparacion != 0)
            {
                return comparacion;
            }

            if (a.EsPrincipal)
            {
                return -1;
            }

            return b.EsPrincipal ? 1 : 0;
        });

        for (var i = 0; i < tramos.Count; i++)
        {
            tramos[i].Numero = i + 1;
        }

        return tramos;
    }

    private string ResumenSesiones()
    {
        var tramos = Tramos();
        var rangos = string.Join(" | ", tramos.Select(t => $"S{t.Numero} {t.Inicio:dd/MM HH:mm}–{t.Fin:HH:mm}"));
        return $"{tramos.Count} sesiones: {rangos}";
    }

    public string EstadoGlyph => Estado switch
    {
        "Realizado" => "\uE930",
        "Seguimiento" => "\uE8EC",
        "Cancelado" => "\uE711",
        _ => "\uE823"
    };

    partial void OnNumeroHistorialChanged(int value) => OnPropertyChanged(nameof(TituloLista));
    partial void OnFechaChanged(DateTimeOffset value) => NotifyDisplay();
    partial void OnNumeroInjertosChanged(int? value) => NotifyDisplay();
    partial void OnAnestesiaChanged(string? value) => NotifyDisplay();
    partial void OnDuracionMinutosChanged(int? value) => NotifyDisplay();
    partial void OnResultadoChanged(string? value) => NotifyDisplay();
    partial void OnComplicacionesChanged(string? value) => NotifyDisplay();
    partial void OnObservacionesChanged(string? value) => NotifyDisplay();
    partial void OnCostoChanged(double? value) => NotifyDisplay();
    partial void OnAreaTratadaChanged(string? value) => NotifyDisplay();
    partial void OnUnidadesFolicularesChanged(int? value) => NotifyDisplay();
    partial void OnMedicoChanged(string? value) => NotifyDisplay();
    partial void OnEnfermeraChanged(string? value) => NotifyDisplay();
    partial void OnEstadoChanged(string value) => NotifyDisplay();
    partial void OnTipoChanged(string value) => NotifyDisplay();
    partial void OnChecklistPasosChanged(string? value) => NotifyDisplay();
    partial void OnZonasOperadasChanged(string? value) => NotifyDisplay();

    private void NotifyDisplay()
    {
        OnPropertyChanged(nameof(FechaTexto));
        OnPropertyChanged(nameof(HoraTexto));
        OnPropertyChanged(nameof(Resumen));
        OnPropertyChanged(nameof(EstadoGlyph));
        OnPropertyChanged(nameof(ChecklistResumen));
        OnPropertyChanged(nameof(ZonasTexto));
        OnPropertyChanged(nameof(DescripcionProcedimiento));
        OnPropertyChanged(nameof(ObservacionesMedicoTexto));
        OnPropertyChanged(nameof(TituloLista));
        OnPropertyChanged(nameof(EsNueva));
        OnPropertyChanged(nameof(EtiquetaTiempo));
        OnPropertyChanged(nameof(EstadoBrocha));
        OnPropertyChanged(nameof(DetalleLista));
    }

    private static string? Recorte(string? texto, int max = 80)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var limpio = texto.Trim().Replace("\r", " ").Replace("\n", " ");
        return limpio.Length <= max ? limpio : limpio[..max].TrimEnd() + "…";
    }
}
