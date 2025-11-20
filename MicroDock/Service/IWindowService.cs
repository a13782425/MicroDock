namespace MicroDock.Service;

/// <summary>
/// 窗口服务接口，定义统一的启用/禁用行为
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// 启用服务
    /// </summary>
    void Enable();
    
    /// <summary>
    /// 禁用服务
    /// </summary>
    void Disable();
    
    /// <summary>
    /// 获取服务是否已启用
    /// </summary>
    bool IsEnabled { get; }
}

