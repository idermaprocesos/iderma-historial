using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Dialogs;

public sealed partial class ProcedimientoDialog : ContentDialog
{
    public Procedimiento Procedimiento { get; private set; } = new();

    private bool _sincronizandoHora;

    public ProcedimientoDialog()
    {
        InitializeComponent();
        FechaUi.AplicarRango(FechaBox);
        TipoBox.ItemsSource = Catalogos.TiposProcedimiento;
        EstadoBox.ItemsSource = Catalogos.EstadosProcedimiento;
    }

    public void Cargar(Procedimiento? existente, int pacienteId, DateTime? fecha = null, TimeSpan? hora = null)
    {
        var settings = AppSettings.Current;
        Procedimiento = existente is null
            ? new Procedimiento
            {
                PacienteId = pacienteId,
                Fecha = FechaUi.Combinar(fecha ?? DateTime.Today, hora ?? HoraSugerida(settings)),
                DuracionMinutos = 60,
                Medico = settings.MedicoPredeterminado,
                Enfermera = settings.EnfermeraPredeterminada
            }
            : new Procedimiento
            {
                Id = existente.Id,
                PacienteId = existente.PacienteId,
                Fecha = existente.Fecha,
                Tipo = existente.Tipo,
                AreaTratada = existente.AreaTratada,
                UnidadesFoliculares = existente.UnidadesFoliculares,
                NumeroInjertos = existente.NumeroInjertos,
                Medico = existente.Medico,
                Enfermera = existente.Enfermera,
                Anestesia = existente.Anestesia,
                DuracionMinutos = existente.DuracionMinutos,
                Estado = existente.Estado,
                Resultado = existente.Resultado,
                Complicaciones = existente.Complicaciones,
                Observaciones = existente.Observaciones,
                ChecklistPasos = existente.ChecklistPasos,
                ZonasOperadas = existente.ZonasOperadas,
                Costo = existente.Costo,
                NombrePaciente = existente.NombrePaciente
            };

        Title = existente is null ? "Nuevo procedimiento / operación" : "Editar procedimiento";
        var tipos = Catalogos.TiposProcedimiento.ToList();
        TipoBox.ItemsSource = tipos;
        TipoBox.SelectedItem = tipos.Contains(Procedimiento.Tipo) ? Procedimiento.Tipo : tipos.FirstOrDefault();
        EstadoBox.SelectedItem = Procedimiento.Estado;
        FechaBox.Date = FechaUi.ParaPicker(Procedimiento.Fecha);
        AreaBox.Text = Procedimiento.AreaTratada ?? string.Empty;
        UfBox.Value = Procedimiento.UnidadesFoliculares ?? double.NaN;
        InjertosBox.Value = Procedimiento.NumeroInjertos ?? double.NaN;
        DuracionBox.Value = Procedimiento.DuracionMinutos ?? 60;
        AnestesiaBox.Text = Procedimiento.Anestesia ?? string.Empty;
        ResultadoBox.Text = Procedimiento.Resultado ?? string.Empty;
        ComplicacionesBox.Text = Procedimiento.Complicaciones ?? string.Empty;
        ObsBox.Text = Procedimiento.Observaciones ?? string.Empty;
        CostoBox.Value = Procedimiento.Costo ?? double.NaN;
        DoctorCatalog.Instance.CargarCombo(MedicoBox, Procedimiento.Medico, usarPredeterminadoSiVacio: existente is null);
        EnfermeraCatalog.Instance.CargarCombo(EnfermeraBox, Procedimiento.Enfermera, usarPredeterminadaSiVacio: existente is null);
        CargarHoras(Procedimiento.Fecha.TimeOfDay);
        ActualizarDescripcion();
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private void TipoBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ActualizarDescripcion();

    private void ActualizarDescripcion()
    {
        var tipo = TipoBox.SelectedItem as string ?? Procedimiento.Tipo;
        if (DescripcionTipoText is not null)
        {
            DescripcionTipoText.Text = Catalogos.DescripcionProcedimiento(tipo);
        }
    }

    private void CargarHoras(TimeSpan seleccion)
    {
        var horas = AppSettings.Current.HorariosCita();
        HoraBox.ItemsSource = horas.Select(h => DateTime.Today.Add(h).ToString("HH:mm")).ToList();
        _sincronizandoHora = true;
        var hora = new TimeSpan(seleccion.Hours, seleccion.Minutes, 0);
        HoraPicker.Time = hora;
        var texto = DateTime.Today.Add(hora).ToString("HH:mm");
        HoraBox.SelectedItem = HoraBox.Items.OfType<string>().Contains(texto) ? texto : null;
        _sincronizandoHora = false;
    }

    private void HoraBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_sincronizandoHora || HoraBox.SelectedItem is not string texto || !TimeSpan.TryParse(texto, out var hora))
        {
            return;
        }

