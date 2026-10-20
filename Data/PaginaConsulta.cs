namespace IdermaCapilar_Historial.Data;

public sealed class PaginaConsulta<T>
{
    public required List<T> Items { get; init; }
    public int Total { get; init; }
    public bool HayMas => Items.Count < Total;
}
