using Avalonia.Controls;
using MicroDock.Plugin;
using MicroDock.Service;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace MicroDock.ViewModels;

/// <summary>
/// 插件设置项
/// </summary>
public class PluginSettingItem : ViewModelBase
{
    // 内部字段，供外部直接设置以避免触发 setter
    internal bool _isEnabled;

    /// <summary>
    /// 插件唯一名字
    /// </summary>
    public string UniqueName { get; set; } = string.Empty;

    /// <summary>
    /// 插件名称
    /// </summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// 插件实例
    /// </summary>
    public IMicroDockPlugin? PluginInstance { get; set; }

    /// <summary>
    /// 设置UI控件
    /// </summary>
    public Control? SettingsControl { get; set; }

    /// <summary>
    /// 是否有设置
    /// </summary>
    public bool HasSettings => SettingsControl != null;

    /// <summary>
    /// 插件是否启用
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                // 只有在 UniqueName 已设置的情况下才触发状态切换
                if (this.RaiseAndSetIfChanged(ref _isEnabled, value) == value && !string.IsNullOrEmpty(UniqueName))
                {
                    // 状态变更时调用启用/禁用方法
                    TogglePluginEnabled(value);
                }
                else
                {
                    _isEnabled = value;
                }
            }
        }
    }

    /// <summary>
    /// 插件版本（确保不为 null）
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 安装时间
    /// </summary>
    public DateTime? InstalledAt { get; set; }

    /// <summary>
    /// 安装时间显示文本（确保不为 null）
    /// </summary>
    public string InstalledAtText
    {
        get
        {
            try
            {
                return InstalledAt?.ToString("yyyy-MM-dd HH:mm") ?? "未知";
            }
            catch
            {
                return "未知";
            }
        }
    }

    /// <summary>
    /// 插件注册的工具列表（确保不为 null）
    /// </summary>
    public ObservableCollection<ToolInfo> Tools { get; set; } = new ObservableCollection<ToolInfo>();

    /// <summary>
    /// 是否有工具
    /// </summary>
    public bool HasTools => Tools?.Count > 0;

    /// <summary>
    /// 工具数量
    /// </summary>
    public int ToolCount => Tools?.Count ?? 0;

    /// <summary>
    /// 工具数量显示文本（确保不为 null）
    /// </summary>
    public string ToolCountText => ToolCount > 0 ? $"({ToolCount} 个工具)" : "(无工具)";

    /// <summary>
    /// 是否标记为待删除
    /// </summary>
    private bool _isPendingDelete = false;
    public bool IsPendingDelete
    {
        get => _isPendingDelete;
        set => this.RaiseAndSetIfChanged(ref _isPendingDelete, value);
    }

    /// <summary>
    /// 是否有待安装的更新
    /// </summary>
    private bool _isPendingUpdate = false;
    public bool IsPendingUpdate
    {
        get => _isPendingUpdate;
        set => this.RaiseAndSetIfChanged(ref _isPendingUpdate, value);
    }

    /// <summary>
    /// 待安装的新版本号
    /// </summary>
    private string? _pendingVersion;
    public string? PendingVersion
    {
        get => _pendingVersion;
        set => this.RaiseAndSetIfChanged(ref _pendingVersion, value);
    }

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText =>
        IsPendingDelete ? "待删除" :
        IsPendingUpdate ? $"v{Version} → v{PendingVersion}" :
        IsEnabled ? "已启用" : "已禁用";

    /// <summary>
    /// 是否可以取消删除
    /// </summary>
    public bool CanCancelDelete => IsPendingDelete;

    /// <summary>
    /// 删除插件命令（确保不为 null）
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; private set; } = null!;

    /// <summary>
    /// 取消删除命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelDeleteCommand { get; private set; } = null!;

    /// <summary>
    /// 取消更新命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelUpdateCommand { get; private set; } = null!;

    public PluginSettingItem()
    {
        // 确保所有属性都有默认值，避免 null 引用
        UniqueName = string.Empty;
        PluginName = string.Empty;
        Version = string.Empty;
        Tools = new ObservableCollection<ToolInfo>();

        DeleteCommand = ReactiveCommand.CreateFromTask(DeletePlugin);
        CancelDeleteCommand = ReactiveCommand.CreateFromTask(CancelDelete);
        CancelUpdateCommand = ReactiveCommand.CreateFromTask(CancelUpdate);
    }

    /// <summary>
    /// 加载插件的工具
    /// </summary>
    public void LoadTools()
    {
        if (string.IsNullOrEmpty(UniqueName))
            return;

        try
        {
            // 确保 Tools 集合存在
            if (Tools == null)
            {
                Tools = new ObservableCollection<ToolInfo>();
            }

            Tools.Clear();

            var toolRegistry = ServiceLocator.Get<ToolRegistry>();
            if (toolRegistry != null)
            {
                var tools = toolRegistry.GetPluginTools(UniqueName);
                if (tools != null)
                {
                    foreach (var tool in tools)
                    {
                        if (tool != null)
                        {
                            Tools.Add(tool);
                        }
                    }
                }
            }

            this.RaisePropertyChanged(nameof(Tools));
            this.RaisePropertyChanged(nameof(HasTools));
            this.RaisePropertyChanged(nameof(ToolCount));
            this.RaisePropertyChanged(nameof(ToolCountText));
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "加载插件 {PluginName} 的工具失败", UniqueName);
        }
    }

    /// <summary>
    /// 切换插件启用状态
    /// </summary>
    private async void TogglePluginEnabled(bool enabled)
    {
        // 确保基本属性已设置
        if (string.IsNullOrEmpty(UniqueName) || string.IsNullOrEmpty(PluginName))
        {
            Serilog.Log.Warning("尝试切换未完全初始化的插件状态");
            return;
        }

        try
        {
            PluginService? pluginLoader = ServiceLocator.Get<PluginService>();
            if (pluginLoader == null)
            {
                Serilog.Log.Error("无法获取 PluginLoader 服务");
                // 恢复原状态
                _isEnabled = !enabled;
                this.RaisePropertyChanged(nameof(IsEnabled));
                return;
            }

            if (enabled)
            {
                bool success = await pluginLoader.EnablePluginAsync(UniqueName);
                if (!success)
                {
                    // 启用失败，恢复状态
                    _isEnabled = false;
                    this.RaisePropertyChanged(nameof(IsEnabled));
                    SettingsTabViewModel.ShowNotification("启用失败", $"插件 {PluginName} 启用失败", Avalonia.Controls.Notifications.NotificationType.Error);
                }
            }
            else
            {
                bool success = pluginLoader.DisablePlugin(UniqueName);
                if (!success)
                {
                    // 禁用失败，恢复状态
                    _isEnabled = true;
                    this.RaisePropertyChanged(nameof(IsEnabled));
                    SettingsTabViewModel.ShowNotification("禁用失败", $"插件 {PluginName} 禁用失败", Avalonia.Controls.Notifications.NotificationType.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "切换插件 {PluginName} 启用状态失败", PluginName);
            // 恢复原状态
            _isEnabled = !enabled;
            this.RaisePropertyChanged(nameof(IsEnabled));
            SettingsTabViewModel.ShowNotification("操作失败", ex.Message, Avalonia.Controls.Notifications.NotificationType.Error);
        }
    }

    /// <summary>
    /// 删除插件
    /// </summary>
    private async Task DeletePlugin()
    {
        try
        {
            // 使用 FluentAvalonia 的 ContentDialog 显示确认对话框
            var dialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = "确认删除",
                Content = new StackPanel
                {
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"确定要删除插件 \"{PluginName}\" 吗？",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = "插件将在下次重启时删除。此操作将删除插件文件和所有相关数据。",
                            Foreground = Avalonia.Media.Brushes.OrangeRed,
                            FontSize = 12,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        }
                    }
                },
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();

            if (result != FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
            {
                return; // 用户取消
            }

            // 显示加载提示
            ServiceLocator.Get<EventService>().Publish(new ShowLoadingMessage("正在标记插件为待删除..."));

            // 调用 PluginLoader 标记插件为待删除
            PluginService pluginLoader = ServiceLocator.Get<PluginService>();
            var (success, message) = await pluginLoader.MarkPluginForDeletionAsync(UniqueName);

            // 隐藏加载提示
            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());

            if (success)
            {
                IsPendingDelete = true;
                this.RaisePropertyChanged(nameof(StatusText));
                this.RaisePropertyChanged(nameof(CanCancelDelete));
                SettingsTabViewModel.ShowNotification("标记成功", message);
            }
            else
            {
                SettingsTabViewModel.ShowNotification("标记失败", message, Avalonia.Controls.Notifications.NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "删除插件 {PluginName} 失败", PluginName);
            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());
            SettingsTabViewModel.ShowNotification("删除失败", ex.Message, Avalonia.Controls.Notifications.NotificationType.Error);
        }
    }

    /// <summary>
    /// 取消删除插件
    /// </summary>
    private async Task CancelDelete()
    {
        try
        {
            PluginService pluginLoader = ServiceLocator.Get<PluginService>();
            bool success = pluginLoader.CancelPluginDeletion(UniqueName);

            if (success)
            {
                IsPendingDelete = false;
                this.RaisePropertyChanged(nameof(StatusText));
                this.RaisePropertyChanged(nameof(CanCancelDelete));

                SettingsTabViewModel.ShowNotification("取消成功", "插件删除已取消，请手动启用插件");
            }
            else
            {
                SettingsTabViewModel.ShowNotification("取消失败", "取消删除失败", Avalonia.Controls.Notifications.NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "取消删除插件 {PluginName} 失败", PluginName);
            SettingsTabViewModel.ShowNotification("取消失败", ex.Message, Avalonia.Controls.Notifications.NotificationType.Error);
        }
    }

    /// <summary>
    /// 取消更新插件
    /// </summary>
    private async Task CancelUpdate()
    {
        try
        {
            PluginService pluginLoader = ServiceLocator.Get<PluginService>();
            var (success, message) = await pluginLoader.CancelPluginUpdateAsync(UniqueName);

            if (success)
            {
                IsPendingUpdate = false;
                PendingVersion = null;
                this.RaisePropertyChanged(nameof(StatusText));

                SettingsTabViewModel.ShowNotification("取消成功", "插件更新已取消");
            }
            else
            {
                SettingsTabViewModel.ShowNotification("取消失败", message, Avalonia.Controls.Notifications.NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "取消更新插件 {PluginName} 失败", PluginName);
            SettingsTabViewModel.ShowNotification("取消失败", ex.Message, Avalonia.Controls.Notifications.NotificationType.Error);
        }
    }
}

