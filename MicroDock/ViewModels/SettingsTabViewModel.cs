using Avalonia.Media;
using MicroDock.Database;
using MicroDock.Services;
using MicroDock.Infrastructure;
using MicroDock.Plugin;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Serilog;

namespace MicroDock.ViewModels;

/// <summary>
/// 设置页签的 ViewModel
/// </summary>
public class SettingsTabViewModel : ViewModelBase
{
    private bool _autoStartup;
    private bool _autoHide;
    private bool _alwaysOnTop;
    private bool _showLogViewer;
    private string _selectedTheme = string.Empty;

    public SettingsTabViewModel()
    {
        Applications = new ObservableCollection<ApplicationDB>(DBContext.GetApplications());
        AddApplicationCommand = ReactiveCommand.CreateFromTask(AddApplication);
        RemoveApplicationCommand = ReactiveCommand.Create<ApplicationDB>(RemoveApplication);
        ImportPluginCommand = ReactiveCommand.CreateFromTask(ImportPlugin);
        PluginSettings = new ObservableCollection<PluginSettingItem>();
        AvailableThemes = new ObservableCollection<MicroDock.Models.ThemeModel>();
        FlattenedThemeList = new ObservableCollection<object>();
        _groupedThemes = new List<IGrouping<string, MicroDock.Models.ThemeModel>>();

        LoadSettings();
        LoadThemes();

        // 加载插件设置（使用单例实例）
        LoadPluginSettings();

        // 订阅服务状态变更通知
        EventAggregator.Instance.Subscribe<ServiceStateChangedMessage>(OnServiceStateChanged);

        // 订阅插件事件
        EventAggregator.Instance.Subscribe<PluginImportedMessage>(OnPluginImported);
        EventAggregator.Instance.Subscribe<PluginDeletedMessage>(OnPluginDeleted);
    }

    /// <summary>
    /// 处理服务状态变更通知
    /// </summary>
    private void OnServiceStateChanged(ServiceStateChangedMessage message)
    {
        // 当服务状态从外部变更时，同步到ViewModel和数据库
        switch (message.ServiceName)
        {
            case "AutoStartup":
                if (_autoStartup != message.IsEnabled)
                {
                    _autoStartup = message.IsEnabled;
                    this.RaisePropertyChanged(nameof(AutoStartup));
                    SaveSetting(nameof(AutoStartup), message.IsEnabled);
                }
                break;
            case "AutoHide":
                if (_autoHide != message.IsEnabled)
                {
                    _autoHide = message.IsEnabled;
                    this.RaisePropertyChanged(nameof(AutoHide));
                    SaveSetting(nameof(AutoHide), message.IsEnabled);
                }
                break;
            case "AlwaysOnTop":
                if (_alwaysOnTop != message.IsEnabled)
                {
                    _alwaysOnTop = message.IsEnabled;
                    this.RaisePropertyChanged(nameof(AlwaysOnTop));
                    SaveSetting(nameof(AlwaysOnTop), message.IsEnabled);
                }
                break;
        }
    }

    /// <summary>
    /// 是否开机自启动
    /// </summary>
    public bool AutoStartup
    {
        get => _autoStartup;
        set
        {
            this.RaiseAndSetIfChanged(ref _autoStartup, value);
            SaveSetting(nameof(AutoStartup), value);
            // 通过事件请求改变服务状态
            EventAggregator.Instance.Publish(new AutoStartupChangeRequestMessage(value));
        }
    }

    /// <summary>
    /// 是否靠边隐藏
    /// </summary>
    public bool AutoHide
    {
        get => _autoHide;
        set
        {
            this.RaiseAndSetIfChanged(ref _autoHide, value);
            SaveSetting(nameof(AutoHide), value);
            // 通过事件请求改变服务状态
            EventAggregator.Instance.Publish(new AutoHideChangeRequestMessage(value));
        }
    }

