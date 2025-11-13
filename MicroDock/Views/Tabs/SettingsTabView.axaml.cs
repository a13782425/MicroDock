using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MicroDock.ViewModels;
using MicroDock.Infrastructure;
using MicroDock.Plugin;

namespace MicroDock.Views
{
    public partial class SettingsTabView : UserControl
    {
        public SettingsTabView()
        {
            InitializeComponent();
            // 设置独立的 ViewModel
            DataContext = new SettingsTabViewModel();
        }
        
        /// <summary>
        /// 获取设置 ViewModel
        /// </summary>
        public SettingsTabViewModel? ViewModel => DataContext as SettingsTabViewModel;
        
        /// <summary>
        /// 添加自定义页签按钮点击事件
        /// </summary>
        private void AddCustomTab_OnClick(object? sender, RoutedEventArgs e)
        {
            // 通过事件聚合器发布添加自定义标签页请求
            EventAggregator.Instance.Publish(new AddCustomTabRequestMessage());
        }
        
        /// <summary>
        /// 插件唯一名点击事件 - 复制到剪切板
        /// </summary>
        private async void PluginUniqueName_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is PluginSettingItem pluginItem)
            {
                await SettingsTabViewModel.CopyToClipboardAsync(pluginItem.UniqueName, "插件唯一名");
                e.Handled = true;
            }
        }
        
        /// <summary>
        /// 工具名点击事件 - 复制到剪切板
        /// </summary>
        private async void ToolName_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is ToolInfo toolInfo)
            {
                await SettingsTabViewModel.CopyToClipboardAsync(toolInfo.Name, "工具名");
                e.Handled = true;
            }
        }
    }
}

