using System;
using System.Globalization;
using System.Windows.Data;

// 1. 确保命名空间是新的文件夹路径
namespace PreciseAlign.WPF.Converters
{
    /// <summary>
    /// 一个值转换器，用于比较ViewModel中的当前步骤名和按钮的参数。
    /// 如果两者相等，则返回 "Current"，触发高亮样式。
    /// </summary>
    public class StepNameToTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString() ? "Current" : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}