        _sincronizandoHora = true;
        HoraPicker.Time = hora;
        _sincronizandoHora = false;
    }

    private void HoraPicker_TimeChanged(object sender, TimePickerValueChangedEventArgs args)
    {
        if (_sincronizandoHora)
        {
            return;
        }

        var texto = DateTime.Today.Add(new TimeSpan(args.NewTime.Hours, args.NewTime.Minutes, 0)).ToString("HH:mm");
        _sincronizandoHora = true;
        if (HoraBox.Items.OfType<string>().Contains(texto))
        {
            HoraBox.SelectedItem = texto;
        }
        else
        {
            HoraBox.SelectedItem = null;
        }

        _sincronizandoHora = false;
    }

    private void OnPrimary(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (TipoBox.SelectedItem is not string tipo)
        {
            ErrorText.Text = "Seleccione el tipo de procedimiento.";
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        var hora = HoraPicker.Time;
        if (HoraBox.SelectedItem is string horaTexto && TimeSpan.TryParse(horaTexto, out var horaLista) &&
            hora == TimeSpan.Zero && horaLista != TimeSpan.Zero)
        {
            hora = horaLista;
        }

        var fecha = FechaUi.FechaCalendario(FechaBox.Date);
        Procedimiento.Tipo = tipo;
        Procedimiento.Estado = EstadoBox.SelectedItem as string ?? "Programado";
        Procedimiento.Fecha = FechaUi.Combinar(fecha, hora);
        Procedimiento.AreaTratada = AreaBox.Text;
        Procedimiento.UnidadesFoliculares = ToInt(UfBox.Value);
        Procedimiento.NumeroInjertos = ToInt(InjertosBox.Value);
        Procedimiento.DuracionMinutos = ToInt(DuracionBox.Value) ?? 60;
        Procedimiento.Medico = MedicoBox.SelectedItem as string;
        Procedimiento.Enfermera = EnfermeraBox.SelectedItem as string;
        Procedimiento.Anestesia = AnestesiaBox.Text;
        Procedimiento.Resultado = ResultadoBox.Text;
        Procedimiento.Complicaciones = ComplicacionesBox.Text;
        Procedimiento.Observaciones = ObsBox.Text;
        Procedimiento.Costo = double.IsNaN(CostoBox.Value) ? null : CostoBox.Value;

        var bloqueo = AppDatabase.Instance.BloqueoTramo(
            Procedimiento.Id,
            Procedimiento.Medico,
            Procedimiento.Estado,
            Procedimiento.Fecha,
            Procedimiento.DuracionMinutos ?? 60,
            0,
            excluirPrincipal: true);
        if (bloqueo is not null)
        {
            ErrorText.Text = bloqueo;
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
        }
    }

    private static TimeSpan HoraSugerida(AppSettings settings)
    {
        var ahora = DateTime.Now.TimeOfDay;
        return settings.HorariosCita().FirstOrDefault(h => h >= ahora, settings.HoraInicioAgenda);
    }

    private static TimeSpan AlinearHora(TimeSpan hora)
    {
        var horas = AppSettings.Current.HorariosCita();
        return horas.Count == 0
            ? hora
            : horas.MinBy(h => Math.Abs((h - hora).TotalMinutes));
    }

    private static int? ToInt(double value) => double.IsNaN(value) ? null : (int)Math.Round(value);
}