    /// <summary>
    /// 是否窗口置顶
    /// </summary>
    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set
        {
            this.RaiseAndSetIfChanged(ref _alwaysOnTop, value);
            SaveSetting(nameof(AlwaysOnTop), value);
            // 通过事件请求改变服务状态
            EventAggregator.Instance.Publish(new WindowTopmostChangeRequestMessage(value));
        }
    }

    /// <summary>
    /// 是否显示日志查看器标签页
    /// </summary>
    public bool ShowLogViewer
    {
        get => _showLogViewer;
        set
        {
            this.RaiseAndSetIfChanged(ref _showLogViewer, value);
            SaveSetting(nameof(ShowLogViewer), value);
            // 通过事件请求改变日志查看器可见性
            EventAggregator.Instance.Publish(new LogViewerVisibilityChangedMessage(value));
        }
    }

    /// <summary>
    /// 可用主题列表（用于内部管理）
    /// </summary>
    public ObservableCollection<MicroDock.Models.ThemeModel> AvailableThemes { get; }

    /// <summary>
    /// 扁平化主题列表（包含分组标题和主题项）
    /// </summary>
    public ObservableCollection<object> FlattenedThemeList { get; }

    private IEnumerable<IGrouping<string, MicroDock.Models.ThemeModel>>? _groupedThemes;

    /// <summary>
    /// 分组后的主题列表（用于 GroupedComboBox 控件）
    /// </summary>
    public IEnumerable<IGrouping<string, MicroDock.Models.ThemeModel>>? GroupedThemes
    {
        get => _groupedThemes;
        private set => this.RaiseAndSetIfChanged(ref _groupedThemes, value);
    }

    /// <summary>
    /// 选中的主题（用于数据库存储）
    /// </summary>
    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme != value)
            {
                _selectedTheme = value;
                this.RaisePropertyChanged();

                // 保存到数据库
                DBContext.UpdateSetting(s => s.SelectedTheme = value);

                // 应用主题
                var themeService = Infrastructure.ServiceLocator.Get<Services.ThemeService>();
                themeService.LoadAndApplyTheme(value);

                // 同步更新 SelectedThemeModel
                UpdateSelectedThemeModel();
            }
        }
    }

    private MicroDock.Models.ThemeModel? _selectedThemeModel;

    private bool _isUpdatingThemeModel = false;

    /// <summary>
    /// 选中的主题模型（用于UI绑定）
    /// </summary>
    public MicroDock.Models.ThemeModel? SelectedThemeModel
    {
        get => _selectedThemeModel;
        set
        {
            // 如果正在从 SelectedTheme 更新，跳过以避免循环
            if (_isUpdatingThemeModel)
            {
                return;
            }

            // 如果选择的是分组标题项，阻止选择并恢复当前选择
            if (value is MicroDock.Models.ThemeGroupHeader)
            {
                // 通知UI恢复之前的选择
                this.RaisePropertyChanged();
                return;
            }

            if (_selectedThemeModel != value)
            {
                _selectedThemeModel = value;
                this.RaisePropertyChanged();

                // 当UI选择改变时，更新 SelectedTheme（string）
                if (value != null && !string.IsNullOrEmpty(value.Name))
                {
                    // 直接设置私有字段，避免触发 SelectedTheme 的 setter 中的 UpdateSelectedThemeModel
                    if (_selectedTheme != value.Name)
                    {
                        _selectedTheme = value.Name;
                        // 保存到数据库
                        DBContext.UpdateSetting(s => s.SelectedTheme = value.Name);

                        // 应用主题
                        var themeService = Infrastructure.ServiceLocator.Get<Services.ThemeService>();
                        themeService.LoadAndApplyTheme(value.Name);

                        // 通知 SelectedTheme 属性已更改
                        this.RaisePropertyChanged(nameof(SelectedTheme));
                    }
                }
            }
        }
    }

    /// <summary>
    /// 根据 SelectedTheme（string）更新 SelectedThemeModel
    /// </summary>
    private void UpdateSelectedThemeModel()
    {
        _isUpdatingThemeModel = true;
        try
        {
            if (string.IsNullOrEmpty(_selectedTheme))
            {
                if (_selectedThemeModel != null)
                {
                    _selectedThemeModel = null;
                    this.RaisePropertyChanged(nameof(SelectedThemeModel));
                }
                return;
            }

            // 从扁平化列表中查找主题（跳过分组标题项）
            var theme = FlattenedThemeList.OfType<MicroDock.Models.ThemeModel>()
                .FirstOrDefault(t => t.Name.Equals(_selectedTheme, StringComparison.OrdinalIgnoreCase));

            if (theme != _selectedThemeModel)
            {
                _selectedThemeModel = theme;
                this.RaisePropertyChanged(nameof(SelectedThemeModel));
            }
        }
        finally
        {
            _isUpdatingThemeModel = false;
        }
    }

    public ObservableCollection<ApplicationDB> Applications { get; }
    public ReactiveCommand<Unit, Unit> AddApplicationCommand { get; }
    public ReactiveCommand<ApplicationDB, Unit> RemoveApplicationCommand { get; }

    /// <summary>
    /// 导入插件命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ImportPluginCommand { get; }

    /// <summary>
    /// 插件设置列表
    /// </summary>
    public ObservableCollection<PluginSettingItem> PluginSettings { get; }

    /// <summary>
    /// 加载插件设置
    /// </summary>
    private void LoadPluginSettings()
    {
        PluginSettings.Clear();

        // 获取插件目录路径
        string appDirectory = System.AppContext.BaseDirectory;
        string pluginDirectory = Path.Combine(appDirectory, "Plugins");

        // 加载所有插件
        IReadOnlyList<PluginInfo> plugins = Infrastructure.ServiceLocator.Get<PluginLoader>().LoadedPlugins;

        // 为每个插件创建设置项
        foreach (PluginInfo pluginInfo in plugins)
        {
            if (pluginInfo.PluginInstance == null)
            {
                continue;
            }

            // 调用插件的 GetSettingsControl 方法
            Control? settingsControl = null;
            try
            {
                settingsControl = pluginInfo.PluginInstance.GetSettingsControl() as Control;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "获取插件 {PluginName} 的设置UI失败", pluginInfo.Name);
            }

            // 从数据库获取插件信息
            PluginInfoDB? dbInfo = DBContext.GetPluginInfo(pluginInfo.UniqueName);

            PluginSettingItem settingItem = new PluginSettingItem();

            // 先设置基础属性，避免在 IsEnabled setter 中出错
            settingItem.UniqueName = pluginInfo.UniqueName;
            settingItem.PluginName = pluginInfo.Name;
            settingItem.PluginInstance = pluginInfo.PluginInstance;
            settingItem.SettingsControl = settingsControl;
            settingItem.Version = pluginInfo.Manifest?.Version ?? "未知";
            settingItem.InstalledAt = dbInfo != null ? (DateTime?)dbInfo.InstalledAtDateTime : null;
            settingItem.IsPendingDelete = dbInfo?.PendingDelete ?? false;

            // 最后设置 IsEnabled，此时其他属性都已就绪
            settingItem._isEnabled = dbInfo?.IsEnabled ?? true;

            // 加载插件的工具
            try
            {
                settingItem.LoadTools();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "加载插件 {PluginName} 的工具列表失败", pluginInfo.Name);
            }

            PluginSettings.Add(settingItem);
        }
    }

    /// <summary>
    /// 导入插件
    /// </summary>
    private async Task ImportPlugin()
    {
        try
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "选择插件ZIP文件",
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>
                {
                    new() { Name = "ZIP 文件", Extensions = { "zip" } },
                    new() { Name = "所有文件", Extensions = { "*" } }
                }
            };

            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                string[]? result = await dialog.ShowAsync(desktop.MainWindow);
                if (result != null && result.Length > 0)
                {
                    string zipFilePath = result[0];

                    // 显示加载提示
                    EventAggregator.Instance.Publish(new ShowLoadingMessage("正在导入插件..."));

                    try
                    {
                        // 获取插件目录
                        string appDirectory = System.AppContext.BaseDirectory;
                        string pluginDirectory = Path.Combine(appDirectory, "Plugins");

                        // 调用 PluginLoader 导入插件
                        PluginLoader pluginLoader = Infrastructure.ServiceLocator.Get<PluginLoader>();
                        var (success, message, pluginName) = await pluginLoader.ImportPluginAsync(zipFilePath, pluginDirectory);

                        // 隐藏加载提示
                        EventAggregator.Instance.Publish(new HideLoadingMessage());

                        if (success)
                        {
                            ShowNotification("导入成功", message);

                            // 刷新插件列表
                            LoadPluginSettings();

                            // 发布插件导入事件
                            if (!string.IsNullOrEmpty(pluginName))
                            {
                                EventAggregator.Instance.Publish(new PluginImportedMessage { PluginName = pluginName });
                            }
                        }
                        else
                        {
                            ShowNotification("导入失败", message, Avalonia.Controls.Notifications.NotificationType.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        EventAggregator.Instance.Publish(new HideLoadingMessage());
                        Log.Error(ex, "导入插件失败");
                        ShowNotification("导入失败", ex.Message, Avalonia.Controls.Notifications.NotificationType.Error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开文件对话框失败");
            ShowNotification("错误", "打开文件对话框失败", Avalonia.Controls.Notifications.NotificationType.Error);
        }
    }

    /// <summary>
    /// 处理插件导入事件
    /// </summary>
    private void OnPluginImported(PluginImportedMessage message)
    {
        // 刷新插件列表
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            LoadPluginSettings();
        });
    }

    /// <summary>
    /// 处理插件删除事件
    /// </summary>
    private void OnPluginDeleted(PluginDeletedMessage message)
    {
        Log.Information("收到插件删除消息: {PluginName}", message.PluginName);
        
        // 检查插件是否是待删除状态
        // 如果是待删除，不从列表中移除，只刷新显示
        // 如果是真正删除（启动时删除），才从列表中移除
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            PluginSettingItem? item = PluginSettings.FirstOrDefault(p => p.UniqueName == message.PluginName);
            if (item != null)
            {
                // 检查数据库中的状态
                var dbInfo = Database.DBContext.GetPluginInfo(message.PluginName);
                
                if (dbInfo != null && dbInfo.PendingDelete)
                {
                    // 待删除状态，不移除，只刷新 UI
                    Log.Information("插件 {PluginName} 标记为待删除，保留在设置列表中", message.PluginName);
                    // 不做任何操作，UI 会自动通过绑定更新
                }
                else
                {
                    // 真正删除，从列表中移除
                    Log.Information("从设置列表中移除插件: {PluginName}", message.PluginName);
                    PluginSettings.Remove(item);
                }
            }
            else
            {
                Log.Warning("未在设置列表中找到插件: {PluginName}", message.PluginName);
            }
        });
    }

    private async System.Threading.Tasks.Task AddApplication()
    {
        OpenFileDialog dialog = new OpenFileDialog
        {
            Title = "选择要添加的应用程序",
            AllowMultiple = true,
            Filters = new List<FileDialogFilter>
            {
                new() { Name = "Applications", Extensions = { "exe", "lnk" } },
                new() { Name = "All files", Extensions = { "*" } }
            }
        };

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            string[]? result = await dialog.ShowAsync(desktop.MainWindow);
            if (result != null && result.Length > 0)
            {
                foreach (string filePath in result)
                {
                    byte[]? iconBytes = IconService.TryExtractFileIconBytes(filePath);
                    ApplicationDB app = new ApplicationDB
                    {
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        FilePath = filePath
                    };
                    DBContext.AddApplication(app, iconBytes);
                    Applications.Add(app);
                }
            }
        }
    }

    private void RemoveApplication(ApplicationDB app)
    {
        if (app != null)
        {
            DBContext.DeleteApplication(app.Id);
            Applications.Remove(app);
        }
    }

    /// <summary>
    /// 从数据库加载配置（仅加载到私有字段，不触发事件）
    /// </summary>
    private void LoadSettings()
    {
        SettingDB settings = DBContext.GetSetting();
        _autoStartup = settings.AutoStartup;
        _autoHide = settings.AutoHide;
        _alwaysOnTop = settings.AlwaysOnTop;
        _showLogViewer = settings.ShowLogViewer;
        _selectedTheme = settings.SelectedTheme;

        // 通知UI更新（仅UI，不触发setter中的事件发布）
        this.RaisePropertyChanged(nameof(AutoStartup));
        this.RaisePropertyChanged(nameof(AutoHide));
        this.RaisePropertyChanged(nameof(AlwaysOnTop));
        this.RaisePropertyChanged(nameof(ShowLogViewer));
        this.RaisePropertyChanged(nameof(SelectedTheme));
    }

    /// <summary>
    /// 加载可用主题列表
    /// </summary>
    private void LoadThemes()
    {
        try
        {
            AvailableThemes.Clear();
            FlattenedThemeList.Clear();

            Services.ThemeService themeService = Infrastructure.ServiceLocator.Get<Services.ThemeService>();
            List<MicroDock.Models.ThemeModel> themes = themeService.GetAvailableThemes();

            // 添加到内部列表
            foreach (MicroDock.Models.ThemeModel theme in themes)
            {
                AvailableThemes.Add(theme);
            }

            // 按 Category 分组
            List<IGrouping<string, MicroDock.Models.ThemeModel>> groupedThemes = themes
                .GroupBy(t => string.IsNullOrEmpty(t.Category) ? "其他" : t.Category)
                .OrderBy(g => GetCategorySortOrder(g.Key))
                .ToList();

            // 为每个分组内的主题按 DisplayName 排序
            GroupedThemes = groupedThemes
                .Select(g => g.OrderBy(t => t.DisplayName).ToList().GroupBy(t => g.Key).First())
                .ToList();

            Serilog.Log.Information("加载主题分组完成，共 {GroupCount} 个分组，总计 {ThemeCount} 个主题",
                GroupedThemes?.Count() ?? 0,
                themes.Count);

            // 创建扁平化列表：每组前插入分组标题项，然后添加该组的主题
            foreach (IGrouping<string, MicroDock.Models.ThemeModel> group in groupedThemes)
            {
                // 添加分组标题
                FlattenedThemeList.Add(new MicroDock.Models.ThemeGroupHeader
                {
                    GroupName = group.Key
                });

                // 添加该组的主题（按 DisplayName 排序）
                foreach (MicroDock.Models.ThemeModel theme in group.OrderBy(t => t.DisplayName))
                {
                    FlattenedThemeList.Add(theme);
                }
            }

            // 主题列表加载完成后，同步更新 SelectedThemeModel
            UpdateSelectedThemeModel();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "加载主题列表失败");
        }
    }

    /// <summary>
    /// 获取分类排序顺序（默认、Fluent、Tailwind等）
    /// </summary>
    private int GetCategorySortOrder(string category)
    {
        return category switch
        {
            "默认" => 0,
            "Fluent" => 1,
            "Tailwind" => 2,
            _ => 999 // 其他分类排在最后
        };
    }

    /// <summary>
    /// 保存配置到数据库
    /// </summary>
    private void SaveSetting(string settingName, bool value)
    {
        DBContext.UpdateSetting(settings =>
        {
            switch (settingName)
            {
                case nameof(AutoStartup):
                    settings.AutoStartup = value;
                    break;
                case nameof(AutoHide):
                    settings.AutoHide = value;
                    break;
                case nameof(AlwaysOnTop):
                    settings.AlwaysOnTop = value;
                    break;
                case nameof(ShowLogViewer):
                    settings.ShowLogViewer = value;
                    break;
            }
        });
    }

    /// <summary>
    /// 复制文本到剪切板并显示通知
    /// </summary>
    /// <param name="text">要复制的文本</param>
    /// <param name="typeName">类型名称（用于通知显示）</param>
    public static async Task CopyToClipboardAsync(string text, string typeName)
    {
        try
        {
            // 获取主窗口
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow != null)
            {
                var clipboard = desktop.MainWindow.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(text);

                    // 显示应用内通知
                    ShowNotification($"已复制{typeName}", text);

                    Log.Information("已复制{TypeName}到剪切板: {Text}", typeName, text);
                }
                else
                {
                    Log.Warning("剪切板服务不可用");
                }
            }
            else
            {
                Log.Warning("无法获取主窗口，剪切板操作失败");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "复制到剪切板失败");
            ShowNotification("复制失败", ex.Message, Avalonia.Controls.Notifications.NotificationType.Error);
        }
    }

    /// <summary>
    /// 显示应用内通知
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    /// <param name="type">通知类型</param>
    public static void ShowNotification(string title, string message, Avalonia.Controls.Notifications.NotificationType type = Avalonia.Controls.Notifications.NotificationType.Success)
    {
        if (Program.WindowNotificationManager != null)
        {
            Program.WindowNotificationManager.Show(new Avalonia.Controls.Notifications.Notification(
                title,
                message,
                type,
                TimeSpan.FromSeconds(2)
            ));
        }
        else
        {
            Log.Warning("WindowNotificationManager 未初始化，无法显示通知");
        }
    }
}

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
    /// 状态文本
    /// </summary>
    public string StatusText => IsPendingDelete ? "待删除" : (IsEnabled ? "已启用" : "已禁用");

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

    public PluginSettingItem()
    {
        // 确保所有属性都有默认值，避免 null 引用
        UniqueName = string.Empty;
        PluginName = string.Empty;
        Version = string.Empty;
        Tools = new ObservableCollection<ToolInfo>();
        
        DeleteCommand = ReactiveCommand.CreateFromTask(DeletePlugin);
        CancelDeleteCommand = ReactiveCommand.CreateFromTask(CancelDelete);
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

            var toolRegistry = Infrastructure.ServiceLocator.Get<ToolRegistry>();
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
    private void TogglePluginEnabled(bool enabled)
    {
        // 确保基本属性已设置
        if (string.IsNullOrEmpty(UniqueName) || string.IsNullOrEmpty(PluginName))
        {
            Serilog.Log.Warning("尝试切换未完全初始化的插件状态");
            return;
        }

        try
        {
            PluginLoader? pluginLoader = Infrastructure.ServiceLocator.Get<PluginLoader>();
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
                bool success = pluginLoader.EnablePlugin(UniqueName);
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
            // 显示确认对话框
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var dialog = new Avalonia.Controls.Window
                {
                    Title = "确认删除",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                    CanResize = false
                };

                var content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 15
                };

                content.Children.Add(new TextBlock
                {
                    Text = $"确定要删除插件 \"{PluginName}\" 吗？",
                    FontSize = 14,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });

                content.Children.Add(new TextBlock
                {
                    Text = "此操作将删除插件文件和所有相关数据，且无法撤销。",
                    Foreground = Avalonia.Media.Brushes.OrangeRed,
                    FontSize = 12,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 10
                };

                var confirmButton = new Button
                {
                    Content = "确定",
                    Width = 80
                };

                var cancelButton = new Button
                {
                    Content = "取消",
                    Width = 80
                };

                bool? dialogResult = null;

                confirmButton.Click += (s, e) =>
                {
                    dialogResult = true;
                    dialog.Close();
                };

                cancelButton.Click += (s, e) =>
                {
                    dialogResult = false;
                    dialog.Close();
                };

                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(confirmButton);
                content.Children.Add(buttonPanel);

                dialog.Content = content;

                await dialog.ShowDialog(desktop.MainWindow);

                if (dialogResult != true)
                {
                    return; // 用户取消
                }

                // 显示加载提示
                EventAggregator.Instance.Publish(new ShowLoadingMessage("正在标记插件为待删除..."));

                // 调用 PluginLoader 标记插件为待删除
                PluginLoader pluginLoader = Infrastructure.ServiceLocator.Get<PluginLoader>();
                var (success, message) = await pluginLoader.MarkPluginForDeletionAsync(UniqueName);

                // 隐藏加载提示
                EventAggregator.Instance.Publish(new HideLoadingMessage());

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
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "删除插件 {PluginName} 失败", PluginName);
            EventAggregator.Instance.Publish(new HideLoadingMessage());
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
            PluginLoader pluginLoader = Infrastructure.ServiceLocator.Get<PluginLoader>();
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
}

