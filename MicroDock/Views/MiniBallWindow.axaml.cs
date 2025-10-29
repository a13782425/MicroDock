using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MicroDock.Database;
using MicroDock.Services;
using System;
using Avalonia;

namespace MicroDock.Views;

/// <summary>
/// 悬浮球窗口，只负责显示悬浮球和拖动功能
/// 长按时打开独立的功能栏窗口
/// </summary>
public partial class MiniBallWindow : Window
{
    private readonly DispatcherTimer _longPressTimer;
    private PointerPressedEventArgs? _dragStartArgs;
    private LauncherWindow? _launcherWindow;
    private Point _pointerPressedPosition;
    private const double DragThreshold = 5.0; // 拖动阈值（像素）
    
    public MiniBallWindow()
    {
        InitializeComponent();
        _longPressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _longPressTimer.Tick += OnLongPress;
        
        // 窗口关闭时，同时关闭功能栏窗口
        this.Closed += (_, _) =>
        {
            _launcherWindow?.Close();
            _launcherWindow = null;
        };
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 使用用户配置的长按阈值
        SettingDB settings = DBContext.GetSetting();
        int ms = settings.LongPressMs;
        if (ms < 100) ms = 100;
        if (ms > 5000) ms = 5000;
        _longPressTimer.Interval = TimeSpan.FromMilliseconds(ms);

        _dragStartArgs = e;
        _pointerPressedPosition = e.GetPosition(this); // 记录按下位置
        _longPressTimer.Start();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _longPressTimer.Stop();
        _dragStartArgs = null;
        e.Handled = true;
    }
    private void OnLongPress(object? sender, EventArgs e)
    {
        _longPressTimer.Stop();

        // 如果功能栏窗口已经打开，不重复打开
        if (_launcherWindow != null && _launcherWindow.IsVisible)
            return;

        // 计算悬浮球的中心点（屏幕像素坐标）
        double scale = RenderScaling;
        if (scale <= 0) scale = 1;
        
        PixelPoint ballCenterPx = Services.WindowPositionCalculator.CalculateCenter(
            Position, Width, Height, scale);
        
        System.Diagnostics.Debug.WriteLine($"[MiniBall] 长按触发，打开功能栏: BallPosition={Position}, BallCenter={ballCenterPx}, DPI={scale:F2}");

        // 创建并显示功能栏窗口
        _launcherWindow = new LauncherWindow();
        _launcherWindow.Closed += (s, args) => _launcherWindow = null;
        _launcherWindow.ShowAroundBall(ballCenterPx, Position);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // 只有移动超过阈值才开始拖动
        if (_longPressTimer.IsEnabled && _dragStartArgs != null)
        {
            Point currentPosition = e.GetPosition(this);
            double distance = Math.Sqrt(
                Math.Pow(currentPosition.X - _pointerPressedPosition.X, 2) +
                Math.Pow(currentPosition.Y - _pointerPressedPosition.Y, 2)
            );

            // 移动距离超过阈值才认为是拖动
            if (distance > DragThreshold)
            {
                _longPressTimer.Stop();
                System.Diagnostics.Debug.WriteLine($"[MiniBall] 移动距离 {distance:F2} 像素，开始拖动");
                BeginMoveDrag(_dragStartArgs);
            }
        }
    }
}
