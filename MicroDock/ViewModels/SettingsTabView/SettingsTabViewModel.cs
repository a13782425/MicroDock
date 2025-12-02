using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HarfBuzzSharp;
using MicroDock.Database;
using MicroDock.Extension;
using MicroDock.Model;
using MicroDock.Plugin;
using MicroDock.Service;
using MicroDock.Utils;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

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

    // Command to save order after reordering
    public ReactiveCommand<Unit, Unit> SaveTabOrderCommand { get; }

    public SettingsTabViewModel()
    {
        SaveTabOrderCommand = ReactiveCommand.Create(SaveTabOrder);
        ImportPluginCommand = ReactiveCommand.CreateFromTask(ImportPlugin);
        OpenPluginsFolderCommand = ReactiveCommand.Create(OpenPluginsFolder);
        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnection);
        BackupAppDataCommand = ReactiveCommand.CreateFromTask(BackupAppData);
        RestoreAppDataCommand = ReactiveCommand.CreateFromTask(RestoreAppData);
        InstallPluginCommand = ReactiveCommand.CreateFromTask(InstallPluginFromServer);

        PluginSettings = new ObservableCollection<PluginSettingItem>();
        AvailableThemes = new ObservableCollection<MicroDock.Model.ThemeModel>();
        FlattenedThemeList = new ObservableCollection<object>();
        _groupedThemes = new List<IGrouping<string, MicroDock.Model.ThemeModel>>();

        NavigationTabs = new ObservableCollection<NavigationTabSettingItem>();

        LoadSettings();
        LoadThemes();
        LoadNavigationTabs();

        // 加载插件设置（使用单例实例）
        LoadPluginSettings();

        // 订阅服务状态变更通知
        ServiceLocator.Get<EventService>()?.Subscribe<ServiceStateChangedMessage>(OnServiceStateChanged);

        // 订阅插件事件
        ServiceLocator.Get<EventService>()?.Subscribe<PluginImportedMessage>(OnPluginImported);
        ServiceLocator.Get<EventService>()?.Subscribe<PluginDeletedMessage>(OnPluginDeleted);
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
            ServiceLocator.Get<EventService>().Publish(new AutoStartupChangeRequestMessage(value));
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
            ServiceLocator.Get<EventService>().Publish(new AutoHideChangeRequestMessage(value));
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
            ServiceLocator.Get<EventService>().Publish(new WindowTopmostChangeRequestMessage(value));
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
            ServiceLocator.Get<EventService>().Publish(new NavigationTabVisibilityChangedMessage(NAVIGATION_LOG_ID, value));
        }
    }

    /// <summary>
    /// 可用主题列表（用于内部管理）
    /// </summary>
    public ObservableCollection<ThemeModel> AvailableThemes { get; }

    /// <summary>
    /// 扁平化主题列表（包含分组标题和主题项）
    /// </summary>
    public ObservableCollection<object> FlattenedThemeList { get; }

    private IEnumerable<IGrouping<string, ThemeModel>>? _groupedThemes;

    /// <summary>
    /// 分组后的主题列表（用于 GroupedComboBox 控件）
    /// </summary>
    public IEnumerable<IGrouping<string, ThemeModel>>? GroupedThemes
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
                var themeService = ServiceLocator.Get<Service.ThemeService>();
                themeService.LoadAndApplyTheme(value);

                // 同步更新 SelectedThemeModel
                UpdateSelectedThemeModel();
            }
        }
    }

    private ThemeModel? _selectedThemeModel;

    private bool _isUpdatingThemeModel = false;

    /// <summary>
    /// 选中的主题模型（用于UI绑定）
    /// </summary>
    public ThemeModel? SelectedThemeModel
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
            if (value is MicroDock.Model.ThemeGroupHeader)
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
                        var themeService = ServiceLocator.Get<Service.ThemeService>();
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
            var theme = FlattenedThemeList.OfType<MicroDock.Model.ThemeModel>()
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

    public ObservableCollection<NavigationTabSettingItem> NavigationTabs { get; }
    // Drag and Drop MoveTab

    private void LoadNavigationTabs()
    {
        NavigationTabs.Clear();
        var dbTabs = DBContext.GetAllNavigationTabs().OrderBy(t => t.OrderIndex).ToList();
        var pluginService = ServiceLocator.Get<PluginService>();

        foreach (var pluginInfo in pluginService.LoadedPlugins)
        {
            if (pluginInfo.PluginInstance == null)
                continue;
            if (pluginInfo.PluginInstance.Tabs == null || pluginInfo.PluginInstance.Tabs.Length == 0)
                continue;
            foreach (var item in pluginInfo.PluginInstance.Tabs)
            {
                string uniqueId = pluginInfo.GetTabUniqueId(item);
                var navTab = DBContext.GetNavigationTab(uniqueId).GetDto();
                string name = item.TabName;
                if (!pluginInfo.IsEnabled)
                {
                    name = $"{name} (未启用)";
                }
                NavigationTabs.Add(new NavigationTabSettingItem(navTab as NavigationTabDto)
                {
                    Name = name,
                    IsEnable = pluginInfo.IsEnabled
                });
            }
        }
        NavigationTabs.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));
    }

    public void SaveTabOrder()
    {
        // Re-assign OrderIndex for all items to ensure consistency
        for (int i = 0; i < NavigationTabs.Count; i++)
        {
            var item = NavigationTabs[i];
            if (item.OrderIndex != i)
            {
                item.OrderIndex = i;
            }
        }

        ServiceLocator.Get<EventService>().Publish(new NavigationTabsConfigurationChangedMessage());
    }

    public void MoveTab(NavigationTabSettingItem source, NavigationTabSettingItem target)
    {
        if (source == target) return;

        int oldIndex = NavigationTabs.IndexOf(source);
        int newIndex = NavigationTabs.IndexOf(target);

        if (oldIndex < 0 || newIndex < 0) return;

        // Move in ObservableCollection
        NavigationTabs.Move(oldIndex, newIndex);

        // Re-assign OrderIndex for all items to ensure consistency
        for (int i = 0; i < NavigationTabs.Count; i++)
        {
            var item = NavigationTabs[i];
            if (item.OrderIndex != i)
            {
                item.OrderIndex = i;

                // Update DB
                var itemDB = DBContext.GetNavigationTab(item.UniqueId);
                if (itemDB != null)
                {
                    itemDB.OrderIndex = i;
                    DBContext.UpdateNavigationTab(itemDB);
                }
            }
        }

        ServiceLocator.Get<EventService>().Publish(new NavigationTabsConfigurationChangedMessage());
    }

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
        string pluginDirectory = Path.Combine(AppConfig.ROOT_PATH, "plugins");

        // 加载所有插件
        IReadOnlyList<PluginInfo> plugins = ServiceLocator.Get<PluginService>().LoadedPlugins;

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
            settingItem.IsPendingUpdate = dbInfo?.PendingUpdate ?? false;
            settingItem.PendingVersion = dbInfo?.PendingVersion;

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

            // 使用新的 StorageProvider API
            if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null)
                return;

            IStorageProvider? storageProvider = desktop.MainWindow.StorageProvider;
            if (storageProvider == null)
                return;

            // 定义文件类型过滤器
            var filePickerFileTypes = new FilePickerFileType[]
            {
                new("ZIP文件")
                {
                    Patterns = new[] { "*.zip" }
                },
                FilePickerFileTypes.All
            };

            var filePickerOptions = new FilePickerOpenOptions
            {
                Title = "选择插件ZIP文件",
                AllowMultiple = false,
                FileTypeFilter = filePickerFileTypes
            };

            IReadOnlyList<IStorageFile> result = await storageProvider.OpenFilePickerAsync(filePickerOptions);
            if (result != null && result.Count > 0)
            {
                string zipFilePath = result[0].Path.LocalPath;

                // 显示加载提示
                ServiceLocator.Get<EventService>().Publish(new ShowLoadingMessage("正在导入插件..."));

                try
                {
                    // 获取插件目录
                    string pluginDirectory = Path.Combine(AppConfig.ROOT_PATH, "plugins");

                    // 调用 PluginLoader 导入插件
                    PluginService pluginLoader = ServiceLocator.Get<PluginService>();
                    var (success, message, pluginName) = await pluginLoader.ImportPluginAsync(zipFilePath, pluginDirectory);

                    // 隐藏加载提示
                    ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());

                    if (success)
                    {
                        // 刷新插件列表
                        LoadPluginSettings();

                        // 发布插件导入事件
                        if (!string.IsNullOrEmpty(pluginName))
                        {
                            ServiceLocator.Get<EventService>().Publish(new PluginImportedMessage { PluginName = pluginName });
                        }

                        // 如果是更新插件（消息包含"重启"），询问是否立即重启
                        if (message.Contains("重启"))
                        {
                            bool restart = await ShowConfirmDialogAsync(
                                "导入成功",
                                message,
                                "是否立即重启应用以完成更新？"
                            );
                            if (restart)
                            {
                                Utils.UniversalUtils.RestartApplication("plugin_updated");
                            }
                        }
                        else
                        {
                            ShowNotification("导入成功", message);
                        }
                    }
                    else
                    {
                        ShowNotification("导入失败", message, AppNotificationType.Error);
                    }
                }
                catch (Exception ex)
                {
                    ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());
                    Log.Error(ex, "导入插件失败");
                    ShowNotification("导入失败", ex.Message, AppNotificationType.Error);
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

        // 加载服务器与备份设置
        _serverAddress = settings.ServerAddress ?? string.Empty;
        _backupServerAddress = settings.BackupServerAddress ?? string.Empty;
        _backupPassword = settings.BackupPassword ?? string.Empty;

        // 通知UI更新（仅UI，不触发setter中的事件发布）
        this.RaisePropertyChanged(nameof(AutoStartup));
        this.RaisePropertyChanged(nameof(AutoHide));
        this.RaisePropertyChanged(nameof(AlwaysOnTop));
        this.RaisePropertyChanged(nameof(ShowLogViewer));
        this.RaisePropertyChanged(nameof(SelectedTheme));
        this.RaisePropertyChanged(nameof(ServerAddress));
        this.RaisePropertyChanged(nameof(BackupServerAddress));
        this.RaisePropertyChanged(nameof(BackupPassword));

        // 更新备份时间显示
        UpdateLastBackupTime();
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

            Service.ThemeService themeService = ServiceLocator.Get<Service.ThemeService>();
            List<MicroDock.Model.ThemeModel> themes = themeService.GetAvailableThemes();

            // 添加到内部列表
            foreach (MicroDock.Model.ThemeModel theme in themes)
            {
                AvailableThemes.Add(theme);
            }

            // 按 Category 分组
            List<IGrouping<string, MicroDock.Model.ThemeModel>> groupedThemes = themes
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
            foreach (IGrouping<string, MicroDock.Model.ThemeModel> group in groupedThemes)
            {
                // 添加分组标题
                FlattenedThemeList.Add(new MicroDock.Model.ThemeGroupHeader
                {
                    GroupName = group.Key
                });

                // 添加该组的主题（按 DisplayName 排序）
                foreach (MicroDock.Model.ThemeModel theme in group.OrderBy(t => t.DisplayName))
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

    #region 服务器与备份设置

    private string _serverAddress = string.Empty;
    private string _backupServerAddress = string.Empty;
    private string _backupPassword = string.Empty;
    private string _lastAppBackupTimeText = "从未备份";

    /// <summary>
    /// 服务器地址
    /// </summary>
    public string ServerAddress
    {
        get => _serverAddress;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _serverAddress, value) == value)
            {
                DBContext.UpdateSetting(s => s.ServerAddress = value);
            }
        }
    }

    /// <summary>
    /// 备份服务器地址（专用于数据备份，可选，为空时使用 ServerAddress）
    /// </summary>
    public string BackupServerAddress
    {
        get => _backupServerAddress;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _backupServerAddress, value) == value)
            {
                DBContext.UpdateSetting(s => s.BackupServerAddress = value);
            }
        }
    }

    /// <summary>
    /// 备份密码
    /// </summary>
    public string BackupPassword
    {
        get => _backupPassword;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _backupPassword, value) == value)
            {
                DBContext.UpdateSetting(s => s.BackupPassword = value);
            }
        }
    }

    /// <summary>
    /// 上次备份时间显示文本
    /// </summary>
    public string LastAppBackupTimeText
    {
        get => _lastAppBackupTimeText;
        private set => this.RaiseAndSetIfChanged(ref _lastAppBackupTimeText, value);
    }

    /// <summary>
    /// 打开插件文件夹命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenPluginsFolderCommand { get; }

    /// <summary>
    /// 测试服务器连接命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; }

    /// <summary>
    /// 备份主程序数据命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> BackupAppDataCommand { get; }

    /// <summary>
    /// 恢复主程序数据命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RestoreAppDataCommand { get; }

    /// <summary>
    /// 安装插件命令（从服务器）
    /// </summary>
    public ReactiveCommand<Unit, Unit> InstallPluginCommand { get; }

    /// <summary>
    /// 打开插件根目录
    /// </summary>
    private void OpenPluginsFolder()
    {
        try
        {
            string pluginDirectory = Path.Combine(AppConfig.ROOT_PATH, "plugins");
            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
            }
            ServiceLocator.Get<IPlatformService>()?.OpenExplorer(pluginDirectory);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开插件文件夹失败");
            ShowNotification("打开失败", ex.Message, AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 测试服务器连接
    /// </summary>
    private async Task TestConnection()
    {
        if (string.IsNullOrEmpty(ServerAddress))
        {
            ShowNotification("测试失败", "请先输入服务器地址", AppNotificationType.Warning);
            return;
        }

        ServiceLocator.Get<EventService>()?.Publish(new ShowLoadingMessage("正在测试连接..."));

        try
        {
            var (success, message) = await PluginServerApiClient.TestConnectionAsync(ServerAddress);

            ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());

            if (success)
            {
                ShowNotification("连接成功", message, AppNotificationType.Success);
            }
            else
            {
                ShowNotification("连接失败", message, AppNotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());
            Log.Error(ex, "测试服务器连接失败");
            ShowNotification("测试失败", ex.Message, AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 备份主程序数据
    /// </summary>
    private async Task BackupAppData()
    {
        if (string.IsNullOrEmpty(ServerAddress))
        {
            ShowNotification("备份失败", "请先配置服务器地址", AppNotificationType.Warning);
            return;
        }

        if (string.IsNullOrEmpty(BackupPassword))
        {
            ShowNotification("备份失败", "请先配置备份密码", AppNotificationType.Warning);
            return;
        }

        bool confirm = await ShowConfirmDialogAsync(
            "备份主程序数据",
            "确定要备份主程序数据库到服务器吗？",
            "这将覆盖服务器上已有的备份。"
        );

        if (!confirm) return;

        ServiceLocator.Get<EventService>()?.Publish(new ShowLoadingMessage("正在备份主程序数据..."));

        try
        {
            var (success, message) = await PluginServerApiClient.BackupAppDataAsync();

            ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());

            if (success)
            {
                UpdateLastBackupTime();
                ShowNotification("备份成功", message, AppNotificationType.Success);
            }
            else
            {
                ShowNotification("备份失败", message, AppNotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());
            Log.Error(ex, "备份主程序数据失败");
            ShowNotification("备份失败", ex.Message, AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 恢复主程序数据
    /// </summary>
    private async Task RestoreAppData()
    {
        if (string.IsNullOrEmpty(ServerAddress) && string.IsNullOrEmpty(BackupServerAddress))
        {
            ShowNotification("恢复失败", "请先配置服务器地址或备份地址", AppNotificationType.Warning);
            return;
        }

        if (string.IsNullOrEmpty(BackupPassword))
        {
            ShowNotification("恢复失败", "请先配置备份密码", AppNotificationType.Warning);
            return;
        }

        ServiceLocator.Get<EventService>()?.Publish(new ShowLoadingMessage("正在获取备份列表..."));

        try
        {
            // 获取备份列表
            var response = await PluginServerApiClient.GetBackupListAsync();

            ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());

            if (!response.Success || response.Data?.Backups == null)
            {
                ShowNotification("获取失败", response.Message ?? "无法获取备份列表", AppNotificationType.Error);
                return;
            }

            // 筛选主程序备份
            var programBackups = response.Data.Backups
                .Where(b => b.BackupType == "program")
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            if (programBackups.Count == 0)
            {
                ShowNotification("提示", "服务器上暂无主程序备份", AppNotificationType.Information);
                return;
            }

            // 转换为列表项
            var backupItems = new ObservableCollection<BackupListItem>(
                programBackups.Select(b => new BackupListItem
                {
                    Id = b.Id,
                    BackupType = b.BackupType,
                    Description = b.Description ?? "",
                    CreatedAt = b.CreatedAt ?? "",
                    FileSize = b.FileSize,
                    PluginName = null
                })
            );

            // 显示备份列表对话框
            var selectedBackup = await ShowBackupListDialogAsync("恢复主程序数据", backupItems, "program");

            if (selectedBackup == null)
            {
                return; // 用户取消
            }

            // 确认恢复
            bool confirm = await ShowConfirmDialogAsync(
                "确认恢复",
                $"确定要恢复此备份吗？\n\n{selectedBackup.DisplayName}\n创建时间: {selectedBackup.FormattedCreatedAt}",
                "这将覆盖当前的数据库，应用需要重启才能生效。"
            );

            if (!confirm) return;

            ServiceLocator.Get<EventService>()?.Publish(new ShowLoadingMessage("正在恢复主程序数据..."));

            var (success, message) = await PluginServerApiClient.RestoreAppDataAsync(selectedBackup.Id);

            ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());

            if (success)
            {
                // 询问用户是否立即重启
                bool restart = await ShowConfirmDialogAsync(
                    "恢复成功",
                    message,
                    "是否立即重启应用以使更改生效？"
                );
                if (restart)
                {
                    Utils.UniversalUtils.RestartApplication("backup_restored");
                }
            }
            else
            {
                ShowNotification("恢复失败", message, AppNotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());
            LogError("恢复主程序数据失败", DEFAULT_LOG_TAG, ex);
            ShowNotification("恢复失败", ex.Message, AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 显示备份列表对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="backupItems">备份列表</param>
    /// <param name="backupType">备份类型用于刷新</param>
    /// <returns>选中的备份项，如果取消则返回 null</returns>
    private async Task<BackupListItem?> ShowBackupListDialogAsync(string title, ObservableCollection<BackupListItem> backupItems, string backupType)
    {
        BackupListItem? selectedBackup = null;
        bool needRefresh = false;

        while (true)
        {
            if (needRefresh)
            {
                // 重新获取备份列表
                ServiceLocator.Get<EventService>()?.Publish(new ShowLoadingMessage("正在刷新备份列表..."));
                var response = await PluginServerApiClient.GetBackupListAsync();
                ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());

                if (response.Success && response.Data?.Backups != null)
                {
                    backupItems.Clear();
                    var filteredBackups = response.Data.Backups
                        .Where(b => b.BackupType == backupType)
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
                            PluginName = null
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

            // 创建列表
            var listBox = new ListBox
            {
                MinWidth = 450,
                SelectionMode = SelectionMode.Single,
                ItemsSource = backupItems,
                ItemTemplate = CreateBackupListItemTemplate()
            };

            var scrollViewer = new ScrollViewer
            {
                MaxHeight = 350,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Content = listBox
            };

            var titleText = new TextBlock
            {
                Text = $"找到 {backupItems.Count} 个备份，选择一个进行操作：",
                FontSize = 13,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };

            // 删除按钮
            var deleteButton = new Button
            {
                Content = "删除选中",
                Margin = new Avalonia.Thickness(0, 10, 0, 0),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };

            var contentPanel = new StackPanel
            {
                Spacing = 0,
                Children = { titleText, scrollViewer, deleteButton }
            };

            // 用于跟踪是否需要删除
            bool deleteRequested = false;
            deleteButton.Click += (s, e) => { deleteRequested = true; };

            // 显示对话框
            var dialogResult = await UniversalUtils.ShowCustomDialogAsync(
                title,
                contentPanel,
                "恢复",
                "取消"
            );

            if (deleteRequested && listBox.SelectedItem is BackupListItem itemToDelete)
            {
                // 用户点击了删除按钮
                bool confirmDelete = await ShowConfirmDialogAsync(
                    "确认删除",
                    $"确定要删除此备份吗？\n\n{itemToDelete.DisplayName}\n创建时间: {itemToDelete.FormattedCreatedAt}",
                    "此操作不可撤销。"
                );

                if (confirmDelete)
                {
                    ServiceLocator.Get<EventService>()?.Publish(new ShowLoadingMessage("正在删除备份..."));
                    var (success, message) = await PluginServerApiClient.DeleteBackupAsync(itemToDelete.Id);
                    ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());

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

            if (dialogResult == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
            {
                selectedBackup = listBox.SelectedItem as BackupListItem;
                if (selectedBackup == null)
                {
                    ShowNotification("提示", "请先选择一个备份", AppNotificationType.Warning);
                    continue; // 重新显示对话框
                }
                return selectedBackup;
            }

            // 用户取消
            return null;
        }
    }

    /// <summary>
    /// 创建备份列表项模板
    /// </summary>
    private static Avalonia.Controls.Templates.FuncDataTemplate<BackupListItem> CreateBackupListItemTemplate()
    {
        return new Avalonia.Controls.Templates.FuncDataTemplate<BackupListItem>((item, _) =>
        {
            var border = new Border
            {
                Padding = new Avalonia.Thickness(10, 8),
                Margin = new Avalonia.Thickness(2),
                CornerRadius = new Avalonia.CornerRadius(6),
                Background = Avalonia.Media.Brushes.Transparent
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*, Auto")
            };

            var leftPanel = new StackPanel { Spacing = 2 };

            // 描述/名称
            var nameText = new TextBlock
            {
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                FontSize = 14
            };
            nameText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("DisplayName"));
            leftPanel.Children.Add(nameText);

            // 创建时间和文件大小
            var infoPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 12 };
            var timeText = new TextBlock { FontSize = 11, Opacity = 0.6 };
            timeText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("FormattedCreatedAt") { StringFormat = "创建时间: {0}" });
            infoPanel.Children.Add(timeText);

            var sizeText = new TextBlock { FontSize = 11, Opacity = 0.6 };
            sizeText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("FormattedFileSize") { StringFormat = "大小: {0}" });
            infoPanel.Children.Add(sizeText);

            leftPanel.Children.Add(infoPanel);

            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            border.Child = grid;
            return border;
        });
    }

    /// <summary>
    /// 从服务器安装插件
    /// </summary>
    private async Task InstallPluginFromServer()
    {
        if (string.IsNullOrEmpty(ServerAddress))
        {
            ShowNotification("安装失败", "请先配置服务器地址", AppNotificationType.Warning);
            return;
        }

        ServiceLocator.Get<EventService>()?.Publish(new ShowLoadingMessage("正在获取插件列表..."));

        try
        {
            // 获取服务器插件列表
            var response = await PluginServerApiClient.GetPluginListAsync();

            ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());

            if (!response.Success || response.Data == null)
            {
                ShowNotification("获取失败", response.Message ?? "无法获取插件列表", AppNotificationType.Error);
                return;
            }

            var plugins = response.Data;
            if (plugins.Count == 0)
            {
                ShowNotification("提示", "服务器上暂无可用插件", AppNotificationType.Information);
                return;
            }

            // 获取已安装的插件名称列表
            var installedPlugins = PluginSettings.Select(p => p.UniqueName).ToHashSet();

            // 构建插件列表数据
            var pluginItems = new ObservableCollection<RemotePluginListItem>();
            foreach (var plugin in plugins.Where(p => p.IsEnabled && !p.IsDeprecated))
            {
                var isInstalled = installedPlugins.Contains(plugin.Name);
                var installedPlugin = PluginSettings.FirstOrDefault(p => p.UniqueName == plugin.Name);
                var needsUpdate = isInstalled && installedPlugin != null &&
                                  !string.IsNullOrEmpty(plugin.CurrentVersion) &&
                                  plugin.CurrentVersion != installedPlugin.Version;

                pluginItems.Add(new RemotePluginListItem
                {
                    Name = plugin.Name,
                    DisplayName = plugin.DisplayName,
                    Description = plugin.Description ?? "",
                    Author = plugin.Author ?? "未知",
                    Version = plugin.CurrentVersion,
                    IsInstalled = isInstalled,
                    NeedsUpdate = needsUpdate,
                    InstalledVersion = installedPlugin?.Version
                });
            }

            // 构建插件列表 UI - 标题固定，列表滚动
            var listBox = new ListBox
            {
                MinWidth = 450,
                SelectionMode = SelectionMode.Single,
                ItemsSource = pluginItems,
                ItemTemplate = CreatePluginListItemTemplate()
            };

            // 使用 ScrollViewer 包裹 ListBox，确保只有列表滚动
            var scrollViewer = new ScrollViewer
            {
                MaxHeight = 350,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Content = listBox
            };

            // 标题固定在顶部
            var titleText = new TextBlock
            {
                Text = $"找到 {pluginItems.Count} 个可用插件，选择一个进行安装：",
                FontSize = 13,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };

            var contentPanel = new StackPanel
            {
                Spacing = 0,
                Children = { titleText, scrollViewer }
            };

            // 显示对话框
            var dialogResult = await UniversalUtils.ShowCustomDialogAsync(
                "安装插件",
                contentPanel,
                "安装",
                "取消"
            );

            if (dialogResult == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
            {
                var selectedItem = listBox.SelectedItem as RemotePluginListItem;
                if (selectedItem == null)
                {
                    ShowNotification("提示", "请先选择一个插件", AppNotificationType.Warning);
                    return;
                }

                await DownloadAndInstallPluginAsync(selectedItem.Name, selectedItem.DisplayName);
            }
        }
        catch (Exception ex)
        {
            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());
            Log.Error(ex, "获取插件列表失败");
            ShowNotification("获取失败", ex.Message, AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 下载并安装插件
    /// </summary>
    private async Task DownloadAndInstallPluginAsync(string pluginName, string displayName)
    {
        ServiceLocator.Get<EventService>()?.Publish(new ShowLoadingMessage($"正在下载 {displayName}..."));

        try
        {
            // 下载插件
            var (success, message, data) = await PluginServerApiClient.DownloadPluginAsync(pluginName);

            if (!success || data == null)
            {
                ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());
                ShowNotification("下载失败", message, AppNotificationType.Error);
                return;
            }

            // 保存到临时文件
            string tempZipPath = Path.Combine(AppConfig.TEMP_BACKUP_FOLDER, $"plugin_install_{pluginName}_{DateTime.Now:yyyyMMddHHmmss}.zip");
            await File.WriteAllBytesAsync(tempZipPath, data);

            ServiceLocator.Get<EventService>()?.Publish(new ShowLoadingMessage($"正在安装 {displayName}..."));

            try
            {
                // 调用 PluginService 导入插件
                string pluginDirectory = Path.Combine(AppConfig.ROOT_PATH, "plugins");
                PluginService? pluginLoader = ServiceLocator.Get<PluginService>();
                var (installSuccess, installMessage, installedPluginName) = await pluginLoader.ImportPluginAsync(tempZipPath, pluginDirectory);

                ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());

                if (installSuccess)
                {
                    // 刷新插件列表
                    LoadPluginSettings();

                    // 发布插件导入事件
                    if (!string.IsNullOrEmpty(installedPluginName))
                    {
                        ServiceLocator.Get<EventService>()?.Publish(new PluginImportedMessage { PluginName = installedPluginName });
                    }

                    // 如果是更新插件（消息包含"重启"），询问是否立即重启
                    if (installMessage.Contains("重启"))
                    {
                        bool restart = await ShowConfirmDialogAsync(
                            "安装成功",
                            installMessage,
                            "是否立即重启应用以完成更新？"
                        );
                        if (restart)
                        {
                            Utils.UniversalUtils.RestartApplication("plugin_updated");
                        }
                    }
                    else
                    {
                        ShowNotification("安装成功", installMessage, AppNotificationType.Success);
                    }
                }
                else
                {
                    ShowNotification("安装失败", installMessage, AppNotificationType.Error);
                }
            }
            finally
            {
                // 清理临时文件
                if (File.Exists(tempZipPath))
                {
                    try { File.Delete(tempZipPath); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            ServiceLocator.Get<EventService>()?.Publish(new HideLoadingMessage());
            Log.Error(ex, "下载安装插件失败: {PluginName}", pluginName);
            ShowNotification("安装失败", ex.Message, AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 创建插件列表项模板
    /// </summary>
    private static Avalonia.Controls.Templates.FuncDataTemplate<RemotePluginListItem> CreatePluginListItemTemplate()
    {
        return new Avalonia.Controls.Templates.FuncDataTemplate<RemotePluginListItem>((item, _) =>
        {
            var border = new Border
            {
                Padding = new Avalonia.Thickness(10, 8),
                Margin = new Avalonia.Thickness(2),
                CornerRadius = new Avalonia.CornerRadius(6),
                Background = Avalonia.Media.Brushes.Transparent
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*, Auto")
            };

            var leftPanel = new StackPanel { Spacing = 2 };

            var namePanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
            var nameText = new TextBlock
            {
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                FontSize = 14
            };
            nameText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("DisplayName"));
            namePanel.Children.Add(nameText);

            // 已安装/可更新标签
            var installedBadge = new Border
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#22C55E")),
                CornerRadius = new Avalonia.CornerRadius(3),
                Padding = new Avalonia.Thickness(6, 2),
                Child = new TextBlock
                {
                    Text = "已安装",
                    FontSize = 10,
                    Foreground = Avalonia.Media.Brushes.White
                }
            };
            installedBadge.Bind(Border.IsVisibleProperty, new Avalonia.Data.Binding("IsInstalled"));
            namePanel.Children.Add(installedBadge);

            var updateBadge = new Border
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3B82F6")),
                CornerRadius = new Avalonia.CornerRadius(3),
                Padding = new Avalonia.Thickness(6, 2),
                Child = new TextBlock
                {
                    Text = "可更新",
                    FontSize = 10,
                    Foreground = Avalonia.Media.Brushes.White
                }
            };
            updateBadge.Bind(Border.IsVisibleProperty, new Avalonia.Data.Binding("NeedsUpdate"));
            namePanel.Children.Add(updateBadge);

            leftPanel.Children.Add(namePanel);

            var descText = new TextBlock
            {
                FontSize = 12,
                Opacity = 0.7,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                MaxWidth = 350
            };
            descText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("Description"));
            leftPanel.Children.Add(descText);

            var infoPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 12 };
            var authorText = new TextBlock { FontSize = 11, Opacity = 0.6 };
            authorText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("Author") { StringFormat = "作者: {0}" });
            infoPanel.Children.Add(authorText);

            var versionText = new TextBlock { FontSize = 11, Opacity = 0.6 };
            versionText.Bind(TextBlock.TextProperty, new Avalonia.Data.Binding("Version") { StringFormat = "v{0}" });
            infoPanel.Children.Add(versionText);

            leftPanel.Children.Add(infoPanel);

            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            border.Child = grid;
            return border;
        });
    }

    /// <summary>
    /// 更新上次备份时间显示
    /// </summary>
    private void UpdateLastBackupTime()
    {
        var settings = DBContext.GetSetting();
        if (settings.LastAppBackupTime > 0)
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(settings.LastAppBackupTime).LocalDateTime;
            LastAppBackupTimeText = dt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        else
        {
            LastAppBackupTimeText = "从未备份";
        }
    }

    #endregion

}

/// <summary>
/// 远程插件列表项（用于对话框显示）
/// </summary>
public class RemotePluginListItem
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public bool NeedsUpdate { get; set; }
    public string? InstalledVersion { get; set; }
}

/// <summary>
/// 备份列表项（用于备份选择对话框显示）
/// </summary>
public class BackupListItem
{
    public int Id { get; set; }
    public string BackupType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? PluginName { get; set; }

    /// <summary>
    /// 格式化的创建时间
    /// </summary>
    public string FormattedCreatedAt
    {
        get
        {
            if (DateTime.TryParse(CreatedAt, out var dt))
            {
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            }
            return CreatedAt;
        }
    }

    /// <summary>
    /// 格式化的文件大小
    /// </summary>
    public string FormattedFileSize
    {
        get
        {
            if (FileSize < 1024)
                return $"{FileSize} B";
            if (FileSize < 1024 * 1024)
                return $"{FileSize / 1024.0:F1} KB";
            return $"{FileSize / (1024.0 * 1024.0):F2} MB";
        }
    }

    /// <summary>
    /// 显示名称（用于列表显示）
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Description) ? $"备份 #{Id}" : Description;
}
