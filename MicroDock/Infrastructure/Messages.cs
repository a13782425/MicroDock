namespace MicroDock.Infrastructure;

/// <summary>
/// 窗口显示请求消息
/// </summary>
public class WindowShowRequestMessage
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
public class WindowHideRequestMessage
{
    public string WindowName { get; }
    
    public WindowHideRequestMessage(string windowName)
    {
        WindowName = windowName;
    }
}

/// <summary>
/// 迷你模式变更请求消息
/// </summary>
public class MiniModeChangeRequestMessage
{
    public bool Enable { get; }
    
    public MiniModeChangeRequestMessage(bool enable)
    {
        Enable = enable;
    }
}

/// <summary>
/// 窗口置顶状态变更请求消息
/// </summary>
public class WindowTopmostChangeRequestMessage
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
public class AutoHideChangeRequestMessage
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
public class AutoStartupChangeRequestMessage
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
public class ServiceStateChangedMessage
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
public class NavigateToTabMessage
{
    public string TabName { get; }
    public int? TabIndex { get; }
    
    public NavigateToTabMessage(string tabName)
    {
        TabName = tabName;
        TabIndex = null;
    }
    
    public NavigateToTabMessage(int tabIndex)
    {
        TabName = string.Empty;
        TabIndex = tabIndex;
    }
}

/// <summary>
/// 添加自定义标签页请求消息
/// </summary>
public class AddCustomTabRequestMessage
{
}

