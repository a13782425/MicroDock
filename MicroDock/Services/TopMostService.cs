using System;
using Avalonia.Controls;

namespace MicroDock.Services;

/// <summary>
/// 窗口置顶服务
/// </summary>
public class TopMostService : IWindowService, IDisposable
{
    private readonly Window _window;
    private bool _isEnabled;
    private bool _disposed = false;

    public TopMostService(Window window)
    {
        _window = window;
    }

    /// <summary>
    /// 启用窗口置顶
    /// </summary>
    public void Enable()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TopMostService));

        _window.Topmost = true;
        _isEnabled = true;
    }

    /// <summary>
    /// 禁用窗口置顶
    /// </summary>
    public void Disable()
    {
        if (_disposed)
            return;

        _window.Topmost = false;
        _isEnabled = false;
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

