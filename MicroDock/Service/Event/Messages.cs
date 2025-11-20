namespace MicroDock.Service;

public interface IEventMessage { }


/// <summary>
/// 窗口显示请求消息
/// </summary>
public class WindowShowRequestMessage : IEventMessage
{
    public string WindowName { get; }

    public WindowShowRequestMessage(string windowName)
    {
        WindowName = windowName;
    }
}

/// <summary>
/// 窗口隐藏请求消息
/// </summary>
public class WindowHideRequestMessage : IEventMessage
{
    public string WindowName { get; }

    public WindowHideRequestMessage(string windowName)
    {
        WindowName = windowName;
    }
}

/// <summary>
/// 窗口置顶状态变更请求消息
/// </summary>
public class WindowTopmostChangeRequestMessage : IEventMessage
{
    public bool Enable { get; }

    public WindowTopmostChangeRequestMessage(bool enable)
    {
        Enable = enable;
    }
}

/// <summary>
/// 窗口自动隐藏状态变更请求消息
/// </summary>
public class AutoHideChangeRequestMessage : IEventMessage
{
    public bool Enable { get; }

    public AutoHideChangeRequestMessage(bool enable)
    {
        Enable = enable;
    }
}

/// <summary>
/// 开机自启动状态变更请求消息
/// </summary>
public class AutoStartupChangeRequestMessage : IEventMessage
{
    public bool Enable { get; }

    public AutoStartupChangeRequestMessage(bool enable)
    {
        Enable = enable;
    }
}

/// <summary>
/// 服务状态变更通知消息
/// </summary>
public class ServiceStateChangedMessage : IEventMessage
{
    public string ServiceName { get; }
    public bool IsEnabled { get; }

    public ServiceStateChangedMessage(string serviceName, bool isEnabled)
    {
        ServiceName = serviceName;
        IsEnabled = isEnabled;
    }
}

/// <summary>
/// 导航到标签页消息
/// </summary>
public class NavigateToTabMessage : IEventMessage
{
    public string? TabName { get; }
    public int? TabIndex { get; }
    public string? UniqueId { get; }

    public NavigateToTabMessage(string? tabName = null, int? tabIndex = null, string? uniqueId = null)
    {
        TabName = tabName;
        TabIndex = tabIndex;
        UniqueId = uniqueId;
    }
}
/// <summary>
/// 日志查看器可见性变更消息
/// </summary>
public class NavigationTabVisibilityChangedMessage : IEventMessage
{
    public bool IsVisible { get; }
    public string TabUniqueId { get; }
    public NavigationTabVisibilityChangedMessage(string tabUniqueId, bool isVisible)
    {
        IsVisible = isVisible;
        TabUniqueId = tabUniqueId;
    }
}

/// <summary>
/// 显示Loading消息
/// </summary>
public class ShowLoadingMessage : IEventMessage
{
    public string? Message { get; }

    public ShowLoadingMessage(string? message = null)
    {
        Message = message;
    }
}

/// <summary>
/// 隐藏Loading消息
/// </summary>
public class HideLoadingMessage : IEventMessage
{
}

/// <summary>
/// 插件状态变更消息
/// </summary>
public class PluginStateChangedMessage : IEventMessage
{
    public string PluginName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 插件删除消息
/// </summary>
public class PluginDeletedMessage : IEventMessage
{
    public string PluginName { get; set; } = string.Empty;
}

/// <summary>
/// 插件导入消息
/// </summary>
public class PluginImportedMessage : IEventMessage
{
    public string PluginName { get; set; } = string.Empty;
}

/// <summary>
/// 导航页签配置变更消息 (用于刷新排序/显隐)
/// </summary>
public class NavigationTabsConfigurationChangedMessage : IEventMessage
{
}

