using System.Globalization;
using System.Windows;
using System.Windows.Data;
//namespace 必须是 PreciseAlign.Controls，不能是 PreciseAlign.Controls.Mvvm。
//因为在HImageWindow.xaml中，local 前缀定义为： xmlns:local = "clr-namespace:PreciseAlign.Controls"
namespace PreciseAlign.Controls
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string? enumValue = value.ToString();
            string? targetValue = parameter.ToString();
            if (enumValue == null || targetValue == null)
                return false;
            return enumValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is false || value == null || parameter == null)
                return DependencyProperty.UnsetValue;
            try
            {
                string? paramStr = parameter.ToString();
                if (string.IsNullOrEmpty(paramStr))
                {
                    return DependencyProperty.UnsetValue;
                }
                return Enum.Parse(targetType, paramStr);
            }
            catch (Exception)
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}