using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Dialogs;

public sealed partial class CitaDialog : ContentDialog
{
    public Cita Cita { get; private set; } = new();

    private Paciente? _paciente;
    private bool _pacienteFijo;
    private bool _cargando;

    public CitaDialog()
    {
        InitializeComponent();
        FechaUi.AplicarRango(FechaBox);
        TipoBox.ItemsSource = Catalogos.OpcionesTipoCita;
        EstadoBox.ItemsSource = Catalogos.EstadosCita;
        TemperamentoBox.ItemsSource = Catalogos.TemperamentosPaciente;
    }

    public void Cargar(Cita? existente, DateTime? fecha = null, TimeSpan? hora = null, int? pacienteId = null, int? duracionMinutos = null)
    {
        _cargando = true;
        _pacienteFijo = pacienteId is > 0;
        var settings = AppSettings.Current;
        var tipos = Catalogos.OpcionesTipoCita.ToList();

        Cita = existente is null
            ? new Cita
            {
                PacienteId = pacienteId ?? 0,
                FechaInicio = FechaUi.Combinar(fecha ?? DateTime.Today, hora ?? HoraSugerida(settings)),
                DuracionMinutos = duracionMinutos ?? settings.CitasDuracionMinutos,
                Tipo = tipos.FirstOrDefault()?.Nombre ?? "Consulta",
                Estado = "Programada",
                Temperamento = "Amable",
                Medico = settings.MedicoPredeterminado
            }
            : new Cita
            {
                Id = existente.Id,
                PacienteId = existente.PacienteId,
                FechaInicio = existente.FechaInicio,
                DuracionMinutos = existente.DuracionMinutos,
                Tipo = existente.Tipo,
                Estado = existente.Estado,
                Medico = existente.Medico,
                Notas = existente.Notas,
                Temperamento = existente.Temperamento,
                NombrePaciente = existente.NombrePaciente,
                ComprobanteTipo = existente.ComprobanteTipo,
                ComprobanteNumero = existente.ComprobanteNumero,
                ComprobanteConcepto = existente.ComprobanteConcepto,
                ComprobanteMonto = existente.ComprobanteMonto,
                ComprobanteFormaPago = existente.ComprobanteFormaPago,
                ComprobanteCliente = existente.ComprobanteCliente,
                ComprobanteDocumento = existente.ComprobanteDocumento,
                ComprobanteCorreo = existente.ComprobanteCorreo
            };

        _paciente = pacienteId is int id
            ? AppDatabase.Instance.GetPaciente(id)
            : (Cita.PacienteId > 0 ? AppDatabase.Instance.GetPaciente(Cita.PacienteId) : null);

        Title = existente is null ? "Nueva cita" : "Editar cita";
        SecondaryButtonText = existente is null ? string.Empty : "Eliminar";
        PacienteBox.Text = _paciente?.NombreCompleto ?? string.Empty;
        PacienteBox.IsEnabled = !_pacienteFijo;
        FechaBox.Date = FechaUi.ParaPicker(Cita.FechaInicio);
        TipoBox.ItemsSource = tipos;
        TipoBox.SelectedItem = tipos.FirstOrDefault(t => t.Nombre == Cita.Tipo) ?? tipos.FirstOrDefault();
        EstadoBox.SelectedItem = Catalogos.EstadosCita.Contains(Cita.Estado) ? Cita.Estado : "Programada";
        TemperamentoBox.SelectedItem = Catalogos.TemperamentosPaciente.Contains(Cita.Temperamento) ? Cita.Temperamento : "Amable";
        DuracionBox.Value = Cita.DuracionMinutos;
        NotasBox.Text = Cita.Notas ?? string.Empty;
        DoctorCatalog.Instance.CargarCombo(MedicoBox, Cita.Medico, usarPredeterminadoSiVacio: existente is null);
        CargarHoras(Cita.FechaInicio.TimeOfDay);
        Comprobante.PrepararCita = AplicarDatos;
        Comprobante.Visibility = Visibility.Collapsed;
        ComprobanteButton.Visibility = Visibility.Visible;
        ErrorText.Visibility = Visibility.Collapsed;
        _cargando = false;
    }

    private void Comprobante_Click(object sender, RoutedEventArgs e)
    {
        var error = AplicarDatos();
        if (error is not null)
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        Comprobante.Cargar(Cita);
        Comprobante.Visibility = Visibility.Visible;
        ComprobanteButton.Visibility = Visibility.Collapsed;
    }

