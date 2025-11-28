using DynamicData.Binding;
using MicroDock.Model;
using MicroDock.Service;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace MicroDock.ViewModels;

/// <summary>
/// 导航项模型
/// </summary>
public class NavigationItemModel : ReactiveObject, IDisposable
{
    private string _title = string.Empty;
    private string _icon = string.Empty;
    private object? _content;
    private NavigationType _navType;
    private string _pluginUniqueName = string.Empty;
    private string _uniqueId = string.Empty;
    private bool _isVisible = true;
    private bool _isEnabled = true;
    private int _order = 0;
    private bool _isLocked = false;
    private bool _isUnlocked = false;
    private readonly CompositeDisposable _cleanUp = new();
    private NavigationTabDto _tabDto;
#pragma warning disable CS8618
    public NavigationItemModel(NavigationTabDto? navigationTabDto)
#pragma warning restore CS8618 
    {
        if (navigationTabDto == null)
        {
            UniqueId = "";
            return;
        }
        this._tabDto = navigationTabDto;
        UniqueId = _tabDto.Id;
        Order = _tabDto.OrderIndex;
        IsVisible = _tabDto.IsVisible;
        
        // 初始化锁定状态
        _isLocked = _tabDto.IsLocked;
        UpdateUnlockedState();

        _tabDto.WhenValueChanged(a => a.IsVisible)
            .Subscribe(_ => IsVisible = _tabDto.IsVisible)
            .DisposeWith(_cleanUp);
        _tabDto.WhenValueChanged(a => a.OrderIndex)
            .Subscribe(_ => Order = _tabDto.OrderIndex)
            .DisposeWith(_cleanUp);
        _tabDto.WhenValueChanged(a => a.IsLocked)
            .Subscribe(_ => 
            {
                IsLocked = _tabDto.IsLocked;
                UpdateUnlockedState();
            })
            .DisposeWith(_cleanUp);

        // 订阅锁定状态变更消息
        ServiceLocator.Get<EventService>().Subscribe<TabLockedMessage>(OnTabLocked);
        ServiceLocator.Get<EventService>().Subscribe<TabUnlockedMessage>(OnTabUnlocked);
        ServiceLocator.Get<EventService>().Subscribe<TabLockStateChangedMessage>(OnTabLockStateChanged);
    }

    private void OnTabLocked(TabLockedMessage message)
    {
        if (message.TabId == UniqueId)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsUnlocked = false;
            });
        }
    }

    private void OnTabUnlocked(TabUnlockedMessage message)
    {
        if (message.TabId == UniqueId)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsUnlocked = true;
            });
        }
    }

    private void OnTabLockStateChanged(TabLockStateChangedMessage message)
    {
        if (message.TabId == UniqueId)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsLocked = message.IsLocked;
                UpdateUnlockedState();
            });
        }
    }

    private void UpdateUnlockedState()
    {
        if (!_isLocked)
        {
            _isUnlocked = true;
        }
        else
        {
            var tabLockService = ServiceLocator.GetService<TabLockService>();
            _isUnlocked = tabLockService?.IsUnlocked(UniqueId) ?? false;
        }
        this.RaisePropertyChanged(nameof(IsUnlocked));
    }

    /// <summary>
    /// 唯一标识符 (格式: "PluginName:ClassName" 或 "microdock:ClassName")
    /// </summary>
    public string UniqueId
    {
        get => _uniqueId;
        set => this.RaiseAndSetIfChanged(ref _uniqueId, value);
    }

    /// <summary>
    /// 导航项标题
    /// </summary>
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    /// <summary>
    /// 图标名称（对应Symbol枚举）
    /// </summary>
    public string Icon
    {
        get => _icon;
        set => this.RaiseAndSetIfChanged(ref _icon, value);
    }

    /// <summary>
    /// 导航项内容（View实例）
    /// </summary>
    public object? Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    /// <summary>
    /// 导航类型
    /// </summary>
    public NavigationType NavType
    {
        get => _navType;
        set => this.RaiseAndSetIfChanged(ref _navType, value);
    }
    /// <summary>
    /// 插件唯一名称（仅对插件导航项有效）
    /// </summary>
    public string PluginUniqueName
    {
        get => _pluginUniqueName;
        set => this.RaiseAndSetIfChanged(ref _pluginUniqueName, value);
    }
    /// <summary>
    /// 是否可见
    /// 如果没有启用则都不可见
    /// </summary>
    public bool IsVisible
    {
        get => _isEnabled ? _isVisible : false;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }
    /// <summary>
    /// 次序,越小越靠前
    /// </summary>
    public int Order
    {
        get => _order;
        set => this.RaiseAndSetIfChanged(ref _order, value);
    }

    /// <summary>
    /// 是否设置了密码锁定
    /// </summary>
    public bool IsLocked
    {
        get => _isLocked;
        set => this.RaiseAndSetIfChanged(ref _isLocked, value);
    }

    /// <summary>
    /// 当前是否已解锁（可访问内容）
    /// 未设置锁定的页签始终返回 true
    /// </summary>
    public bool IsUnlocked
    {
        get => _isUnlocked;
        set => this.RaiseAndSetIfChanged(ref _isUnlocked, value);
    }

    /// <summary>
    /// 是否需要显示锁屏（已锁定且未解锁）
    /// </summary>
    public bool NeedsUnlock => IsLocked && !IsUnlocked;

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}

/// <summary>
/// 导航类型枚举
/// </summary>
public enum NavigationType
{
    /// <summary>
    /// 应用管理
    /// </summary>
    Application,

    /// <summary>
    /// 插件
    /// </summary>
    Plugin,

    /// <summary>
    /// 系统功能（如日志查看器等）
    /// </summary>
    System,

    /// <summary>
    /// 设置
    /// </summary>
    Settings
}

