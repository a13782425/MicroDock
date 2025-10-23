using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MicroDock.Database;
using MicroDock.Services;

namespace MicroDock.Views.Controls;

public class IconConverter : IValueConverter
{
    public static readonly IconConverter Instance = new IconConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 支持三种方式：
        // 1. 直接传入 ApplicationDB 对象
        // 2. 传入 IconHash 字符串
        // 3. 传入 byte[] 数据（向后兼容）
        
        byte[]? iconData = null;
        
        if (value is ApplicationDB app)
        {
            iconData = DBContext.GetIconData(app.IconHash);
        }
        else if (value is string iconHash)
        {
            iconData = DBContext.GetIconData(iconHash);
        }
        else if (value is byte[] iconBytes)
        {
            iconData = iconBytes;
        }

        if (iconData != null && iconData.Length > 0)
        {
            return IconService.ImageFromBytes(iconData);
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
