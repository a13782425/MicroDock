using System;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using MicroDock.Services.Platform;

namespace MicroDock.Services;

/// <summary>
/// 靠边隐藏服务 - QQ 风格实现
/// </summary>
public class AutoHideService : IWindowService, IDisposable
{
    private readonly Window _window;
    private readonly IPlatformCursorService? _cursorService;
    private bool _isEnabled;
    private Timer? _hideTimer;
    private Timer? _showCheckTimer;
    private Timer? _animationTimer;

    // 配置参数（QQ 风格）
    private const int EDGE_THRESHOLD = 5;        // 触发靠边的阈值
    private const int HIDE_OFFSET = 2;           // 隐藏后可见像素（更接近QQ）
    private const int SHOW_TRIGGER_ZONE = 25;    // 显示触发热区
    private const int TRIGGER_EXTEND_MARGIN = 5; // 热区延伸到屏幕外
    private const int HIDE_DELAY = 1000;         // 隐藏延迟（毫秒）
    private const int ANIMATION_DURATION = 250;  // 动画时长（毫秒）
    private const int ANIMATION_FPS = 60;        // 动画帧率

    // 状态管理
    private AutoHideState _state = AutoHideState.Visible;
    private EdgePosition _hiddenEdge = EdgePosition.None;
    private PixelPoint _positionBeforeHide;      // 隐藏前的精确位置
    private Screen? _hiddenScreen = null;        // 窗口隐藏时所在的屏幕

    // 动画相关
    private DateTime _animationStartTime;
    private PixelPoint _animationStartPos;
    private PixelPoint _animationTargetPos;
    private Action? _animationOnComplete;        // 动画完成回调

    private bool _disposed = false;

    public enum AutoHideState
    {
        Visible,      // 完全可见
        Hiding,       // 正在隐藏（动画中）
        Hidden,       // 已隐藏
        Showing       // 正在显示（动画中）
    }

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
        _cursorService = PlatformServiceFactory.CreateCursorService();

        _hideTimer = new Timer(HIDE_DELAY);
        _hideTimer.Elapsed += OnHideTimerElapsed;
        _hideTimer.AutoReset = false;

        _showCheckTimer = new Timer(100);
        _showCheckTimer.Elapsed += OnShowCheckTimerElapsed;
        _showCheckTimer.AutoReset = true;

