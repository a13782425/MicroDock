using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Timers;

namespace MicroDock.Services;

/// <summary>
/// 靠边隐藏服务
/// </summary>
public class AutoHideService : IWindowService
{
    private readonly Window _window;
    private bool _isEnabled;
    private Timer? _hideTimer;
    private Timer? _showCheckTimer;
    private const int EDGE_THRESHOLD = 5; // 触发靠边隐藏的像素阈值
    private const int HIDE_OFFSET = 2; // 隐藏时保留的像素
    private const int HIDE_DELAY = 1000; // 隐藏延迟（毫秒）
    private bool _isHidden = false;
    private EdgePosition _hiddenEdge = EdgePosition.None;
    private PixelPoint _lastPosition;

    public enum EdgePosition
    {
        None,
        Left,
        Right,
        Top
    }

    public AutoHideService(Window window)
    {
        _window = window;
        _hideTimer = new Timer(HIDE_DELAY);
        _hideTimer.Elapsed += OnHideTimerElapsed;
        _hideTimer.AutoReset = false;
        
        _showCheckTimer = new Timer(100);
        _showCheckTimer.Elapsed += OnShowCheckTimerElapsed;
        _showCheckTimer.AutoReset = true;
    }

    /// <summary>
    /// 启用靠边隐藏
    /// </summary>
    public void Enable()
    {
        if (!_isEnabled)
        {
            _window.PositionChanged += OnWindowPositionChanged;
            _showCheckTimer?.Start();
            _isEnabled = true;
        }
    }

    /// <summary>
    /// 禁用靠边隐藏
    /// </summary>
    public void Disable()
    {
        _window.PositionChanged -= OnWindowPositionChanged;
        _hideTimer?.Stop();
        _showCheckTimer?.Stop();
        
        // 如果窗口处于隐藏状态，恢复显示
        if (_isHidden)
        {
            RestoreWindow();
        }
        
        _isEnabled = false;
    }

    /// <summary>
    /// 获取服务是否已启用
    /// </summary>
    public bool IsEnabled => _isEnabled;

    /// <summary>
    /// 窗口位置变化事件处理
    /// </summary>
    private void OnWindowPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (!_isEnabled || _isHidden)
            return;

        _lastPosition = e.Point;
        EdgePosition edge = GetEdgePosition(e.Point);

        if (edge != EdgePosition.None)
        {
            // 窗口靠近边缘，启动隐藏计时器
            _hiddenEdge = edge;
            _hideTimer?.Stop();
            _hideTimer?.Start();
        }
        else
        {
            // 窗口离开边缘，取消隐藏
            _hideTimer?.Stop();
        }
    }

    /// <summary>
    /// 隐藏计时器到期事件
    /// </summary>
    private void OnHideTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_isEnabled && !_isHidden && _hiddenEdge != EdgePosition.None)
            {
                HideWindow(_hiddenEdge);
            }
        });
    }

    /// <summary>
    /// 显示检查计时器
    /// </summary>
    private void OnShowCheckTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isEnabled || !_isHidden)
            return;

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (ShouldShowWindow())
            {
                RestoreWindow();
            }
        });
    }

    /// <summary>
    /// 判断窗口是否靠近屏幕边缘
    /// </summary>
    private EdgePosition GetEdgePosition(PixelPoint position)
    {
        Screen? screen = _window.Screens.ScreenFromPoint(position);
        if (screen == null)
            return EdgePosition.None;

        PixelRect workingArea = screen.WorkingArea;
        
        // 检查是否靠近左边缘
        if (position.X <= workingArea.X + EDGE_THRESHOLD)
            return EdgePosition.Left;
        
        // 检查是否靠近右边缘
        if (position.X + _window.Width >= workingArea.Right - EDGE_THRESHOLD)
            return EdgePosition.Right;
        
        // 检查是否靠近上边缘
        if (position.Y <= workingArea.Y + EDGE_THRESHOLD)
            return EdgePosition.Top;

        return EdgePosition.None;
    }

    /// <summary>
    /// 隐藏窗口到边缘
    /// </summary>
    private void HideWindow(EdgePosition edge)
    {
        Screen? screen = _window.Screens.ScreenFromWindow(_window);
        if (screen == null)
            return;

        PixelRect workingArea = screen.WorkingArea;
        PixelPoint newPosition = _window.Position;

        switch (edge)
        {
            case EdgePosition.Left:
                newPosition = new PixelPoint(workingArea.X - (int)_window.Width + HIDE_OFFSET, _window.Position.Y);
                break;
            case EdgePosition.Right:
                newPosition = new PixelPoint(workingArea.Right - HIDE_OFFSET, _window.Position.Y);
                break;
            case EdgePosition.Top:
                newPosition = new PixelPoint(_window.Position.X, workingArea.Y - (int)_window.Height + HIDE_OFFSET);
                break;
        }

        _window.Position = newPosition;
        _isHidden = true;
    }

    /// <summary>
    /// 恢复窗口显示
    /// </summary>
    private void RestoreWindow()
    {
        if (!_isHidden)
            return;

        Screen? screen = _window.Screens.ScreenFromWindow(_window);
        if (screen == null)
            return;

        PixelRect workingArea = screen.WorkingArea;
        PixelPoint newPosition = _window.Position;

        switch (_hiddenEdge)
        {
            case EdgePosition.Left:
                newPosition = new PixelPoint(workingArea.X, _window.Position.Y);
                break;
            case EdgePosition.Right:
                newPosition = new PixelPoint(workingArea.Right - (int)_window.Width, _window.Position.Y);
                break;
            case EdgePosition.Top:
                newPosition = new PixelPoint(_window.Position.X, workingArea.Y);
                break;
        }

        _window.Position = newPosition;
        _isHidden = false;
        _hiddenEdge = EdgePosition.None;
    }

    /// <summary>
    /// 检查是否应该显示窗口（鼠标靠近边缘）
    /// </summary>
    private bool ShouldShowWindow()
    {
        if (!_isHidden)
            return false;

        // 获取鼠标位置（这里简化处理，实际可能需要使用平台特定的API）
        // 由于Avalonia没有直接提供全局鼠标位置，这里使用窗口激活作为触发
        // 当鼠标移动到隐藏的窗口区域时，窗口会收到鼠标事件
        
        return false; // 暂时使用简单逻辑，可以后续优化
    }
}

