using Avalonia.Controls;

namespace MicroDock.Services;

/// <summary>
/// 窗口置顶服务
/// </summary>
public class TopMostService : IWindowService
{
    private readonly Window _window;
    private bool _isEnabled;

    public TopMostService(Window window)
    {
        _window = window;
    }

    /// <summary>
    /// 启用窗口置顶
    /// </summary>
    public void Enable()
    {
        _window.Topmost = true;
        _isEnabled = true;
    }

    /// <summary>
    /// 禁用窗口置顶
    /// </summary>
    public void Disable()
    {
        _window.Topmost = false;
        _isEnabled = false;
    }

    /// <summary>
    /// 获取服务是否已启用
    /// </summary>
    public bool IsEnabled => _isEnabled;
}

