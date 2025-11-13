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
    private bool _isMiniModeEnabled;
    private bool _showLogViewer;
    private Color _customAccentColor = Color.FromRgb(255, 0, 0);

    // P1: 迷你模式配置
    private int _longPressMs = 500;
    private double _miniRadius = 60;
    private double _miniItemSize = 40;
    private double _miniStartAngle = -90;
    private double _miniSweepAngle = 360;
    private bool _miniAutoDynamicArc = true;
    private bool _miniAutoCollapseAfterTrigger = true;

    public SettingsTabViewModel()
    {
        Applications = new ObservableCollection<ApplicationDB>(DBContext.GetApplications());
        AddApplicationCommand = ReactiveCommand.CreateFromTask(AddApplication);
        RemoveApplicationCommand = ReactiveCommand.Create<ApplicationDB>(RemoveApplication);
        PluginSettings = new ObservableCollection<PluginSettingItem>();
        LoadSettings();

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
            case "MiniMode":
                if (_isMiniModeEnabled != message.IsEnabled)
                {
                    _isMiniModeEnabled = message.IsEnabled;
                    this.RaisePropertyChanged(nameof(IsMiniModeEnabled));
                    SaveSetting(nameof(IsMiniModeEnabled), message.IsEnabled);
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

    public bool IsMiniModeEnabled
    {
        get => _isMiniModeEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _isMiniModeEnabled, value);
            SaveSetting(nameof(IsMiniModeEnabled), value);
            // 通过事件请求改变服务状态
            EventAggregator.Instance.Publish(new MiniModeChangeRequestMessage(value));
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

    // === 迷你模式配置（P1） ===
    public int LongPressMs
    {
        get => _longPressMs;
        set
        {
            this.RaiseAndSetIfChanged(ref _longPressMs, value);
            DBContext.UpdateSetting(s => s.LongPressMs = value);
        }
    }

    public double MiniRadius
    {
        get => _miniRadius;
        set
        {
            this.RaiseAndSetIfChanged(ref _miniRadius, value);
            DBContext.UpdateSetting(s => s.MiniRadius = value);
        }
    }

    public double MiniItemSize
    {
        get => _miniItemSize;
        set
        {
            this.RaiseAndSetIfChanged(ref _miniItemSize, value);
            DBContext.UpdateSetting(s => s.MiniItemSize = value);
        }
    }

    public double MiniStartAngle
    {
        get => _miniStartAngle;
        set
        {
            this.RaiseAndSetIfChanged(ref _miniStartAngle, value);
            DBContext.UpdateSetting(s => s.MiniStartAngle = value);
        }
    }

    public double MiniSweepAngle
    {
        get => _miniSweepAngle;
        set
        {
            this.RaiseAndSetIfChanged(ref _miniSweepAngle, value);
            DBContext.UpdateSetting(s => s.MiniSweepAngle = value);
        }
    }

    public bool MiniAutoDynamicArc
    {
        get => _miniAutoDynamicArc;
        set
        {
            this.RaiseAndSetIfChanged(ref _miniAutoDynamicArc, value);
            DBContext.UpdateSetting(s => s.MiniAutoDynamicArc = value);
        }
    }

    public bool MiniAutoCollapseAfterTrigger
    {
        get => _miniAutoCollapseAfterTrigger;
        set
        {
            this.RaiseAndSetIfChanged(ref _miniAutoCollapseAfterTrigger, value);
            DBContext.UpdateSetting(s => s.MiniAutoCollapseAfterTrigger = value);
        }
    }

    public List<Color> PredefinedColors => AppConfig.PredefAccentColors;
    public Color CustomAccentColor
    {
        get => _customAccentColor;
        set
        {
            this.RaiseAndSetIfChanged(ref _customAccentColor, value);
            //// 无论值是否改变，都要保存并触发事件，确保 Service 状态同步
            //SaveSetting(nameof(AlwaysOnTop), value);
            //SettingChanged?.Invoke(nameof(AlwaysOnTop), value);
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
        _isMiniModeEnabled = settings.IsMiniModeEnabled;
        _showLogViewer = settings.ShowLogViewer;

        // 迷你模式配置
        _longPressMs = settings.LongPressMs;
        _miniRadius = settings.MiniRadius;
        _miniItemSize = settings.MiniItemSize;
        _miniStartAngle = settings.MiniStartAngle;
        _miniSweepAngle = settings.MiniSweepAngle;
        _miniAutoDynamicArc = settings.MiniAutoDynamicArc;
        _miniAutoCollapseAfterTrigger = settings.MiniAutoCollapseAfterTrigger;

        // 通知UI更新（仅UI，不触发setter中的事件发布）
        this.RaisePropertyChanged(nameof(AutoStartup));
        this.RaisePropertyChanged(nameof(AutoHide));
        this.RaisePropertyChanged(nameof(AlwaysOnTop));
        this.RaisePropertyChanged(nameof(IsMiniModeEnabled));
        this.RaisePropertyChanged(nameof(ShowLogViewer));

        this.RaisePropertyChanged(nameof(LongPressMs));
        this.RaisePropertyChanged(nameof(MiniRadius));
        this.RaisePropertyChanged(nameof(MiniItemSize));
        this.RaisePropertyChanged(nameof(MiniStartAngle));
        this.RaisePropertyChanged(nameof(MiniSweepAngle));
        this.RaisePropertyChanged(nameof(MiniAutoDynamicArc));
        this.RaisePropertyChanged(nameof(MiniAutoCollapseAfterTrigger));
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
                case nameof(IsMiniModeEnabled):
                    settings.IsMiniModeEnabled = value;
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
            ShowNotification("复制失败", ex.Message, NotificationType.Error);
        }
    }
    
    /// <summary>
    /// 显示应用内通知
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    /// <param name="type">通知类型</param>
    private static void ShowNotification(string title, string message, NotificationType type = NotificationType.Success)
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

