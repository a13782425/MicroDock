using Avalonia.Controls;
using MicroDock.Plugin;
using ClaudeSwitchPlugin.ViewModels;

namespace ClaudeSwitchPlugin.Views
{
    public partial class ClaudeSwitchTabView : UserControl, IMicroTab
    {
        public string TabName => "Claude 配置";
        public IconSymbolEnum IconSymbol => IconSymbolEnum.Settings;

        public ClaudeSwitchTabView()
        {
            InitializeComponent();

            // 设置 ViewModel
            DataContext = new ClaudeSwitchTabViewModel();
        }
    }
}