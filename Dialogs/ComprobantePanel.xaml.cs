using IdermaCapilar_Historial.Data;
using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Dialogs;

public sealed partial class ComprobantePanel : UserControl
{
    private static readonly string[] Tipos = ["Boleta", "Factura"];
    private static readonly string[] FormasPago = ["Efectivo", "Tarjeta", "Transferencia", "Yape", "Plin"];

    private Cita _cita = new();
    private Paciente? _paciente;
    private string _conceptoAuto = string.Empty;
    private bool _cargando;
    private bool _ocupado;

    public Func<string?>? PrepararCita { get; set; }

    public ComprobantePanel()
    {
        InitializeComponent();
        TipoBox.ItemsSource = Tipos;
        PagoBox.ItemsSource = FormasPago;
    }

    public void Cargar(Cita cita)
    {
        _cargando = true;
        _cita = cita;
        _paciente = cita.PacienteId > 0 ? AppDatabase.Instance.GetPaciente(cita.PacienteId) : null;
        var factura = string.Equals(cita.ComprobanteTipo, "Factura", StringComparison.OrdinalIgnoreCase);
        TipoBox.SelectedItem = factura ? "Factura" : "Boleta";
        _conceptoAuto = string.IsNullOrWhiteSpace(cita.Tipo) ? "Consulta" : cita.Tipo.Trim();
        ConceptoBox.Text = string.IsNullOrWhiteSpace(cita.ComprobanteConcepto) ? _conceptoAuto : cita.ComprobanteConcepto;
        MontoBox.Value = cita.ComprobanteMonto ?? double.NaN;
        PagoBox.SelectedItem = FormasPago.Contains(cita.ComprobanteFormaPago) ? cita.ComprobanteFormaPago : "Efectivo";
        ClienteBox.Text = cita.ComprobanteCliente
            ?? _paciente?.NombreCompleto
            ?? cita.NombrePaciente
            ?? string.Empty;
        DocumentoBox.Text = cita.ComprobanteDocumento ?? _paciente?.NumeroDocumento ?? string.Empty;
        CorreoBox.Text = cita.ComprobanteCorreo ?? _paciente?.Email ?? string.Empty;
        ActualizarEtiquetas();
        CitaResumen.Text = ResumenCita();
        OcultarAvisos();
        _cargando = false;
    }

    public void AplicarEn(Cita cita)
    {
        if (!ReferenceEquals(cita, _cita))
        {
            _cita = cita;
        }

        Leer(mostrarErrores: false);
    }

