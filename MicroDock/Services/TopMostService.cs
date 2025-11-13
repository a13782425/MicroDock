using System;
using Avalonia.Controls;

namespace MicroDock.Services;

/// <summary>
/// 窗口置顶服务
/// </summary>
public class TopMostService : IWindowService, IDisposable
{
    private Window? _window;
    private bool _isEnabled;
    private bool _isInitialized;
    private bool _disposed = false;

    /// <summary>
    /// 无参构造函数，用于 ServiceLocator 注册
    /// </summary>
    public TopMostService()
    {
    }

    /// <summary>
    /// 初始化服务（在窗口创建后调用）
    /// </summary>
    public void Initialize(Window window)
    {
        if (_isInitialized)
        {
            Serilog.Log.Warning("TopMostService 已经初始化过");
            return;
        }

        _window = window;
        _isInitialized = true;
        Serilog.Log.Debug("TopMostService 已初始化");
    }

    /// <summary>
    /// 启用窗口置顶
    /// </summary>
    public void Enable()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TopMostService));

        if (!CheckWindow()) return;

        _window!.Topmost = true;
        _isEnabled = true;
    }

    /// <summary>
    /// 禁用窗口置顶
    /// </summary>
    public void Disable()
    {
        if (_disposed)
            return;

        if (!CheckWindow()) return;

        _window!.Topmost = false;
        _isEnabled = false;
    }

    /// <summary>
    /// 检查窗口是否已初始化
    /// </summary>
    private bool CheckWindow()
    {
        if (_window == null || !_isInitialized)
        {
            Serilog.Log.Warning("TopMostService: 服务未初始化或窗口为空");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 获取服务是否已启用
    /// </summary>
    public bool IsEnabled => _isEnabled;

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // 禁用时恢复窗口状态
        if (_isEnabled)
        {
            Disable();
        }

        _disposed = true;
    }
}

