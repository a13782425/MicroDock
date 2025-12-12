using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using MicroDock.Model;
using MicroDock.Plugin;
using MicroDock.Service;
using MicroDock.Utils;
using MicroDock.Views.Dialog;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    /// <summary>
    /// 打开插件文件夹命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; private set; } = null!;

    /// <summary>
    /// 备份插件数据命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> BackupDataCommand { get; private set; } = null!;

    /// <summary>
    /// 恢复插件数据命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RestoreDataCommand { get; private set; } = null!;

    /// <summary>
    /// 上传插件命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> UploadPluginCommand { get; private set; } = null!;

    public PluginSettingItem()
    {
        // 确保所有属性都有默认值，避免 null 引用
        UniqueName = string.Empty;
        PluginName = string.Empty;
        Version = string.Empty;
        Tools = new ObservableCollection<ToolInfo>();

        DeleteCommand = ReactiveCommand.CreateFromTask(DeletePlugin);
        CancelDeleteCommand = ReactiveCommand.Create(CancelDelete);
        CancelUpdateCommand = ReactiveCommand.CreateFromTask(CancelUpdate);
        OpenFolderCommand = ReactiveCommand.Create(OpenFolder);
        BackupDataCommand = ReactiveCommand.CreateFromTask(BackupData);
        RestoreDataCommand = ReactiveCommand.CreateFromTask(RestoreData);
        UploadPluginCommand = ReactiveCommand.CreateFromTask(UploadPlugin);
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
                    ShowNotification("启用失败", $"插件 {PluginName} 启用失败", AppNotificationType.Error);
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
                    ShowNotification("禁用失败", $"插件 {PluginName} 禁用失败", AppNotificationType.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "切换插件 {PluginName} 启用状态失败", PluginName);
            // 恢复原状态
            _isEnabled = !enabled;
            this.RaisePropertyChanged(nameof(IsEnabled));
            ShowNotification("操作失败", ex.Message, AppNotificationType.Error);
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
            var (success, message) = pluginLoader.MarkPluginForDeletion(UniqueName);

            // 隐藏加载提示
            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());

            if (success)
            {
                IsPendingDelete = true;
                this.RaisePropertyChanged(nameof(StatusText));
                this.RaisePropertyChanged(nameof(CanCancelDelete));
                ShowNotification("标记成功", message);
            }
            else
            {
                ShowNotification("标记失败", message, AppNotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "删除插件 {PluginName} 失败", PluginName);
            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());
            ShowNotification("删除失败", ex.Message, AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 取消删除插件
    /// </summary>
    private void CancelDelete()
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

                ShowNotification("取消成功", "插件删除已取消，请手动启用插件");
            }
            else
            {
                ShowNotification("取消失败", "取消删除失败", AppNotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "取消删除插件 {PluginName} 失败", PluginName);
            ShowNotification("取消失败", ex.Message, AppNotificationType.Error);
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

                ShowNotification("取消成功", "插件更新已取消");
            }
            else
            {
                ShowNotification("取消失败", message, AppNotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "取消更新插件 {PluginName} 失败", PluginName);
            ShowNotification("取消失败", ex.Message, AppNotificationType.Error);
        }
    }

    #region 文件夹和备份操作

    /// <summary>
    /// 获取插件文件夹路径
    /// </summary>
    private string GetPluginFolderPath()
    {
        return Path.Combine(AppConfig.ROOT_PATH, "plugins", UniqueName);
    }

    /// <summary>
    /// 获取插件数据文件夹路径
    /// </summary>
    private string GetPluginDataPath()
    {
        return Path.Combine(GetPluginFolderPath(), "data");
    }

    /// <summary>
    /// 打开插件文件夹
    /// </summary>
    private void OpenFolder()
    {
        try
        {
            string pluginFolder = GetPluginFolderPath();
            if (!Directory.Exists(pluginFolder))
            {
                ShowNotification("打开失败", "插件文件夹不存在", AppNotificationType.Error);
                return;
            }
            ServiceLocator.Get<IPlatformService>()?.OpenExplorer(pluginFolder);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "打开插件文件夹失败: {PluginName}", PluginName);
            ShowNotification("打开失败", ex.Message, AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 备份插件数据
    /// </summary>
    private async Task BackupData()
    {
        try
        {
            var settings = Database.DBContext.GetSetting();
            
            if (string.IsNullOrEmpty(settings.ServerAddress))
            {
                ShowNotification("备份失败", "请先在高级设置中配置服务器地址", AppNotificationType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(settings.BackupPassword))
            {
                ShowNotification("备份失败", "请先在高级设置中配置备份密码", AppNotificationType.Warning);
                return;
            }

            string dataPath = GetPluginDataPath();
            if (!Directory.Exists(dataPath))
            {
                ShowNotification("备份失败", "插件数据文件夹不存在", AppNotificationType.Warning);
                return;
            }

            bool confirm = await ShowConfirmDialogAsync(
                $"备份插件数据",
                $"确定要备份插件 \"{PluginName}\" 的数据到服务器吗？",
                "这将覆盖服务器上已有的备份。"
            );

            if (!confirm) return;

            ServiceLocator.Get<EventService>().Publish(new ShowLoadingMessage($"正在备份 {PluginName} 数据..."));

            var (success, message) = await PluginServerApiClient.BackupPluginDataAsync(UniqueName, dataPath);

            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());

            if (success)
            {
                ShowNotification("备份成功", message, AppNotificationType.Success);
            }
            else
            {
                ShowNotification("备份失败", message, AppNotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());
            Serilog.Log.Error(ex, "备份插件数据失败: {PluginName}", PluginName);
            ShowNotification("备份失败", ex.Message, AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 恢复插件数据
    /// </summary>
    private async Task RestoreData()
    {
        try
        {
            var settings = Database.DBContext.GetSetting();
            
            if (string.IsNullOrEmpty(settings.ServerAddress) && string.IsNullOrEmpty(settings.BackupServerAddress))
            {
                ShowNotification("恢复失败", "请先在高级设置中配置服务器地址或备份地址", AppNotificationType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(settings.BackupPassword))
            {
                ShowNotification("恢复失败", "请先在高级设置中配置备份密码", AppNotificationType.Warning);
                return;
            }

            ServiceLocator.Get<EventService>().Publish(new ShowLoadingMessage("正在获取备份列表..."));

            // 获取备份列表
            var response = await PluginServerApiClient.GetBackupListAsync();

            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());

            if (!response.Success || response.Data?.Backups == null)
            {
                ShowNotification("获取失败", response.Message ?? "无法获取备份列表", AppNotificationType.Error);
                return;
            }

            // 筛选该插件的备份
            var pluginBackups = response.Data.Backups
                .Where(b => b.BackupType == "plugin" && (b.Description?.Contains(UniqueName) ?? false))
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            if (pluginBackups.Count == 0)
            {
                ShowNotification("提示", $"服务器上暂无插件 \"{PluginName}\" 的备份", AppNotificationType.Information);
                return;
            }

            // 转换为列表项
            var backupItems = new ObservableCollection<BackupListItem>(
                pluginBackups.Select(b => new BackupListItem
                {
                    Id = b.Id,
                    BackupType = b.BackupType,
                    Description = b.Description ?? "",
                    CreatedAt = b.CreatedAt ?? "",
                    FileSize = b.FileSize,
                    PluginName = UniqueName
                })
            );

            // 显示备份列表对话框
            var selectedBackup = await ShowPluginBackupListDialogAsync($"恢复插件数据: {PluginName}", backupItems);

            if (selectedBackup == null)
            {
                return; // 用户取消
            }

            // 确认恢复
            bool confirm = await ShowConfirmDialogAsync(
                "确认恢复",
                $"确定要恢复此备份吗？\n\n{selectedBackup.DisplayName}\n创建时间: {selectedBackup.FormattedCreatedAt}",
                "这将覆盖当前的插件数据，建议先手动备份重要数据。"
            );

            if (!confirm) return;

            string dataPath = GetPluginDataPath();

            ServiceLocator.Get<EventService>().Publish(new ShowLoadingMessage($"正在恢复 {PluginName} 数据..."));

            var (success, message) = await PluginServerApiClient.RestorePluginDataAsync(UniqueName, dataPath, selectedBackup.Id);

            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());

            if (success)
            {
                ShowNotification("恢复成功", message, AppNotificationType.Success);
            }
            else
            {
                ShowNotification("恢复失败", message, AppNotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());
            Serilog.Log.Error(ex, "恢复插件数据失败: {PluginName}", PluginName);
            ShowNotification("恢复失败", ex.Message, AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 显示插件备份列表对话框
    /// </summary>
    private async Task<BackupListItem?> ShowPluginBackupListDialogAsync(string title, ObservableCollection<BackupListItem> backupItems)
    {
        bool needRefresh = false;

        while (true)
        {
            if (needRefresh)
            {
                // 重新获取备份列表
                ServiceLocator.Get<EventService>().Publish(new ShowLoadingMessage("正在刷新备份列表..."));
                var response = await PluginServerApiClient.GetBackupListAsync();
                ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());

                if (response.Success && response.Data?.Backups != null)
                {
                    backupItems.Clear();
                    var filteredBackups = response.Data.Backups
                        .Where(b => b.BackupType == "plugin" && (b.Description?.Contains(UniqueName) ?? false))
                        .OrderByDescending(b => b.CreatedAt)
                        .ToList();

                    foreach (var b in filteredBackups)
                    {
                        backupItems.Add(new BackupListItem
                        {
                            Id = b.Id,
                            BackupType = b.BackupType,
                            Description = b.Description ?? "",
                            CreatedAt = b.CreatedAt ?? "",
                            FileSize = b.FileSize,
                            PluginName = UniqueName
                        });
                    }
                }
                needRefresh = false;
            }

            if (backupItems.Count == 0)
            {
                ShowNotification("提示", "没有可用的备份", AppNotificationType.Information);
                return null;
            }

            // 使用 GlobalData 传递数据并显示对话框
            GlobalData.TempBackupList = backupItems;
            var result = await UniversalUtils.ShowCustomDialogAsync<BackupListDialog, BackupDialogResult>(
                title, "恢复", "取消");
            GlobalData.TempBackupList = null; // 清理

            if (result?.Action == BackupDialogAction.Delete && result.SelectedItem != null)
            {
                // 用户点击了删除按钮
                var itemToDelete = result.SelectedItem;
                bool confirmDelete = await ShowConfirmDialogAsync(
                    "确认删除",
                    $"确定要删除此备份吗？\n\n{itemToDelete.DisplayName}\n创建时间: {itemToDelete.FormattedCreatedAt}",
                    "此操作不可撤销。"
                );

                if (confirmDelete)
                {
                    ServiceLocator.Get<EventService>().Publish(new ShowLoadingMessage("正在删除备份..."));
                    var (success, message) = await PluginServerApiClient.DeleteBackupAsync(itemToDelete.Id);
                    ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());

                    if (success)
                    {
                        ShowNotification("删除成功", "备份已删除", AppNotificationType.Success);
                        needRefresh = true;
                        continue; // 刷新列表并重新显示对话框
                    }
                    else
                    {
                        ShowNotification("删除失败", message, AppNotificationType.Error);
                        continue; // 重新显示对话框
                    }
                }
                else
                {
                    continue; // 重新显示对话框
                }
            }

            if (result?.Action == BackupDialogAction.Restore && result.SelectedItem != null)
            {
                return result.SelectedItem;
            }

            // 用户取消
            return null;
        }
    }

    /// <summary>
    /// 上传插件到服务器
    /// </summary>
    private async Task UploadPlugin()
    {
        try
        {
            var settings = Database.DBContext.GetSetting();
            
            if (string.IsNullOrEmpty(settings.ServerAddress))
            {
                ShowNotification("上传失败", "请先在高级设置中配置服务器地址", AppNotificationType.Warning);
                return;
            }
            // 检查服务器验证Key
            if (string.IsNullOrEmpty(settings.ServerValidationKey))
            {
                ShowNotification("上传失败", "请先在高级设置中配置服务器验证Key", AppNotificationType.Warning);
                return;
            }
            // 弹窗输入上传验证Key
            var uploadKey = await ShowInputDialogAsync(
                $"上传插件: {PluginName}",
                "请输入上传验证Key",
                null,
                true // 密码模式
            );

            if (string.IsNullOrEmpty(uploadKey))
            {
                return; // 用户取消
            }

            string pluginFolder = GetPluginFolderPath();
            if (!Directory.Exists(pluginFolder))
            {
                ShowNotification("上传失败", "插件文件夹不存在", AppNotificationType.Error);
                return;
            }

            ServiceLocator.Get<EventService>().Publish(new ShowLoadingMessage($"正在上传 {PluginName}..."));

            var (success, message) = await PluginServerApiClient.UploadPluginAsync(pluginFolder, uploadKey);

            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());

            if (success)
            {
                ShowNotification("上传成功", message, AppNotificationType.Success);
            }
            else
            {
                ShowNotification("上传失败", message, AppNotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());
            Serilog.Log.Error(ex, "上传插件失败: {PluginName}", PluginName);
            ShowNotification("上传失败", ex.Message, AppNotificationType.Error);
        }
    }

    #endregion
}

