namespace IdermaCapilar_Historial.Models;

public sealed class SesionOperacion
{
    public int Id { get; set; }
    public int ProcedimientoId { get; set; }
    public DateTimeOffset FechaInicio { get; set; }
    public int DuracionMinutos { get; set; } = 60;
    public string Estado { get; set; } = "Programada";
    public string? Notas { get; set; }
    public int PacienteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string? Medico { get; set; }
    public string? NombrePaciente { get; set; }
    public string EstadoOperacion { get; set; } = "Programado";

    public DateTimeOffset FechaFin => FechaInicio.AddMinutes(Math.Max(5, DuracionMinutos));
}

public sealed class TramoOperacion
{
    public bool EsPrincipal { get; init; }
    public int SesionId { get; init; }
    public DateTimeOffset Inicio { get; init; }
    public int DuracionMinutos { get; init; }
    public string Estado { get; init; } = "Programada";
    public string? Notas { get; init; }
    public int Numero { get; set; }

    public DateTimeOffset Fin => Inicio.AddMinutes(Math.Max(5, DuracionMinutos));
    public string Titulo => $"Sesión {Numero}";
    public string RangoTexto => $"{Inicio:dd/MM/yyyy}  {Inicio:HH:mm}–{Fin:HH:mm}";
    public string DuracionTexto => FormatoDuracion(DuracionMinutos);

    public static string FormatoDuracion(int minutos)
    {
        minutos = Math.Max(0, minutos);
        if (minutos < 60)
        {
            return $"{minutos} min";
        }

        var horas = minutos / 60;
        var resto = minutos % 60;
        return resto == 0 ? $"{horas} h" : $"{horas} h {resto} min";
    }
}
