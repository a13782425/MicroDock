using System;
using MicroDock.Database;
using ReactiveUI;

namespace MicroDock.ViewModels;

/// <summary>
/// 设置页签的 ViewModel
/// </summary>
public class SettingsTabViewModel : ViewModelBase
{
    private bool _autoStartup;
    private bool _autoHide;
    private bool _alwaysOnTop;
    
    /// <summary>
    /// 配置变更事件，通知Window层应用配置
    /// 参数1: 配置名称, 参数2: 配置值
    /// </summary>
    public event Action<string, bool>? SettingChanged;
    
    public SettingsTabViewModel()
    {
        LoadSettings();
    }
    
    /// <summary>
    /// 是否开机自启动
    /// </summary>
    public bool AutoStartup
    {
        get => _autoStartup;
        set
        {
            bool changed = this.RaiseAndSetIfChanged(ref _autoStartup, value);
            // 无论值是否改变，都要保存并触发事件，确保 Service 状态同步
            SaveSetting(nameof(AutoStartup), value);
            SettingChanged?.Invoke(nameof(AutoStartup), value);
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
            bool changed = this.RaiseAndSetIfChanged(ref _autoHide, value);
            // 无论值是否改变，都要保存并触发事件，确保 Service 状态同步
            SaveSetting(nameof(AutoHide), value);
            SettingChanged?.Invoke(nameof(AutoHide), value);
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
            bool changed = this.RaiseAndSetIfChanged(ref _alwaysOnTop, value);
            // 无论值是否改变，都要保存并触发事件，确保 Service 状态同步
            SaveSetting(nameof(AlwaysOnTop), value);
            SettingChanged?.Invoke(nameof(AlwaysOnTop), value);
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
        
        // 通知UI更新
        this.RaisePropertyChanged(nameof(AutoStartup));
        this.RaisePropertyChanged(nameof(AutoHide));
        this.RaisePropertyChanged(nameof(AlwaysOnTop));
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
            }
        });
    }
}

