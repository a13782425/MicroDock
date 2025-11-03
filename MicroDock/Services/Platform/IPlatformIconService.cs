namespace MicroDock.Services.Platform;

/// <summary>
/// 平台图标提取服务接口
/// 不同操作系统从文件中提取图标的方式不同
/// </summary>
public interface IPlatformIconService
{
    /// <summary>
    /// 当前平台是否支持此功能
    /// </summary>
    bool IsSupported { get; }
    
    /// <summary>
    /// 从文件（可执行文件、快捷方式等）提取图标
    /// </summary>
    /// <param name="filePath">文件完整路径</param>
    /// <param name="preferredSize">期望的图标尺寸（像素）</param>
    /// <returns>图标的PNG字节数组，失败返回null</returns>
    byte[]? ExtractIconBytes(string filePath, int preferredSize = 48);
}

