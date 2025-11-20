using DynamicData.Binding;
using MicroDock.Model;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace MicroDock.ViewModel;

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
        _tabDto.WhenValueChanged(a => a.IsVisible)
            .Subscribe(_ => IsVisible = _tabDto.IsVisible)
            .DisposeWith(_cleanUp);

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

