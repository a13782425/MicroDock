using AIChatPlugin.Models;
using AIChatPlugin.Services;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using System.IO;

namespace AIChatPlugin.Views
{
    /// <summary>
    /// 消息气泡控件
    /// </summary>
    public partial class MessageBubble : UserControl
    {
        private readonly MermaidToImageService _mermaidService;

        public MessageBubble()
        {
            InitializeComponent();
            _mermaidService = new MermaidToImageService();

            // 监听 DataContext 变化
            this.WhenAnyValue(x => x.DataContext)
               .Subscribe(OnDataContextChanged);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnDataContextChanged(object? dc)
        {
            if (dc is MessageViewModel msg)
            {
                // 监听 MermaidCode 变化
                msg.WhenAnyValue(x => x.MermaidCode)
                    .Subscribe(async code =>
                    {
                        if (!string.IsNullOrEmpty(code) && msg.MermaidImage == null)
                        {
                            await LoadMermaidImageAsync(msg, code);
                        }
                    });
            }
        }

        /// <summary>
        /// 加载 Mermaid 图片
        /// </summary>
        private async Task LoadMermaidImageAsync(MessageViewModel msg, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            try
            {
                msg.IsMermaidLoading = true;
                var bitmap = await _mermaidService.ConvertToImageAsync(code);
                if (bitmap != null)
                {
                    msg.MermaidImage = bitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载 Mermaid 图片失败: {ex.Message}");
            }
            finally
            {
                msg.IsMermaidLoading = false;
            }
        }

        /// <summary>
        /// 复制 Mermaid 代码
        /// </summary>
        public async void OnCopyMermaidCode(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MessageViewModel msg && !string.IsNullOrEmpty(msg.MermaidCode))
            {
                try
                {
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(msg.MermaidCode);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"复制失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 查看大图
        /// </summary>
        public async void OnViewFullMermaid(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MessageViewModel msg && msg.MermaidImage != null)
            {
                try
                {
                    // 创建一个新窗口显示大图
                    var window = new Window
                    {
                        Title = "Mermaid 图表",
                        Width = 800,
                        Height = 600,
                        Content = new ScrollViewer
                        {
                            Content = new Image
                            {
                                Source = msg.MermaidImage,
                                Stretch = Avalonia.Media.Stretch.Uniform
                            }
                        }
                    };

                    await window.ShowDialog(TopLevel.GetTopLevel(this) as Window);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"显示大图失败: {ex.Message}");
                }
            }
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
                    MessageRole.Assistant => new SolidColorBrush(Avalonia.Media.Color.FromRgb(245, 245, 245)), // 浅灰色
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
