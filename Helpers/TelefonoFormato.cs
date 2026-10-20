using Microsoft.UI.Xaml.Controls;

namespace IdermaCapilar_Historial.Helpers;

public static class TelefonoFormato
{
    public static string Formatear(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        var digitos = Digitos(valor);
        if (digitos.Length == 0)
        {
            return string.Empty;
        }

        var partes = new List<string>();
        for (var i = 0; i < digitos.Length; i += 3)
        {
            partes.Add(digitos.Substring(i, Math.Min(3, digitos.Length - i)));
        }

        return string.Join(" ", partes);
    }

    public static string? FormatearONull(string? valor)
    {
        var formateado = Formatear(valor);
        return string.IsNullOrEmpty(formateado) ? null : formateado;
    }

    public static string Digitos(string? valor)
        => string.IsNullOrEmpty(valor) ? string.Empty : new string(valor.Where(char.IsDigit).ToArray());

    public static void AplicarEnCaja(TextBox caja)
    {
        var original = caja.Text ?? string.Empty;
        var formateado = Formatear(original);
        if (original == formateado)
        {
            return;
        }

        var limite = Math.Clamp(caja.SelectionStart, 0, original.Length);
        var digitosAntes = original.Take(limite).Count(char.IsDigit);

        caja.Text = formateado;

        var vistos = 0;
        var caret = formateado.Length;
        for (var i = 0; i < formateado.Length; i++)
        {
            if (vistos == digitosAntes)
            {
                caret = i;
                break;
            }

            if (char.IsDigit(formateado[i]))
            {
                vistos++;
            }
        }

        caja.SelectionStart = caret;
    }
}
