using Avalonia.Controls;
using Avalonia.Input;
using MicroDock.Plugin;
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
      
    }
}

