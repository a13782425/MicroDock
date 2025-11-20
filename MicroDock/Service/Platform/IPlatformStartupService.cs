namespace MicroDock.Service.Platform;

/// <summary>
/// 平台开机自启动服务接口
/// 不同操作系统需要提供各自的实现
/// </summary>
public interface IPlatformStartupService
{
    /// <summary>
    /// 当前平台是否支持此功能
    /// </summary>
    bool IsSupported { get; }
    
    /// <summary>
    /// 启用开机自启动
    /// </summary>
    /// <param name="appName">应用程序名称</param>
    /// <param name="executablePath">可执行文件完整路径</param>
    void Enable(string appName, string executablePath);
    
    /// <summary>
    /// 禁用开机自启动
    /// </summary>
    /// <param name="appName">应用程序名称</param>
    void Disable(string appName);
    
    /// <summary>
    /// 检查是否已启用开机自启动
    /// </summary>
    /// <param name="appName">应用程序名称</param>
    /// <returns>如果已启用返回true，否则返回false</returns>
    bool IsEnabled(string appName);
}

