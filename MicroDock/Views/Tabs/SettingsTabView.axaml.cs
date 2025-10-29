using Avalonia.Controls;
using Avalonia.Interactivity;
using MicroDock.ViewModels;
using MicroDock.Infrastructure;

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
    }
}

