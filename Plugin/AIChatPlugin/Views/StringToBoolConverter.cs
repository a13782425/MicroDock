using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AIChatPlugin.Views
{
    /// <summary>
    /// 字符串到布尔值转换器（非空字符串为 true）
    /// </summary>
    public class StringToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return !string.IsNullOrWhiteSpace(str);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

