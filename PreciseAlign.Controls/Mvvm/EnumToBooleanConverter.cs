using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PreciseAlign.Controls
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string enumValue = value.ToString();
            string targetValue = parameter.ToString();
            return enumValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is false)
                return DependencyProperty.UnsetValue;

            try
            {
                return Enum.Parse(targetType, parameter.ToString());
            }
            catch (Exception)
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}