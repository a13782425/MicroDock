using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MicroDock.ViewModels;

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
            // 查找父级的 MainWindow
            MainWindow? mainWindow = this.FindAncestorOfType<MainWindow>();
            if (mainWindow?.DataContext is MainWindowViewModel mainViewModel)
            {
                // 调用主窗口 ViewModel 的命令
                mainViewModel.AddCustomTabCommand.Execute(default);
            }
        }
    }
}

