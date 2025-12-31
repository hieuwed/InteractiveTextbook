using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InteractiveTextbook.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public static NullToVisibilityConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Nếu value là null, hiển thị (Visible)
        // Nếu value không null, ẩn (Collapsed)
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
