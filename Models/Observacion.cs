using CommunityToolkit.Mvvm.ComponentModel;

namespace IdermaCapilar_Historial.Models;

public partial class Observacion : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private int pacienteId;
    [ObservableProperty] private DateTimeOffset fecha = DateTimeOffset.Now;
    [ObservableProperty] private string tipo = "Consulta";
    [ObservableProperty] private string? titulo;
    [ObservableProperty] private string contenido = string.Empty;
    [ObservableProperty] private string? autor;

    public string FechaTexto => Fecha.ToString("dd/MM/yyyy HH:mm");
    public string Encabezado => string.IsNullOrWhiteSpace(Titulo) ? Tipo : Titulo;
    public string Pie => string.Join("  ·  ", new[] { FechaTexto, Tipo, Autor }.Where(x => !string.IsNullOrWhiteSpace(x)));

    partial void OnFechaChanged(DateTimeOffset value) => NotifyDisplay();
    partial void OnTipoChanged(string value) => NotifyDisplay();
    partial void OnTituloChanged(string? value) => NotifyDisplay();
    partial void OnAutorChanged(string? value) => NotifyDisplay();

    private void NotifyDisplay()
    {
        OnPropertyChanged(nameof(FechaTexto));
        OnPropertyChanged(nameof(Encabezado));
        OnPropertyChanged(nameof(Pie));
    }
}