    public static async Task MostrarAsync(XamlRoot? root, Cita cita)
    {
        if (root is null)
        {
            return;
        }

        var panel = new ComprobantePanel();
        panel.Cargar(cita);
        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Title = string.IsNullOrWhiteSpace(cita.NombrePaciente) ? "Boleta o factura" : $"Boleta o factura · {cita.NombrePaciente}",
            CloseButtonText = "Cerrar",
            Content = new ScrollViewer
            {
                MaxHeight = 560,
                Content = panel
            }
        };
        await dialog.ShowAsync();
    }

    private void TipoBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando)
        {
            return;
        }

        ActualizarEtiquetas();
    }

    private void Generar_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, GenerarAsync, "No se pudo generar el comprobante");

    private void Enviar_Click(object sender, RoutedEventArgs e)
        => UiSafe.Run(XamlRoot, EnviarAsync, "No se pudo enviar el comprobante");

    private async Task GenerarAsync()
    {
        if (_ocupado || !Preparar())
        {
            return;
        }

        _ocupado = true;
        GenerarButton.IsEnabled = false;
        EnviarButton.IsEnabled = false;
        try
        {
            await ImpresionService.EmitirComprobanteAsync(_cita, _paciente!, abrir: true);
            MostrarInfo("Se abrió el comprobante. Puede imprimirlo o enviarlo por correo desde el documento.");
        }
        finally
        {
            _ocupado = false;
            GenerarButton.IsEnabled = true;
            EnviarButton.IsEnabled = true;
        }
    }

    private async Task EnviarAsync()
    {
        if (_ocupado || !Preparar())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(CorreoBox.Text))
        {
            MostrarError("Indique el correo para enviar el comprobante.");
            return;
        }

        _ocupado = true;
        GenerarButton.IsEnabled = false;
        EnviarButton.IsEnabled = false;
        try
        {
            var ruta = await ImpresionService.EmitirComprobanteAsync(_cita, _paciente!, abrir: false);
            var resultado = CorreoService.Enviar(
                _cita.ComprobanteCorreo,
                ImpresionService.AsuntoComprobante(_cita),
                ImpresionService.CuerpoCorreoComprobante(_cita, _paciente!),
                ruta);
            switch (resultado)
            {
                case ResultadoCorreo.AbiertoConAdjunto:
                    MostrarInfo("Se abrió el correo con el comprobante adjunto. Revíselo y envíelo.");
                    break;
                case ResultadoCorreo.Cancelado:
                    MostrarInfo("Se canceló el envío. El comprobante ya quedó generado.");
                    break;
                case ResultadoCorreo.AbiertoSinAdjunto:
                    MostrarError($"Se abrió el correo sin el archivo adjunto. El comprobante está en: {ruta}");
                    break;
                default:
                    MostrarError($"No se pudo abrir el correo. El comprobante está en: {ruta}");
                    break;
            }
        }
        finally
        {
            _ocupado = false;
            GenerarButton.IsEnabled = true;
            EnviarButton.IsEnabled = true;
        }
    }

    private bool Preparar()
    {
        OcultarAvisos();
        var errorCita = PrepararCita?.Invoke();
        if (errorCita is not null)
        {
            MostrarError(errorCita);
            return false;
        }

        _paciente = _cita.PacienteId > 0 ? AppDatabase.Instance.GetPaciente(_cita.PacienteId) : _paciente;
        if (_paciente is null)
        {
            MostrarError("Seleccione un paciente antes de emitir el comprobante.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(ConceptoBox.Text) || ConceptoBox.Text.Trim() == _conceptoAuto)
        {
            var tipo = string.IsNullOrWhiteSpace(_cita.Tipo) ? "Consulta" : _cita.Tipo.Trim();
            _conceptoAuto = tipo;
            if (string.IsNullOrWhiteSpace(ConceptoBox.Text) || ConceptoBox.Text.Trim() == _conceptoAuto || ConceptoBox.Text.Trim() == tipo)
            {
                ConceptoBox.Text = tipo;
            }
        }

        CitaResumen.Text = ResumenCita();
        return Leer(mostrarErrores: true);
    }

    private bool Leer(bool mostrarErrores)
    {
        var factura = TipoBox.SelectedItem as string == "Factura";
        var concepto = ConceptoBox.Text.Trim();
        var cliente = ClienteBox.Text.Trim();
        var documento = new string((DocumentoBox.Text ?? string.Empty).Where(char.IsDigit).ToArray());
        var monto = MontoBox.Value;
        if (string.IsNullOrWhiteSpace(concepto))
        {
            if (mostrarErrores)
            {
                MostrarError("Escriba lo que está pagando.");
            }

            return false;
        }

        if (double.IsNaN(monto) || monto <= 0)
        {
            if (mostrarErrores)
            {
                MostrarError("Indique un monto mayor que cero.");
            }

            return false;
        }

        if (string.IsNullOrWhiteSpace(cliente))
        {
            if (mostrarErrores)
            {
                MostrarError(factura ? "Escriba la razón social." : "Escriba el nombre del cliente.");
            }

            return false;
        }

        if (factura && documento.Length != 11)
        {
            if (mostrarErrores)
            {
                MostrarError("La factura necesita el RUC de 11 dígitos.");
            }

            return false;
        }

        _cita.ComprobanteTipo = factura ? "Factura" : "Boleta";
        _cita.ComprobanteConcepto = concepto;
        _cita.ComprobanteMonto = Math.Round(monto, 2);
        _cita.ComprobanteFormaPago = PagoBox.SelectedItem as string ?? "Efectivo";
        _cita.ComprobanteCliente = cliente;
        _cita.ComprobanteDocumento = string.IsNullOrWhiteSpace(documento) ? DocumentoBox.Text.Trim() : documento;
        _cita.ComprobanteCorreo = string.IsNullOrWhiteSpace(CorreoBox.Text) ? null : CorreoBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(_cita.NombrePaciente))
        {
            _cita.NombrePaciente = _paciente?.NombreCompleto;
        }

        return true;
    }

    private void ActualizarEtiquetas()
    {
        var factura = TipoBox.SelectedItem as string == "Factura";
        ClienteBox.Header = factura ? "Razón social *" : "Cliente *";
        DocumentoBox.Header = factura ? "RUC *" : "DNI";
        DocumentoBox.PlaceholderText = factura ? "11 dígitos" : "Documento del paciente";
        DocumentoBox.MaxLength = factura ? 11 : 15;
    }

    private string ResumenCita()
        => string.Join("  ·  ", new[]
        {
            _cita.FechaTexto,
            _cita.HorarioTexto,
            _cita.Tipo,
            _cita.Medico
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private void MostrarError(string mensaje)
    {
        InfoText.Visibility = Visibility.Collapsed;
        ErrorText.Text = mensaje;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void MostrarInfo(string mensaje)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        InfoText.Text = mensaje;
        InfoText.Visibility = Visibility.Visible;
    }

    private void OcultarAvisos()
    {
        ErrorText.Visibility = Visibility.Collapsed;
        InfoText.Visibility = Visibility.Collapsed;
    }
}
