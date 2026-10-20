using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Dialogs;

public sealed partial class SesionDialog : ContentDialog
{
    private Procedimiento _operacion = new();
    private bool _esPrincipal;
    private int _sesionId;

    public DateTimeOffset Inicio { get; private set; }
    public int DuracionMinutos { get; private set; }
    public string Estado { get; private set; } = "Programada";
    public string? Notas { get; private set; }

    public SesionDialog()
    {
        InitializeComponent();
        FechaUi.AplicarRango(FechaBox);
        EstadoBox.ItemsSource = new[] { "Programada", "Realizada", "Cancelada" };
        EstadoBox.SelectedIndex = 0;
    }

    public void CargarNueva(Procedimiento operacion)
    {
        var ultimo = operacion.Tramos().OrderBy(t => t.Inicio).LastOrDefault();
        var baseInicio = ultimo?.Inicio ?? operacion.Fecha;
        var duracion = Math.Max(30, ultimo?.DuracionMinutos ?? operacion.DuracionMinutos ?? 60);
        var inicio = baseInicio.AddDays(7);
        Cargar(operacion, esPrincipal: false, sesionId: 0, inicio, duracion, "Programada", null, "Programar sesión");
    }

    public void Cargar(TramoOperacion tramo, Procedimiento operacion)
    {
        Cargar(
            operacion,
            tramo.EsPrincipal,
            tramo.SesionId,
            tramo.Inicio,
            tramo.DuracionMinutos,
            tramo.EsPrincipal ? operacion.Estado : tramo.Estado,
            tramo.Notas,
            tramo.Titulo);
    }

    private void Cargar(
        Procedimiento operacion,
        bool esPrincipal,
        int sesionId,
        DateTimeOffset inicio,
        int duracion,
        string estado,
        string? notas,
        string titulo)
    {
        _operacion = operacion;
        _esPrincipal = esPrincipal;
        _sesionId = sesionId;
        Title = titulo;
        FechaBox.Date = FechaUi.ParaPicker(inicio);
        InicioPicker.Time = new TimeSpan(inicio.Hour, inicio.Minute, 0);
        var fin = inicio.AddMinutes(Math.Max(5, duracion));
        FinPicker.Time = fin.Date > inicio.Date
            ? new TimeSpan(23, 55, 0)
            : new TimeSpan(fin.Hour, fin.Minute, 0);
        EstadoBox.SelectedItem = estado is "Realizada" or "Cancelada" ? estado : "Programada";
        NotasBox.Text = notas ?? string.Empty;
        EstadoPanel.Visibility = esPrincipal ? Visibility.Collapsed : Visibility.Visible;
        NotasPanel.Visibility = esPrincipal ? Visibility.Collapsed : Visibility.Visible;
        ErrorText.Visibility = Visibility.Collapsed;
        ActualizarRango();
    }

    private void Horario_Changed(object sender, TimePickerValueChangedEventArgs e)
        => ActualizarRango();

    private void ActualizarRango()
    {
        if (RangoText is null || !TryLeer(out var inicio, out var fin, out _))
        {
            if (RangoText is not null)
            {
                RangoText.Text = "La hora de fin tiene que ser posterior a la de inicio, el mismo día.";
            }

            return;
        }

        var minutos = (int)Math.Round((fin - inicio).TotalMinutes);
        RangoText.Text = $"{inicio:HH:mm}–{fin:HH:mm}  ·  {TramoOperacion.FormatoDuracion(minutos)}";
    }

    private void OnPrimary(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!TryLeer(out var inicio, out var fin, out var error))
        {
            Mostrar(error ?? "Revise el horario.", args);
            return;
        }

        var duracion = (int)Math.Round((fin - inicio).TotalMinutes);
        if (duracion > 14 * 60)
        {
            Mostrar("Una sesión no puede pasar de 14 horas. Programe otra sesión para el resto.", args);
            return;
        }

        var estado = _esPrincipal
            ? _operacion.Estado
            : EstadoBox.SelectedItem as string ?? "Programada";
        var bloqueo = AppDatabase.Instance.BloqueoTramo(
            _operacion.Id,
            _operacion.Medico,
            estado,
            inicio,
            duracion,
            _esPrincipal ? 0 : _sesionId,
            _esPrincipal);
        if (bloqueo is not null)
        {
            Mostrar(bloqueo, args);
            return;
        }

        Inicio = inicio;
        DuracionMinutos = duracion;
        Estado = estado;
        var notas = NotasBox.Text?.Trim();
        Notas = string.IsNullOrWhiteSpace(notas) ? null : notas;
    }

    private bool TryLeer(out DateTimeOffset inicio, out DateTimeOffset fin, out string? error)
    {
        if (FechaBox is null || InicioPicker is null || FinPicker is null)
        {
            inicio = default;
            fin = default;
            error = null;
            return false;
        }

        var fecha = FechaUi.FechaCalendario(FechaBox.Date);
        inicio = FechaUi.Combinar(fecha, InicioPicker.Time);
        fin = FechaUi.Combinar(fecha, FinPicker.Time);
        if (fin <= inicio)
        {
            error = "La hora de fin tiene que ser posterior a la de inicio.";
            return false;
        }

        error = null;
        return true;
    }

    private void Mostrar(string mensaje, ContentDialogButtonClickEventArgs args)
    {
        ErrorText.Text = mensaje;
        ErrorText.Visibility = Visibility.Visible;
        args.Cancel = true;
    }
}