    private void FechaBox_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        try
        {
            if (!_cargando)
            {
                CargarHoras();
            }
        }
        catch
        {
        }
    }

    private void MedicoBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_cargando)
        {
            CargarHoras();
        }
    }

    private void DuracionBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_cargando)
        {
            CargarHoras();
        }
    }

    private void CargarHoras(TimeSpan? preferida = null)
    {
        var seleccion = preferida ?? HoraSeleccionada() ?? Cita.FechaInicio.TimeOfDay;
        var fecha = FechaUi.FechaCalendario(FechaBox.Date, Cita.FechaInicio.DateTime);
        var medico = MedicoBox.SelectedItem as string;
        var duracion = double.IsNaN(DuracionBox.Value)
            ? AppSettings.Current.CitasDuracionMinutos
            : Math.Max(5, (int)DuracionBox.Value);
        var ocupadas = AppDatabase.Instance.GetOcupacionMedico(medico, fecha, Cita.Id);

        HoraBox.Items.Clear();
        ComboBoxItem? elegir = null;
        var libres = 0;
        foreach (var hora in AppSettings.Current.HorariosCita())
        {
            var inicio = FechaUi.Combinar(fecha, hora);
            var fin = inicio.AddMinutes(duracion);
            var ocupado = ocupadas.Any(o => o.Inicio < fin && o.Fin > inicio);
            if (!ocupado)
            {
                libres++;
            }

            var texto = DateTime.Today.Add(hora).ToString("HH:mm");
            var item = new ComboBoxItem
            {
                Content = ocupado ? $"{texto}  ·  ocupado" : texto,
                IsEnabled = !ocupado,
                Tag = texto,
                Opacity = ocupado ? 0.5 : 1
            };
            HoraBox.Items.Add(item);
            if (!ocupado && Math.Abs((hora - seleccion).TotalMinutes) < 1)
            {
                elegir = item;
            }
        }

        HoraBox.SelectedItem = elegir ?? HoraBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.IsEnabled);

        if (string.IsNullOrWhiteSpace(medico) || ocupadas.Count == 0)
        {
            AvisoText.Visibility = Visibility.Collapsed;
            return;
        }

        AvisoText.Text = libres == 0
            ? $"{medico} no tiene horarios libres este día."
            : "Las horas marcadas como ocupado ya están asignadas a este médico.";
        AvisoText.Visibility = Visibility.Visible;
    }

    private TimeSpan? HoraSeleccionada()
    {
        if (HoraBox.SelectedItem is ComboBoxItem { Tag: string texto } && TimeSpan.TryParse(texto, out var hora))
        {
            return hora;
        }

        if (HoraBox.SelectedItem is string plano && TimeSpan.TryParse(plano, out var horaTexto))
        {
            return horaTexto;
        }

        return null;
    }

    private void Paciente_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        var q = sender.Text?.Trim() ?? string.Empty;
        if (q.Length == 0)
        {
            sender.ItemsSource = null;
            return;
        }

        try
        {
            sender.ItemsSource = AppDatabase.Instance.BuscarPacientes(q, AppDatabase.SugerenciasPaciente)
                .Select(p => new PacienteSugerencia(p))
                .ToList();
        }
        catch
        {
            sender.ItemsSource = null;
        }
    }

    private void Paciente_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is PacienteSugerencia sugerencia)
        {
            _paciente = sugerencia.Paciente;
            sender.Text = sugerencia.Paciente.NombreCompleto;
        }
    }

    private void OnPrimary(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var error = AplicarDatos();
        if (error is not null)
        {
            MostrarError(error, args);
            return;
        }

        if (Comprobante.Visibility == Visibility.Visible)
        {
            Comprobante.AplicarEn(Cita);
        }
    }

    private string? AplicarDatos()
    {
        if (_paciente is null && !string.IsNullOrWhiteSpace(PacienteBox.Text))
        {
            var texto = PacienteBox.Text.Trim();
            _paciente = AppDatabase.Instance.BuscarPacientes(texto, 8)
                .FirstOrDefault(p => p.NombreCompleto.Equals(texto, StringComparison.CurrentCultureIgnoreCase));
        }

        if (_paciente is null)
        {
            return "Seleccione un paciente de la lista.";
        }

        if (HoraBox.SelectedItem is not ComboBoxItem { IsEnabled: true, Tag: string horaTexto } ||
            !TimeSpan.TryParse(horaTexto, out var hora))
        {
            return "Seleccione una hora disponible.";
        }

        var fecha = FechaUi.FechaCalendario(FechaBox.Date);
        Cita.PacienteId = _paciente.Id;
        Cita.NombrePaciente = _paciente.NombreCompleto;
        Cita.FechaInicio = FechaUi.Combinar(fecha, hora);
        Cita.DuracionMinutos = double.IsNaN(DuracionBox.Value) ? AppSettings.Current.CitasDuracionMinutos : (int)DuracionBox.Value;
        Cita.Tipo = TipoBox.SelectedItem is TipoCitaOpcion opcion ? opcion.Nombre : "Consulta";
        Cita.Estado = EstadoBox.SelectedItem as string ?? "Programada";
        Cita.Temperamento = TemperamentoBox.SelectedItem as string ?? "Amable";
        Cita.Medico = MedicoBox.SelectedItem as string;
        Cita.Notas = NotasBox.Text.Trim();

        if (AppDatabase.Instance.HayMedicoOcupado(Cita))
        {
            return $"El médico {Cita.Medico} ya tiene una cita u operación en ese horario.";
        }

        return null;
    }

    private void MostrarError(string mensaje, ContentDialogButtonClickEventArgs args)
    {
        ErrorText.Text = mensaje;
        ErrorText.Visibility = Visibility.Visible;
        args.Cancel = true;
    }

    private static TimeSpan HoraSugerida(AppSettings settings)
    {
        var ahora = DateTime.Now.TimeOfDay;
        return settings.HorariosCita().FirstOrDefault(h => h >= ahora, settings.HoraInicioAgenda);
    }

    private sealed record PacienteSugerencia(Paciente Paciente)
    {
        public override string ToString() => $"{Paciente.NombreCompleto}  ·  {Paciente.NumeroExpediente}";
    }
}
