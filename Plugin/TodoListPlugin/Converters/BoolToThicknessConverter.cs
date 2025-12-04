using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TodoListPlugin.Converters
{
    /// <summary>
    /// 将布尔值转换为边框厚度的转换器
    /// true -> 2, false -> 0
    /// </summary>
    public class BoolToThicknessConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                return new Thickness(2);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
