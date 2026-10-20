using System.Globalization;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Helpers;

internal static class SqliteDate
{
    public const string Format = "yyyy-MM-dd HH:mm:ss";

    public static string FormatValue(DateTimeOffset value)
        => Reloj(value).ToString(Format, CultureInfo.InvariantCulture);

    public static string FormatValue(DateTime value)
        => new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second)
            .ToString(Format, CultureInfo.InvariantCulture);

    public static object FormatOrDbNull(DateTimeOffset? value)
        => value is null ? DBNull.Value : FormatValue(value.Value);

    public static DateTime Reloj(DateTimeOffset value)
        => new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second);

    public static DateTimeOffset ComoLocal(DateTime reloj)
    {
        try
        {
            var local = DateTime.SpecifyKind(
                new DateTime(reloj.Year, reloj.Month, reloj.Day, reloj.Hour, reloj.Minute, reloj.Second),
                DateTimeKind.Unspecified);
            return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
        }
        catch
        {
            return DateTimeOffset.Now;
        }
    }

    public static DateTimeOffset? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            string[] formatos =
            [
                Format,
                "yyyy-MM-dd",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
                "yyyy-MM-dd HH:mm:ss.FFFFFFF"
            ];
            if (DateTime.TryParseExact(raw, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exacta))
            {
                return ComoLocal(exacta);
            }

            if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var conZona))
            {
                return ComoLocal(Reloj(conZona));
            }

            return DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out var fecha)
                ? ComoLocal(fecha)
                : null;
        }
        catch
        {
            return null;
        }
    }
}

internal static class FechaUi
{
    public static readonly DateTimeOffset MinCalendario = new(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset MaxCalendario = new(2199, 12, 31, 0, 0, 0, TimeSpan.Zero);

    public static DateTime FechaCalendario(DateTimeOffset? picker, DateTime? respaldo = null)
    {
        if (picker is DateTimeOffset valor)
        {
            return new DateTime(valor.Year, valor.Month, valor.Day);
        }

        return (respaldo ?? DateTime.Today).Date;
    }

    public static bool MismaFechaCalendario(DateTimeOffset? a, DateTimeOffset? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Value.Year == b.Value.Year && a.Value.Month == b.Value.Month && a.Value.Day == b.Value.Day;
    }

    public static DateTimeOffset Combinar(DateTimeOffset? picker, TimeSpan hora)
        => Combinar(FechaCalendario(picker), hora);

    public static DateTimeOffset Combinar(DateTime fecha, TimeSpan hora)
        => SqliteDate.ComoLocal(fecha.Date.Add(hora));

    public static void AplicarRango(CalendarDatePicker picker)
    {
        try
        {
            picker.MinDate = MinCalendario;
            picker.MaxDate = MaxCalendario;
        }
        catch
        {
        }
    }

    public static DateTimeOffset? ParaPicker(DateTimeOffset? valor)
    {
        if (valor is null)
        {
            return null;
        }

        try
        {
            var v = valor.Value;
            var utc = new DateTimeOffset(v.Year, v.Month, v.Day, 0, 0, 0, TimeSpan.Zero);
            if (utc < MinCalendario)
            {
                return MinCalendario;
            }

            if (utc > MaxCalendario)
            {
                return MaxCalendario;
            }

            return utc;
        }
        catch
        {
            return DateTimeOffset.Now.Date;
        }
    }

    public static void Asignar(CalendarDatePicker picker, DateTimeOffset? valor)
    {
        try
        {
            AplicarRango(picker);
            picker.Date = ParaPicker(valor);
        }
        catch
        {
            try
            {
                picker.Date = null;
            }
            catch
            {
            }
        }
    }

    public static DateTimeOffset? DesdePicker(DateTimeOffset? picker)
    {
        if (picker is null)
        {
            return null;
        }

        try
        {
            return SqliteDate.ComoLocal(FechaCalendario(picker));
        }
        catch
        {
            return null;
        }
    }
}
