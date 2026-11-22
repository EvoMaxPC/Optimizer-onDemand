using System.Globalization;
using System.Windows.Data;

namespace EVO.Desktop.Converters;

/// <summary>Invierte un bool (true→false, false→true) para IsEnabled/Visibilidad.</summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}