        _animationTimer = new Timer(1000.0 / ANIMATION_FPS);
        _animationTimer.Elapsed += OnAnimationTimerElapsed;
        _animationTimer.AutoReset = true;
    }

    /// <summary>
    /// 启用靠边隐藏
    /// </summary>
    public void Enable()
    {
        if (!_isEnabled)
        {
            _window.PositionChanged += OnWindowPositionChanged;
            _window.PointerExited += OnWindowPointerExited;
            _window.Deactivated += OnWindowDeactivated;
            _showCheckTimer?.Start();
            _isEnabled = true;
            System.Diagnostics.Debug.WriteLine("[AutoHide] 服务已启用");

            // 立即检查当前位置是否在边缘
            CheckAndStartHideTimer();
        }
    }

    /// <summary>
    /// 禁用靠边隐藏
    /// </summary>
    public void Disable()
    {
        _window.PositionChanged -= OnWindowPositionChanged;
        _window.PointerExited -= OnWindowPointerExited;
        _window.Deactivated -= OnWindowDeactivated;
        _hideTimer?.Stop();
        _showCheckTimer?.Stop();
        _animationTimer?.Stop();

        // 如果窗口处于隐藏状态，立即恢复显示（无动画）
        if (_state == AutoHideState.Hidden || _state == AutoHideState.Hiding)
        {
            RestoreWindowImmediate();
        }

        _isEnabled = false;
        System.Diagnostics.Debug.WriteLine("[AutoHide] 服务已禁用");
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
        if (!_isEnabled || _state != AutoHideState.Visible)
            return;

        System.Diagnostics.Debug.WriteLine($"[AutoHide] 事件: PositionChanged -> {e.Point}");
        CheckAndStartHideTimer();
    }

    /// <summary>
    /// 鼠标离开窗口事件处理
    /// </summary>
    private void OnWindowPointerExited(object? sender, PointerEventArgs e)
    {
        if (!_isEnabled || _state != AutoHideState.Visible)
            return;

        System.Diagnostics.Debug.WriteLine("[AutoHide] 事件: PointerExited（鼠标离开窗口）");
        // 鼠标离开窗口时，检查是否在边缘
        CheckAndStartHideTimer();
    }

    /// <summary>
    /// 窗口失去焦点事件处理
    /// </summary>
    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (!_isEnabled || _state != AutoHideState.Visible)
            return;

        System.Diagnostics.Debug.WriteLine("[AutoHide] 事件: Deactivated（窗口失去焦点）");
        // 窗口失去焦点时，检查是否在边缘
        CheckAndStartHideTimer();
    }

    /// <summary>
    /// 检查当前位置并启动隐藏计时器
    /// </summary>
    private void CheckAndStartHideTimer()
    {
        System.Diagnostics.Debug.WriteLine($"[AutoHide] CheckAndStartHideTimer 被调用, 当前位置: {_window.Position}");
        EdgePosition edge = GetEdgePosition(_window.Position);

        if (edge != EdgePosition.None)
        {
            // 窗口靠近边缘
            if (_hiddenEdge != edge)
            {
                // 边缘位置改变，重新启动计时器
                _hiddenEdge = edge;
                _hideTimer?.Stop();
                _hideTimer?.Start();
                System.Diagnostics.Debug.WriteLine($"[AutoHide] 检测到窗口在{edge}边缘，{HIDE_DELAY}ms 后隐藏");
            }
            else if (_hideTimer?.Enabled != true)
            {
                // 同一边缘但计时器未运行，启动计时器
                _hideTimer?.Start();
                System.Diagnostics.Debug.WriteLine($"[AutoHide] 重新启动隐藏计时器（{edge}边缘）");
            }
            else
            {
                // 计时器已经在运行，不要重启
                System.Diagnostics.Debug.WriteLine($"[AutoHide] 计时器正在运行中，不重启（{edge}边缘）");
            }
        }
        else
        {
            // 窗口离开边缘，取消隐藏
            if (_hideTimer?.Enabled == true)
            {
                _hideTimer.Stop();
                System.Diagnostics.Debug.WriteLine($"[AutoHide] 窗口离开边缘，取消隐藏计时（当前位置: {_window.Position}）");
            }
            _hiddenEdge = EdgePosition.None;
        }
    }

    /// <summary>
    /// 隐藏计时器到期事件
    /// </summary>
    private void OnHideTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[AutoHide] 计时器到期: _isEnabled={_isEnabled}, _state={_state}, _hiddenEdge={_hiddenEdge}");

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_isEnabled && _state == AutoHideState.Visible && _hiddenEdge != EdgePosition.None)
            {
                System.Diagnostics.Debug.WriteLine("[AutoHide] 条件满足，开始执行隐藏");
                HideWindow(_hiddenEdge);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AutoHide] 条件不满足，取消隐藏: _isEnabled={_isEnabled}, _state={_state}, _hiddenEdge={_hiddenEdge}");
            }
        });
    }

    /// <summary>
    /// 显示检查计时器
    /// </summary>
    private void OnShowCheckTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isEnabled || _state != AutoHideState.Hidden)
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
    /// 动画计时器
    /// </summary>
    private void OnAnimationTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            UpdateAnimation();
        });
    }

    /// <summary>
    /// 判断窗口是否靠近屏幕边缘
    /// </summary>
    private EdgePosition GetEdgePosition(PixelPoint position)
    {
        // 使用窗口本身查找屏幕，而不是用位置点（位置可能在屏幕外）
        Screen? screen = _window.Screens.ScreenFromWindow(_window);
        if (screen == null)
        {
            System.Diagnostics.Debug.WriteLine("[AutoHide] 无法获取屏幕信息");
            return EdgePosition.None;
        }

        PixelRect workingArea = screen.WorkingArea;
        int windowWidth = (int)_window.Width;
        int windowHeight = (int)_window.Height;

        // 如果计时器正在运行，使用更宽松的阈值（防止微小位移导致取消）
        int threshold = (_hideTimer?.Enabled == true) ? EDGE_THRESHOLD * 3 : EDGE_THRESHOLD;

        int distanceLeft = position.X - workingArea.X;
        int distanceRight = workingArea.Right - (position.X + windowWidth);
        int distanceTop = position.Y - workingArea.Y;

        // 检查是否靠近左边缘（允许负值，即窗口部分移出屏幕）
        if (distanceLeft <= threshold)
        {
            System.Diagnostics.Debug.WriteLine($"[AutoHide] 判定在Left边缘: X={position.X}, WorkingArea.X={workingArea.X}, Distance={distanceLeft}, Threshold={threshold}");
            return EdgePosition.Left;
        }

        // 检查是否靠近右边缘（允许负值）
        if (distanceRight <= threshold)
        {
            System.Diagnostics.Debug.WriteLine($"[AutoHide] 判定在Right边缘: Distance={distanceRight}, Threshold={threshold}");
            return EdgePosition.Right;
        }

        // 检查是否靠近上边缘（允许负值）
        if (distanceTop <= threshold)
        {
            System.Diagnostics.Debug.WriteLine($"[AutoHide] 判定在Top边缘: Distance={distanceTop}, Threshold={threshold}");
            return EdgePosition.Top;
        }

        System.Diagnostics.Debug.WriteLine($"[AutoHide] 不在边缘: Position={position}, DistanceLeft={distanceLeft}, DistanceRight={distanceRight}, DistanceTop={distanceTop}");
        return EdgePosition.None;
    }

    /// <summary>
    /// 隐藏窗口到边缘（带动画）
    /// </summary>
    private void HideWindow(EdgePosition edge)
    {
        Screen? screen = _window.Screens.ScreenFromWindow(_window);
        if (screen == null)
            return;

        // 保存窗口隐藏时的状态
        _hiddenScreen = screen;
        _positionBeforeHide = _window.Position;

        PixelRect workingArea = screen.WorkingArea;
        PixelPoint targetPosition = _window.Position;
        int windowWidth = (int)(_window.Width * _window.DesktopScaling);
        int windowHeight = (int)(_window.Height * _window.DesktopScaling);

        switch (edge)
        {
            case EdgePosition.Left:
                targetPosition = new PixelPoint(workingArea.X - windowWidth + HIDE_OFFSET, _window.Position.Y);
                break;
            case EdgePosition.Right:
                targetPosition = new PixelPoint(workingArea.Right - HIDE_OFFSET, _window.Position.Y);
                break;
            case EdgePosition.Top:
                targetPosition = new PixelPoint(_window.Position.X, workingArea.Y - windowHeight + HIDE_OFFSET);
                break;
        }

        System.Diagnostics.Debug.WriteLine($"[AutoHide] 开始隐藏: Edge={edge}, Position={_window.Position}, Target={targetPosition}");

        _state = AutoHideState.Hiding;
        StartAnimation(_window.Position, targetPosition, () =>
        {
            _state = AutoHideState.Hidden;
            System.Diagnostics.Debug.WriteLine($"[AutoHide] 隐藏完成: FinalPosition={_window.Position}");
        });
    }

    /// <summary>
    /// 恢复窗口显示（带动画）
    /// </summary>
    private void RestoreWindow()
    {
        if (_state != AutoHideState.Hidden)
            return;

        // 使用保存的屏幕信息
        Screen? screen = _hiddenScreen ?? _window.Screens.ScreenFromWindow(_window);
        if (screen == null)
            return;

        PixelRect workingArea = screen.WorkingArea;
        PixelPoint targetPosition = _positionBeforeHide;
        int windowWidth = (int)_window.Width;
        int windowHeight = (int)_window.Height;

        // 验证保存的位置是否仍然有效
        bool positionValid = false;// IsPositionValid(_positionBeforeHide, workingArea, windowWidth, windowHeight);

        if (!positionValid)
        {
            // 位置已失效，根据边缘重新计算安全位置
            System.Diagnostics.Debug.WriteLine("[AutoHide] 检测到保存位置失效，重新计算");
            switch (_hiddenEdge)
            {
                case EdgePosition.Left:
                    targetPosition = new PixelPoint(workingArea.X, _window.Position.Y);
                    break;
                case EdgePosition.Right:
                    targetPosition = new PixelPoint(workingArea.Right - windowWidth, _window.Position.Y);
                    break;
                case EdgePosition.Top:
                    targetPosition = new PixelPoint(_window.Position.X, workingArea.Y);
                    break;
            }
        }

        System.Diagnostics.Debug.WriteLine($"[AutoHide] 开始显示: Position={_window.Position}, Target={targetPosition}");

        _state = AutoHideState.Showing;
        StartAnimation(_window.Position, targetPosition, () =>
        {
            _state = AutoHideState.Visible;
            _hiddenEdge = EdgePosition.None;
            _hiddenScreen = null;
            System.Diagnostics.Debug.WriteLine($"[AutoHide] 显示完成: FinalPosition={_window.Position}");
        });
    }

    /// <summary>
    /// 立即恢复窗口（无动画）
    /// </summary>
    private void RestoreWindowImmediate()
    {
        if (_state == AutoHideState.Visible)
            return;

        _animationTimer?.Stop();

        Screen? screen = _hiddenScreen ?? _window.Screens.ScreenFromWindow(_window);
        if (screen == null)
            return;

        PixelRect workingArea = screen.WorkingArea;
        int windowWidth = (int)_window.Width;
        int windowHeight = (int)_window.Height;

        bool positionValid = IsPositionValid(_positionBeforeHide, workingArea, windowWidth, windowHeight);

        if (positionValid)
        {
            _window.Position = _positionBeforeHide;
        }
        else
        {
            // 计算安全位置
            PixelPoint safePosition = _positionBeforeHide;
            switch (_hiddenEdge)
            {
                case EdgePosition.Left:
                    safePosition = new PixelPoint(workingArea.X, _window.Position.Y);
                    break;
                case EdgePosition.Right:
                    safePosition = new PixelPoint(workingArea.Right - windowWidth, _window.Position.Y);
                    break;
                case EdgePosition.Top:
                    safePosition = new PixelPoint(_window.Position.X, workingArea.Y);
                    break;
            }
            _window.Position = safePosition;
        }

        _state = AutoHideState.Visible;
        _hiddenEdge = EdgePosition.None;
        _hiddenScreen = null;

        System.Diagnostics.Debug.WriteLine($"[AutoHide] 立即恢复显示: Position={_window.Position}");
    }

    /// <summary>
    /// 验证位置是否有效
    /// </summary>
    private bool IsPositionValid(PixelPoint position, PixelRect workingArea, int windowWidth, int windowHeight)
    {
        return position.X >= workingArea.X - windowWidth / 2 &&
               position.X <= workingArea.Right - windowWidth / 2 &&
               position.Y >= workingArea.Y - windowHeight / 2 &&
               position.Y <= workingArea.Bottom - windowHeight / 2;
    }

    /// <summary>
    /// 检查是否应该显示窗口（鼠标靠近边缘）
    /// </summary>
    private bool ShouldShowWindow()
    {
        if (_state != AutoHideState.Hidden || _hiddenScreen == null)
            return false;

        // 使用平台光标服务获取鼠标位置
        if (_cursorService == null || !_cursorService.IsSupported)
            return false;

        // 获取全局鼠标位置
        if (!_cursorService.TryGetCursorPosition(out Point mousePosition))
            return false;

        // 转换为整数坐标
        int mousePosX = (int)mousePosition.X;
        int mousePosY = (int)mousePosition.Y;

        // 使用保存的屏幕信息
        PixelRect workingArea = _hiddenScreen.WorkingArea;
        PixelPoint windowPos = _window.Position;
        int windowWidth = (int)_window.Width;
        int windowHeight = (int)_window.Height;

        // 根据隐藏的边缘判断鼠标是否在触发热区内
        bool inTriggerZone = false;

        switch (_hiddenEdge)
        {
            case EdgePosition.Left:
                // 左边缘：检查鼠标是否在触发热区内
                inTriggerZone = mousePosX >= workingArea.X - TRIGGER_EXTEND_MARGIN &&
                               mousePosX <= workingArea.X + SHOW_TRIGGER_ZONE &&
                               mousePosY >= windowPos.Y &&
                               mousePosY <= windowPos.Y + windowHeight;
                break;

            case EdgePosition.Right:
                // 右边缘：检查鼠标是否在触发热区内
                inTriggerZone = mousePosX >= workingArea.Right - SHOW_TRIGGER_ZONE &&
                               mousePosX <= workingArea.Right + TRIGGER_EXTEND_MARGIN &&
                               mousePosY >= windowPos.Y &&
                               mousePosY <= windowPos.Y + windowHeight;
                break;

            case EdgePosition.Top:
                // 上边缘：检查鼠标是否在触发热区内
                inTriggerZone = mousePosY >= workingArea.Y - TRIGGER_EXTEND_MARGIN &&
                               mousePosY <= workingArea.Y + SHOW_TRIGGER_ZONE &&
                               mousePosX >= windowPos.X &&
                               mousePosX <= windowPos.X + windowWidth;
                break;

            default:
                return false;
        }

        if (inTriggerZone)
        {
            System.Diagnostics.Debug.WriteLine($"[AutoHide] 鼠标触发显示: MousePos=({mousePosX},{mousePosY}), TriggerZone={SHOW_TRIGGER_ZONE}px");
        }

        return inTriggerZone;
    }

    /// <summary>
    /// 启动位置动画
    /// </summary>
    private void StartAnimation(PixelPoint from, PixelPoint to, Action? onComplete = null)
    {
        _animationStartTime = DateTime.Now;
        _animationStartPos = from;
        _animationTargetPos = to;
        _animationOnComplete = onComplete;

        _animationTimer?.Stop();
        _animationTimer?.Start();
    }

    /// <summary>
    /// 更新动画帧
    /// </summary>
    private void UpdateAnimation()
    {
        if (_state != AutoHideState.Hiding && _state != AutoHideState.Showing)
        {
            _animationTimer?.Stop();
            return;
        }

        double elapsed = (DateTime.Now - _animationStartTime).TotalMilliseconds;
        double progress = Math.Min(1.0, elapsed / ANIMATION_DURATION);
        double eased = EaseInOutCubic(progress);

        int x = (int)(_animationStartPos.X + (_animationTargetPos.X - _animationStartPos.X) * eased);
        int y = (int)(_animationStartPos.Y + (_animationTargetPos.Y - _animationStartPos.Y) * eased);

        _window.Position = new PixelPoint(x, y);

        if (progress >= 1.0)
        {
            _animationTimer?.Stop();
            System.Diagnostics.Debug.WriteLine($"[AutoHide] 动画完成100%: Position=({x},{y})");

            // 执行完成回调
            Action? callback = _animationOnComplete;
            _animationOnComplete = null;
            if (callback != null)
            {
                callback.Invoke();
            }
        }
        else if (progress > 0 && progress % 0.25 < 0.05) // 每25%进度输出一次日志
        {
            string action = _state == AutoHideState.Hiding ? "隐藏" : "显示";
            System.Diagnostics.Debug.WriteLine($"[AutoHide] {action}动画: 进度={progress:P0}, Position=({x},{y})");
        }
    }

    /// <summary>
    /// 缓动函数：Ease-In-Out 三次方曲线
    /// </summary>
    private static double EaseInOutCubic(double t)
    {
        return t < 0.5
            ? 4 * t * t * t
            : 1 - Math.Pow(-2 * t + 2, 3) / 2;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        System.Diagnostics.Debug.WriteLine("[AutoHide] 释放资源");

        Disable();

        if (_hideTimer != null)
        {
            _hideTimer.Elapsed -= OnHideTimerElapsed;
            _hideTimer.Dispose();
            _hideTimer = null;
        }

        if (_showCheckTimer != null)
        {
            _showCheckTimer.Elapsed -= OnShowCheckTimerElapsed;
            _showCheckTimer.Dispose();
            _showCheckTimer = null;
        }

        if (_animationTimer != null)
        {
            _animationTimer.Elapsed -= OnAnimationTimerElapsed;
            _animationTimer.Dispose();
            _animationTimer = null;
        }

        _disposed = true;
    }
}
