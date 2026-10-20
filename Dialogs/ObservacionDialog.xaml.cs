using IdermaCapilar_Historial.Helpers;
using IdermaCapilar_Historial.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Dialogs;

public sealed partial class ObservacionDialog : ContentDialog
{
    public Observacion Observacion { get; private set; } = new();

    public ObservacionDialog()
    {
        InitializeComponent();
        FechaUi.AplicarRango(FechaBox);
        TipoBox.ItemsSource = Catalogos.TiposObservacion;
    }

    public void Cargar(Observacion? existente, int pacienteId)
    {
        Observacion = existente is null
            ? new Observacion
            {
                PacienteId = pacienteId,
                Autor = AppSettings.Current.MedicoPredeterminado
            }
            : new Observacion
            {
                Id = existente.Id,
                PacienteId = existente.PacienteId,
                Fecha = existente.Fecha,
                Tipo = existente.Tipo,
                Titulo = existente.Titulo,
                Contenido = existente.Contenido,
                Autor = existente.Autor
            };

        Title = existente is null ? "Nueva observación" : "Editar observación";
        TipoBox.SelectedItem = Observacion.Tipo;
        FechaBox.Date = FechaUi.ParaPicker(Observacion.Fecha);
        TituloBox.Text = Observacion.Titulo ?? string.Empty;
        ContenidoBox.Text = Observacion.Contenido;
        DoctorCatalog.Instance.CargarCombo(AutorBox, Observacion.Autor, usarPredeterminadoSiVacio: existente is null);
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private void OnPrimary(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(ContenidoBox.Text))
        {
            ErrorText.Text = "El detalle de la observación es obligatorio.";
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        Observacion.Tipo = TipoBox.SelectedItem as string ?? "Consulta";
        Observacion.Fecha = FechaUi.Combinar(FechaBox.Date, Observacion.Fecha.TimeOfDay);
        Observacion.Titulo = TituloBox.Text;
        Observacion.Contenido = ContenidoBox.Text.Trim();
        Observacion.Autor = AutorBox.SelectedItem as string;
    }
}
