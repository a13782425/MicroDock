using AIChatPlugin.Models;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;

namespace AIChatPlugin.Views
{
    /// <summary>
    /// 消息气泡控件
    /// </summary>
    public partial class MessageBubble : UserControl
    {
        public MessageBubble()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    /// <summary>
    /// 角色到背景色转换器
    /// </summary>
    public class RoleToBackgroundConverter : Avalonia.Data.Converters.IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is MessageRole role)
            {
                return role switch
                {
                    MessageRole.User => new SolidColorBrush(Avalonia.Media.Color.FromRgb(0, 120, 215)), // 蓝色
                    MessageRole.Assistant => new SolidColorBrush(Avalonia.Media.Color.FromRgb(100, 100, 100)), // 灰色
                    _ => new SolidColorBrush(Avalonia.Media.Color.FromRgb(200, 200, 200))
                };
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 角色到对齐方式转换器
    /// </summary>
    public class RoleToAlignmentConverter : Avalonia.Data.Converters.IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is MessageRole role)
            {
                return role == MessageRole.User ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            return HorizontalAlignment.Left;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 角色到名称转换器
    /// </summary>
    public class RoleToNameConverter : Avalonia.Data.Converters.IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is MessageRole role)
            {
                return role switch
                {
                    MessageRole.User => "用户",
                    MessageRole.Assistant => "AI",
                    MessageRole.System => "系统",
                    MessageRole.Tool => "工具",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 是否为 Assistant 角色转换器
    /// </summary>
    public class IsAssistantRoleConverter : Avalonia.Data.Converters.IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return value is MessageRole role && role == MessageRole.Assistant;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 是否为 User 角色转换器
    /// </summary>
    public class IsUserRoleConverter : Avalonia.Data.Converters.IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return value is MessageRole role && role == MessageRole.User;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

