using System.Globalization;
using System.Windows.Data;

namespace InteractiveTextbook.Converters;

/// <summary>
/// Converts ZoomLevel (0-3) to page width in pixels
/// Base width = 500px
/// </summary>
public class ZoomToWidthConverter : IValueConverter
{
    public static readonly ZoomToWidthConverter Instance = new();
    private const double BaseWidth = 500.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is double zoomLevel)
        {
            return BaseWidth * zoomLevel;
        }
        return BaseWidth;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts ZoomLevel (0-3) to page height in pixels
/// Base height = 700px
/// </summary>
public class ZoomToHeightConverter : IValueConverter
{
    public static readonly ZoomToHeightConverter Instance = new();
    private const double BaseHeight = 700.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is double zoomLevel)
        {
            return BaseHeight * zoomLevel;
        }
        return BaseHeight;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
