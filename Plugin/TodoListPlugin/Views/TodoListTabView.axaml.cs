using Avalonia.Controls;
using MicroDock.Plugin;
using TodoListPlugin.ViewModels;

namespace TodoListPlugin.Views
{
    /// <summary>
    /// 待办清单页签视图 - IMicroTab 实现
    /// </summary>
    public partial class TodoListTabView : UserControl, IMicroTab
    {
        private readonly string _dataPath;
        private TodoListMainViewModel? _viewModel;

        public TodoListTabView(string dataPath)
        {
            _dataPath = dataPath;
            InitializeComponent();
            
            // 初始化 ViewModel
            _viewModel = new TodoListMainViewModel(dataPath);
            MainView.DataContext = _viewModel;
            SettingsView.DataContext = _viewModel;
            
            // 订阅设置请求事件
            MainView.SettingsRequested += OnSettingsRequested;
            SettingsView.BackRequested += OnSettingsBackRequested;
            
            // 自动加载数据
            _ = _viewModel.LoadAsync();
        }

        #region IMicroTab 实现

        public string TabName => "待办清单";

        public IconSymbolEnum IconSymbol => IconSymbolEnum.List;

        #endregion

        /// <summary>
        /// 获取设置内容
        /// </summary>
        public Control? SettingsContent => SettingsView;

        /// <summary>
        /// 显示设置视图
        /// </summary>
        private void OnSettingsRequested(object? sender, System.EventArgs e)
        {
            MainView.IsVisible = false;
            SettingsView.IsVisible = true;
            // 刷新设置视图数据
            SettingsView.RefreshData();
        }

        /// <summary>
        /// 返回主视图
        /// </summary>
        private void OnSettingsBackRequested(object? sender, System.EventArgs e)
        {
            SettingsView.IsVisible = false;
            MainView.IsVisible = true;
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        public async void RefreshData()
        {
            if (_viewModel != null)
            {
                await _viewModel.LoadAsync();
            }
        }
    }
}

