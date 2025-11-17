using Avalonia.Controls;
using ClaudeSwitchPlugin.ViewModels;
using System;

namespace ClaudeSwitchPlugin.Views
{
    public partial class ConfigurationCard : UserControl
    {
        public ConfigurationCard()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            // 如果 DataContext 是 ConfigurationCardViewModel，设置 DataContext
            if (DataContext is ConfigurationCardViewModel viewModel)
            {
                // 确保 DataContext 正确设置
                DataContext = viewModel;
            }
        }
    }
}