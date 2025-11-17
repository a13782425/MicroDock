using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using RemoteDesktopPlugin.Models;
using System;
using System.Threading.Tasks;

namespace RemoteDesktopPlugin.Views
{
    /// <summary>
    /// 连接卡片控件
    /// </summary>
    public partial class ConnectionCard : UserControl
    {
        private SplitButton? _connectSplitButton;
        private MenuItem? _deleteMenuItem;
        private MenuItem? _editMenuItem;
        private TextBlock? _hostTextBlock;
        private RemoteDesktopPlugin? _plugin;

        public ConnectionCard()
        {
            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _connectSplitButton = this.FindControl<SplitButton>("ConnectSplitButton");
            _deleteMenuItem = this.FindControl<MenuItem>("DeleteMenuItem");
            _editMenuItem = this.FindControl<MenuItem>("EditMenuItem");
            _hostTextBlock = this.FindControl<TextBlock>("HostTextBlock");
        }

        private void AttachEventHandlers()
        {
            if (_connectSplitButton != null)
            {
                _connectSplitButton.Click += OnConnectClick;
            }

            if (_deleteMenuItem != null)
            {
                _deleteMenuItem.Click += OnDeleteClick;
            }

            if (_editMenuItem != null)
            {
                _editMenuItem.Click += OnEditClick;
            }

            // 主机地址点击复制
            if (_hostTextBlock != null)
            {
                _hostTextBlock.PointerPressed += OnHostPointerPressed;
            }

            // 监听附加到可视树事件以获取插件实例
            this.AttachedToVisualTree += OnAttachedToVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            // 查找父级 RemoteDesktopTabView 获取插件实例
            RemoteDesktopTabView? tabView = this.FindAncestorOfType<RemoteDesktopTabView>();
            if (tabView != null)
            {
                _plugin = tabView.Plugin;
            }
        }

        private void OnConnectClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is RemoteConnection connection && _plugin != null)
            {
                try
                {
                    _ = Task.Run(() => _plugin.ConnectToRemote(connection));

                    // 刷新列表
                    RefreshParentList();
                }
                catch (Exception ex)
                {
                    _plugin.Context?.LogError($"连接失败: {connection.Name}", ex);
                    _ = ShowErrorDialogAsync("连接失败", ex.Message);
                }
            }
        }

        private async void OnEditClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is RemoteConnection connection && _plugin != null)
            {
                try
                {
                    var content = new AddConnectionDialog(_plugin, connection);
                    
                    var dialog = new ContentDialog
                    {
                        Title = "编辑远程连接",
                        Content = content,
                        PrimaryButtonText = "保存",
                        CloseButtonText = "取消",
                        DefaultButton = ContentDialogButton.Primary
                    };

                    var result = await dialog.ShowAsync();
                    
                    if (result == ContentDialogResult.Primary)
                    {
                        try
                        {
                            content.SaveConnection();
                            RefreshParentList();
                        }
                        catch (Exception saveEx)
                        {
                            await ShowErrorDialogAsync("保存失败", saveEx.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _plugin.Context?.LogError($"编辑连接失败: {connection.Name}", ex);
                    await ShowErrorDialogAsync("编辑失败", ex.Message);
                }
            }
        }

        private async void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is RemoteConnection connection && _plugin != null)
            {
                try
                {
                    // 显示确认对话框
                    var confirmDialog = new ContentDialog
                    {
                        Title = "确认删除",
                        Content = $"确定要删除连接 \"{connection.Name}\" 吗？此操作无法撤销。",
                        PrimaryButtonText = "删除",
                        CloseButtonText = "取消",
                        DefaultButton = ContentDialogButton.Close
                    };

                    var result = await confirmDialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        _plugin.RemoveConnection(connection.Id);
                        RefreshParentList();
                    }
                }
                catch (Exception ex)
                {
                    _plugin.Context?.LogError($"删除连接失败: {connection.Name}", ex);
                    await ShowErrorDialogAsync("删除失败", ex.Message);
                }
            }
        }

        /// <summary>
        /// 主机地址点击事件 - 复制主机地址
        /// </summary>
        private void OnHostPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is RemoteConnection connection)
            {
                try
                {
                    // 复制到剪贴板
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard != null)
                    {
                        Task.Run(async () =>
                        {
                            await clipboard.SetTextAsync(connection.FullAddress);
                        });
                    }

                    _plugin?.Context?.LogInfo($"已复制主机地址: {connection.FullAddress}");
                }
                catch (Exception ex)
                {
                    _plugin?.Context?.LogError($"复制主机地址失败: {connection.FullAddress}", ex);
                }
            }
        }

        private void RefreshParentList()
        {
            RemoteDesktopTabView? tabView = this.FindAncestorOfType<RemoteDesktopTabView>();
            tabView?.RefreshConnections();
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        private async Task ShowErrorDialogAsync(string title, string message)
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

    /// <summary>
    /// 查找可视树祖先的扩展方法
    /// </summary>
    public static class VisualTreeHelper
    {
        public static T? FindAncestorOfType<T>(this Control control) where T : class
        {
            Avalonia.Visual? current = control.GetVisualParent();
            while (current != null)
            {
                if (current is T result)
                    return result;
                current = current.GetVisualParent();
            }
            return null;
        }
    }
}
