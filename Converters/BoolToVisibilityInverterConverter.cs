using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InteractiveTextbook.Converters;

public class BoolToVisibilityInverterConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // Nếu là true, trả về Hidden, nếu false trả về Visible
            return boolValue ? Visibility.Hidden : Visibility.Visible;
        }

        return Visibility.Hidden;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
