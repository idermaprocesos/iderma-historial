using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Dialogs;

public sealed partial class EnfermeraDialog : ContentDialog
{
    public Enfermera Enfermera { get; private set; } = new();

    public EnfermeraDialog()
    {
        InitializeComponent();
        TituloBox.ItemsSource = Catalogos.TitulosEnfermeria;
        EspecialidadBox.ItemsSource = Catalogos.EspecialidadesEnfermeria;
    }

    public void Cargar(Enfermera? existente)
    {
        Enfermera = existente is null
            ? new Enfermera()
            : new Enfermera
            {
                Id = existente.Id,
                Titulo = existente.Titulo,
                Nombres = existente.Nombres,
                Apellidos = existente.Apellidos,
                Especialidad = existente.Especialidad,
                Cep = existente.Cep,
                Telefono = existente.Telefono,
                Email = existente.Email,
                Activo = existente.Activo,
                EsPredeterminada = existente.EsPredeterminada
            };

        Title = existente is null ? "Nueva enfermera" : "Editar enfermera";
        TituloBox.SelectedItem = Enfermera.Titulo;
        NombresBox.Text = Enfermera.Nombres;
        ApellidosBox.Text = Enfermera.Apellidos;
        EspecialidadBox.SelectedItem = Enfermera.Especialidad;
        CepBox.Text = Enfermera.Cep ?? string.Empty;
        TelefonoBox.Text = Enfermera.Telefono ?? string.Empty;
        EmailBox.Text = Enfermera.Email ?? string.Empty;
        ActivoSwitch.IsOn = Enfermera.Activo;
        PredeterminadaBox.IsChecked = Enfermera.EsPredeterminada;
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private void OnPrimary(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(NombresBox.Text) || string.IsNullOrWhiteSpace(ApellidosBox.Text))
        {
            ErrorText.Text = "Nombres y apellidos son obligatorios.";
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        Enfermera.Titulo = TituloBox.SelectedItem as string ?? "Enf.";
        Enfermera.Nombres = NombresBox.Text.Trim();
        Enfermera.Apellidos = ApellidosBox.Text.Trim();
        Enfermera.Especialidad = EspecialidadBox.SelectedItem as string;
        Enfermera.Cep = CepBox.Text.Trim();
        Enfermera.Telefono = TelefonoBox.Text.Trim();
        Enfermera.Email = EmailBox.Text.Trim();
        Enfermera.Activo = ActivoSwitch.IsOn;
        Enfermera.EsPredeterminada = PredeterminadaBox.IsChecked == true;
    }

    private void Telefono_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        => TelefonoFormato.AplicarEnCaja(sender);
}
