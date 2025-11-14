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

            PluginSettingItem settingItem = new PluginSettingItem
            {
                UniqueName = pluginInfo.UniqueName,
                PluginName = pluginInfo.Name, // 使用 UniqueName (plugin.json 中的 name 字段)
                PluginInstance = pluginInfo.PluginInstance,
                SettingsControl = settingsControl
            };

            // 加载插件的工具
            settingItem.LoadTools();

            PluginSettings.Add(settingItem);
        }
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
    private static void ShowNotification(string title, string message, Avalonia.Controls.Notifications.NotificationType type = Avalonia.Controls.Notifications.NotificationType.Success)
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
    /// 插件注册的工具列表
    /// </summary>
    public ObservableCollection<ToolInfo> Tools { get; set; } = new ObservableCollection<ToolInfo>();

    /// <summary>
    /// 是否有工具
    /// </summary>
    public bool HasTools => Tools.Count > 0;

    /// <summary>
    /// 工具数量
    /// </summary>
    public int ToolCount => Tools.Count;

    /// <summary>
    /// 工具数量显示文本
    /// </summary>
    public string ToolCountText => ToolCount > 0 ? $"({ToolCount} 个工具)" : "(无工具)";

    /// <summary>
    /// 加载插件的工具
    /// </summary>
    public void LoadTools()
    {
        if (string.IsNullOrEmpty(UniqueName))
            return;

        Tools.Clear();
        var tools = Infrastructure.ServiceLocator.Get<ToolRegistry>().GetPluginTools(UniqueName);
        foreach (var tool in tools)
        {
            Tools.Add(tool);
        }

        this.RaisePropertyChanged(nameof(Tools));
        this.RaisePropertyChanged(nameof(HasTools));
        this.RaisePropertyChanged(nameof(ToolCount));
        this.RaisePropertyChanged(nameof(ToolCountText));
    }
}

