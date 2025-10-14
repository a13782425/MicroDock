using Avalonia.Media;
using MicroDock.Database;
using MicroDock.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using Avalonia.Controls;

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
    private Color _customAccentColor = Color.FromRgb(255,0,0);
    
    private AutoStartupService? _autoStartupService;
    private AutoHideService? _autoHideService;
    private TopMostService? _topMostService;
    private MiniModeService? _miniModeService;

    public SettingsTabViewModel()
    {
        Applications = new ObservableCollection<ApplicationDB>(DBContext.GetApplications());
        AddApplicationCommand = ReactiveCommand.CreateFromTask(AddApplication);
        RemoveApplicationCommand = ReactiveCommand.Create<ApplicationDB>(RemoveApplication);
        LoadSettings();
    }

    /// <summary>
    /// 初始化服务，由主窗口调用
    /// </summary>
    public void InitializeServices(AutoStartupService autoStartupService, AutoHideService autoHideService, TopMostService topMostService, MiniModeService miniModeService)
    {
        _autoStartupService = autoStartupService;
        _autoHideService = autoHideService;
        _topMostService = topMostService;
        _miniModeService = miniModeService;

        // 应用初始配置
        ApplyServiceState(_autoStartupService, AutoStartup);
        ApplyServiceState(_autoHideService, AutoHide);
        ApplyServiceState(_topMostService, AlwaysOnTop);
        ApplyMiniModeServiceState(_miniModeService, IsMiniModeEnabled);
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
            ApplyServiceState(_autoStartupService, value);
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
            ApplyServiceState(_autoHideService, value);
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
            ApplyServiceState(_topMostService, value);
        }
    }

    public bool IsMiniModeEnabled
    {
        get => _isMiniModeEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _isMiniModeEnabled, value);
            SaveSetting(nameof(IsMiniModeEnabled), value);
            ApplyMiniModeServiceState(_miniModeService, value);
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

    private async System.Threading.Tasks.Task AddApplication()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要添加的应用程序",
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>
            {
                new() { Name = "Applications", Extensions = { "exe", "lnk" } },
                new() { Name = "All files", Extensions = { "*" } }
            }
        };

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var result = await dialog.ShowAsync(desktop.MainWindow);
            if (result != null && result.Length > 0)
            {
                var filePath = result[0];
                var app = new ApplicationDB
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = filePath
                };
                DBContext.AddApplication(app);
                Applications.Add(app);
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
    /// 从数据库加载配置
    /// </summary>
    private void LoadSettings()
    {
        SettingDB settings = DBContext.GetSetting();
        _autoStartup = settings.AutoStartup;
        _autoHide = settings.AutoHide;
        _alwaysOnTop = settings.AlwaysOnTop;
        _isMiniModeEnabled = settings.IsMiniModeEnabled;
        
        // 通知UI更新
        this.RaisePropertyChanged(nameof(AutoStartup));
        this.RaisePropertyChanged(nameof(AutoHide));
        this.RaisePropertyChanged(nameof(AlwaysOnTop));
        this.RaisePropertyChanged(nameof(IsMiniModeEnabled));
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
            }
        });
    }

    private void ApplyServiceState(IWindowService? service, bool enable)
    {
        if (service == null) return;

        if (enable)
            service.Enable();
        else
            service.Disable();
    }
    
    private void ApplyMiniModeServiceState(MiniModeService? service, bool enable)
    {
        if (service == null) return;

        if (enable)
            service.Enable();
        else
            service.Disable();
    }
}

