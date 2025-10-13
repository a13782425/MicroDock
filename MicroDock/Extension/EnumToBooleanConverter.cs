using System;
using System.Globalization;
using Avalonia.Data.Converters;
using MicroDock.Models;

namespace MicroDock.Extension
{
    /// <summary>
    /// 枚举到布尔值转换器
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            if (value is TabType tabType && parameter is string paramStr)
            {
                if (Enum.TryParse<TabType>(paramStr, out TabType targetType1))
                {
                    return tabType == targetType1;
                }
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

