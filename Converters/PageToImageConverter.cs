using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using InteractiveTextbook.ViewModels;

namespace InteractiveTextbook.Converters;

public class PageToImageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ObservableCollection<BitmapSourceWrapper> pages && pages.Count > 0)
        {
            return pages[0].Source ?? new BitmapImage();
        }

        return new BitmapImage();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
