using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PreciseAlign.WPF.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果绑定的值是 true，则返回 Visible，否则返回 Collapsed
            bool isVisible = (value is bool b) && b;
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}