using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace IdermaCapilar_Historial.Dialogs;

public sealed partial class TipoMotivoDialog : ContentDialog
{
    public TipoAgendaConfig Motivo { get; private set; } = new();

    public TipoMotivoDialog()
    {
        InitializeComponent();
    }

    public void Cargar(TipoAgendaConfig? existente, string titulo)
    {
        Motivo = existente is null
            ? TipoAgendaConfig.From("Nuevo motivo", 110, 140, 132)
            : TipoAgendaConfig.From(existente.Nombre, existente.R, existente.G, existente.B);
        Title = titulo;
        NombreBox.Text = Motivo.Nombre == "Nuevo motivo" && existente is null ? string.Empty : Motivo.Nombre;
        ColorBox.Color = Color.FromArgb(255, Motivo.R, Motivo.G, Motivo.B);
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private void OnPrimary(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var nombre = NombreBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            ErrorText.Text = "Escriba el nombre del motivo.";
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        var color = ColorBox.Color;
        Motivo.Nombre = nombre;
        Motivo.R = color.R;
        Motivo.G = color.G;
        Motivo.B = color.B;
    }
}
