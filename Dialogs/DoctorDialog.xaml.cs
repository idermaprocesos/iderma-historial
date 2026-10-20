using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Dialogs;

public sealed partial class DoctorDialog : ContentDialog
{
    public Doctor Doctor { get; private set; } = new();

    public DoctorDialog()
    {
        InitializeComponent();
        TituloBox.ItemsSource = Catalogos.TitulosMedicos;
        EspecialidadBox.ItemsSource = Catalogos.EspecialidadesMedicas;
    }

    public void Cargar(Doctor? existente)
    {
        Doctor = existente is null
            ? new Doctor()
            : new Doctor
            {
                Id = existente.Id,
                Titulo = existente.Titulo,
                Nombres = existente.Nombres,
                Apellidos = existente.Apellidos,
                Especialidad = existente.Especialidad,
                Cmp = existente.Cmp,
                Telefono = existente.Telefono,
                Email = existente.Email,
                Activo = existente.Activo,
                EsPredeterminado = existente.EsPredeterminado
            };

        Title = existente is null ? "Nuevo doctor" : "Editar doctor";
        TituloBox.SelectedItem = Doctor.Titulo;
        NombresBox.Text = Doctor.Nombres;
        ApellidosBox.Text = Doctor.Apellidos;
        EspecialidadBox.SelectedItem = Doctor.Especialidad;
        CmpBox.Text = Doctor.Cmp ?? string.Empty;
        TelefonoBox.Text = Doctor.Telefono ?? string.Empty;
        EmailBox.Text = Doctor.Email ?? string.Empty;
        ActivoSwitch.IsOn = Doctor.Activo;
        PredeterminadoBox.IsChecked = Doctor.EsPredeterminado;
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

        Doctor.Titulo = TituloBox.SelectedItem as string ?? "Dr.";
        Doctor.Nombres = NombresBox.Text.Trim();
        Doctor.Apellidos = ApellidosBox.Text.Trim();
        Doctor.Especialidad = EspecialidadBox.SelectedItem as string;
        Doctor.Cmp = CmpBox.Text.Trim();
        Doctor.Telefono = TelefonoBox.Text.Trim();
        Doctor.Email = EmailBox.Text.Trim();
        Doctor.Activo = ActivoSwitch.IsOn;
        Doctor.EsPredeterminado = PredeterminadoBox.IsChecked == true;
    }

    private void Telefono_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        => TelefonoFormato.AplicarEnCaja(sender);
}
