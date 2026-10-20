using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Dialogs;

public sealed partial class ContactoDialog : ContentDialog
{
    public ContactoEmergencia Contacto { get; private set; } = new();

    public ContactoDialog()
    {
        InitializeComponent();
        RelacionBox.ItemsSource = Catalogos.RelacionesEmergencia;
    }

    public void Cargar(ContactoEmergencia? existente, int pacienteId)
    {
        Contacto = existente is null
            ? new ContactoEmergencia { PacienteId = pacienteId }
            : new ContactoEmergencia
            {
                Id = existente.Id,
                PacienteId = existente.PacienteId,
                Nombre = existente.Nombre,
                Relacion = existente.Relacion,
                Telefono = existente.Telefono,
                TelefonoAlterno = existente.TelefonoAlterno,
                Email = existente.Email,
                Direccion = existente.Direccion
            };

        Title = existente is null ? "Nuevo contacto de emergencia" : "Editar contacto de emergencia";
        NombreBox.Text = Contacto.Nombre;
        RelacionBox.SelectedItem = Contacto.Relacion;
        TelefonoBox.Text = Contacto.Telefono ?? string.Empty;
        TelefonoAltBox.Text = Contacto.TelefonoAlterno ?? string.Empty;
        EmailBox.Text = Contacto.Email ?? string.Empty;
        DireccionBox.Text = Contacto.Direccion ?? string.Empty;
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private void OnPrimary(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(NombreBox.Text) || string.IsNullOrWhiteSpace(TelefonoBox.Text))
        {
            ErrorText.Text = "Nombre y teléfono son obligatorios.";
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        Contacto.Nombre = NombreBox.Text.Trim();
        Contacto.Relacion = RelacionBox.SelectedItem as string;
        Contacto.Telefono = TelefonoBox.Text.Trim();
        Contacto.TelefonoAlterno = TelefonoAltBox.Text.Trim();
        Contacto.Email = EmailBox.Text.Trim();
        Contacto.Direccion = DireccionBox.Text.Trim();
    }

    private void Telefono_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        => TelefonoFormato.AplicarEnCaja(sender);
}
