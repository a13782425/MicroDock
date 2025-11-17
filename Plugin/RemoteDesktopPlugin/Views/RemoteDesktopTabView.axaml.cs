using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using MicroDock.Plugin;
using RemoteDesktopPlugin.ViewModels;
using System;

namespace RemoteDesktopPlugin.Views
{
    /// <summary>
    /// 远程桌面连接列表标签页视图
    /// </summary>
    public partial class RemoteDesktopTabView : UserControl, IMicroTab
    {
        private readonly RemoteDesktopPlugin _plugin;
        private readonly RemoteDesktopTabViewModel _viewModel;

        // UI 控件引用
        private Button? _addConnectionButton;
        private Button? _emptyAddButton;

        /// <summary>
        /// 公开插件实例供子控件使用
        /// </summary>
        public RemoteDesktopPlugin Plugin => _plugin;

        public RemoteDesktopTabView(RemoteDesktopPlugin plugin)
        {
            _plugin = plugin;
            _viewModel = new RemoteDesktopTabViewModel(plugin);

            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
        }

        public string TabName => "远程桌面";

        public IconSymbolEnum IconSymbol => IconSymbolEnum.Remote;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _addConnectionButton = this.FindControl<Button>("AddConnectionButton");
            _emptyAddButton = this.FindControl<Button>("EmptyAddButton");

            // 设置 DataContext
            DataContext = _viewModel;
        }

        private void AttachEventHandlers()
        {
            if (_addConnectionButton != null)
            {
                _addConnectionButton.Click += OnAddConnectionClick;
            }

            if (_emptyAddButton != null)
            {
                _emptyAddButton.Click += OnAddConnectionClick;
            }
        }

        /// <summary>
        /// 刷新连接列表（供卡片调用）
        /// </summary>
        public void RefreshConnections()
        {
            _viewModel.LoadConnections();
        }

        private async void OnAddConnectionClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var content = new AddConnectionDialog(_plugin);
                
                var dialog = new ContentDialog
                {
                    Title = "添加远程连接",
                    Content = content,
                    PrimaryButtonText = "添加",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        content.SaveConnection();
                        RefreshConnections();
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorDialogAsync("添加失败", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _plugin.Context?.LogError("打开添加连接对话框失败", ex);
            }
        }

        private async System.Threading.Tasks.Task ShowErrorDialogAsync(string title, string message)
        {
            var errorDialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定"
            };
            await errorDialog.ShowAsync();
        }
    }
}
