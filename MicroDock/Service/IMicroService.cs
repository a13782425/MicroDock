using System;
using System.Threading.Tasks;

namespace MicroDock.Service;


/// <summary>
/// 只有标记了AutoRegisterAttribute的服务类才会被自动注册
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AutoRegisterAttribute : Attribute
{
    /// <summary>
    /// 越小优先级越高
    /// </summary>
    public int Priority { get; init; } = 0; // 较高优先级
    public AutoRegisterAttribute() : this(0) { }

    public AutoRegisterAttribute(int priority)
    {
        this.Priority = priority;
    }
}

/// <summary>
/// 微服务生命周期接口
/// </summary>
public interface IMicroService
{

    /// <summary>
    /// 服务注册完成后（可安全访问其他服务）
    /// </summary>
    Task OnRegistered() => Task.CompletedTask;
    /// <summary>
    /// 在构建AppBuilder之后
    /// </summary>
    Task OnAfterAppBuilder() => Task.CompletedTask;
    /// <summary>
    /// 启动页开始之前
    /// </summary>
    Task OnBeforeSplashScreen() => Task.CompletedTask;
    /// <summary>
    /// 启动页关闭之后
    /// </summary>
    Task OnAfterSplashScreen() => Task.CompletedTask;
    /// <summary>
    /// 应用启动完成（主窗口已显示）
    /// </summary>
    Task OnApplicationStarted() => Task.CompletedTask;
    /// <summary>
    /// 应用即将关闭,即释放
    /// </summary>
    Task OnApplicationStopping() => Task.CompletedTask;
}
