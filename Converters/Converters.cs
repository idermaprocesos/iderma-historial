using IdermaCapilar_Historial.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace IdermaCapilar_Historial.Converters;

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var invert = parameter as string == "Invert";
        var isEmpty = value is null || value is string s && string.IsNullOrWhiteSpace(s);
        var visible = invert ? isEmpty : !isEmpty;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var flag = value is true;
        if (parameter as string == "Invert")
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

public sealed class NullableDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is double d ? d : double.NaN;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is double d && !double.IsNaN(d) ? d : null!;
}

public sealed class NullableIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int n ? (double)n : double.NaN;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is double d && !double.IsNaN(d) ? (int)Math.Round(d) : null!;
}

public sealed class CalendarDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            return (object?)(value is DateTimeOffset fecha ? FechaUi.ParaPicker(fecha) : null) ?? null!;
        }
        catch
        {
            return null!;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        try
        {
            return (object?)(value is DateTimeOffset fecha ? FechaUi.DesdePicker(fecha) : null) ?? null!;
        }
        catch
        {
            return null!;
        }
    }
}

public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var count = value is int n ? n : 0;
        var invert = parameter as string == "Invert";
        var visible = invert ? count == 0 : count > 0;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
