using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroDock.Plugin;
using System;
using UnityProjectPlugin.Services;
using UnityProjectPlugin.ViewModels;

namespace UnityProjectPlugin.Views
{
    /// <summary>
    /// Unity 项目列表标签页视图
    /// </summary>
    public partial class UnityProjectTabView : UserControl, IMicroTab
    {
        private readonly UnityProjectPlugin _plugin;
        private readonly UnityProjectTabViewModel _viewModel;

        /// <summary>
        /// 公开插件实例供子控件使用
        /// </summary>
        public UnityProjectPlugin Plugin => _plugin;

        // UI 控件引用
        private TextBox? _searchTextBox;
        private ToggleButton? _groupViewToggle;
        private Button? _addProjectButton;
        private Button? _emptyAddButton;
        private ItemsControl? _tileView;
        private ItemsControl? _groupView;

        public UnityProjectTabView(UnityProjectPlugin plugin)
        {
            _plugin = plugin;
            
            // 创建文件选择服务
            var filePickerService = new FilePickerService(this);
            _viewModel = new UnityProjectTabViewModel(plugin, filePickerService);

            InitializeComponent();
            InitializeControls();
        }

        public string TabName => "Unity项目";

        public IconSymbolEnum IconSymbol => IconSymbolEnum.GamesFilled;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            _groupViewToggle = this.FindControl<ToggleButton>("GroupViewToggle");
            _addProjectButton = this.FindControl<Button>("AddProjectButton");
            _emptyAddButton = this.FindControl<Button>("EmptyAddButton");
            _tileView = this.FindControl<ItemsControl>("TileView");
            _groupView = this.FindControl<ItemsControl>("GroupView");

            // 设置 DataContext
            DataContext = _viewModel;
        }

        /// <summary>
        /// 刷新项目列表（供卡片调用）
        /// </summary>
        public void RefreshProjects()
        {
            _viewModel.LoadProjects();
        }
    }
}
