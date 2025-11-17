using Avalonia.Controls;
using System.Threading.Tasks;

namespace ClaudeSwitchPlugin.Services
{
    /// <summary>
    /// 简单的对话框服务
    /// </summary>
    public class DialogService
    {
        /// <summary>
        /// 显示模态对话框
        /// </summary>
        public static Task<bool> ShowModalDialog(Window? parent, UserControl dialogContent, string title = "对话框")
        {
            var window = new Window
            {
                Title = title,
                Width = 500,
                Height = 450,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = dialogContent,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            return window.ShowDialog<bool>(parent);
        }
    }
}