using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace MicroDock.Service;

/// <summary>
/// 窗口置顶服务
/// </summary>
[AutoRegister]
public class TopMostService : IMicroService, IWindowService
{
    private Window? _window;
    private bool _isEnabled;

    /// <summary>
    /// 无参构造函数，用于 ServiceLocator 注册
    /// </summary>
    public TopMostService()
    {
    }

    Task IMicroService.OnAfterSplashScreen()
    {
        _window = AppConfig.MicroMainWindow;
        LogDebug("TopMostService 已初始化", DEFAULT_LOG_TAG);
        return Task.CompletedTask;
    }




    /// <summary>
    /// 启用窗口置顶
    /// </summary>
    public void Enable()
    {

        if (!CheckWindow()) return;

        _window!.Topmost = true;
        _isEnabled = true;
    }

    /// <summary>
    /// 禁用窗口置顶
    /// </summary>
    public void Disable()
    {
        if (!CheckWindow()) return;

        _window!.Topmost = false;
        _isEnabled = false;
    }

    /// <summary>
    /// 检查窗口是否已初始化
    /// </summary>
    private bool CheckWindow()
    {
        if (_window == null)
        {
            LogWarning("TopMostService: 服务未初始化或窗口为空", DEFAULT_LOG_TAG);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 获取服务是否已启用
    /// </summary>
    public bool IsEnabled => _isEnabled;

    Task IMicroService.OnApplicationStopping()
    {
        // 禁用时恢复窗口状态
        if (_isEnabled)
        {
            Disable();
        }
        return Task.CompletedTask;
    }
}

