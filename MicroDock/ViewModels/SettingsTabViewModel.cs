using Avalonia.Media;
using MicroDock.Database;
using MicroDock.Services;
using MicroDock.Infrastructure;
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
        LoadSettings();
        
        // 订阅服务状态变更通知
        EventAggregator.Instance.Subscribe<ServiceStateChangedMessage>(OnServiceStateChanged);
    }
    
    /// <summary>
    /// 处理服务状态变更通知
    /// </summary>
    private void OnServiceStateChanged(ServiceStateChangedMessage message)
    {
        // 当服务状态从外部变更时，同步到ViewModel
        switch (message.ServiceName)
        {
            case "AutoStartup":
                if (_autoStartup != message.IsEnabled)
                {
                    _autoStartup = message.IsEnabled;
                    this.RaisePropertyChanged(nameof(AutoStartup));
                }
                break;
            case "AutoHide":
                if (_autoHide != message.IsEnabled)
                {
                    _autoHide = message.IsEnabled;
                    this.RaisePropertyChanged(nameof(AutoHide));
                }
                break;
            case "AlwaysOnTop":
                if (_alwaysOnTop != message.IsEnabled)
                {
                    _alwaysOnTop = message.IsEnabled;
                    this.RaisePropertyChanged(nameof(AlwaysOnTop));
                }
                break;
            case "MiniMode":
                if (_isMiniModeEnabled != message.IsEnabled)
                {
                    _isMiniModeEnabled = message.IsEnabled;
                    this.RaisePropertyChanged(nameof(IsMiniModeEnabled));
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
            }
        });
    }
}

