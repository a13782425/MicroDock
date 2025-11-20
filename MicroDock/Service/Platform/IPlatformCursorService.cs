using Avalonia;

namespace MicroDock.Service.Platform;

/// <summary>
/// 平台鼠标光标服务接口
/// 用于获取全局鼠标位置等光标相关操作
/// </summary>
public interface IPlatformCursorService
{
    /// <summary>
    /// 当前平台是否支持此功能
    /// </summary>
    bool IsSupported { get; }
    
    /// <summary>
    /// 尝试获取当前鼠标光标的屏幕位置
    /// </summary>
    /// <param name="position">输出参数，鼠标位置（屏幕坐标）</param>
    /// <returns>成功返回true，失败返回false</returns>
    bool TryGetCursorPosition(out Point position);
}

