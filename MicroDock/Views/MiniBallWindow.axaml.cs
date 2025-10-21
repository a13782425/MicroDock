using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MicroDock.Database;
using MicroDock.Services;
using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using System.Runtime.InteropServices;

namespace MicroDock.Views;

public partial class MiniBallWindow : Window
{
    private readonly DispatcherTimer _longPressTimer;
    private bool _isDragging;
    private bool _isExpanded;
    private PointerPressedEventArgs? _dragStartArgs;
    private readonly IMiniModeService? _miniModeService;
    private PixelPoint? _fixedCenterPxDuringExpand;

    // P0: Full-screen transparent host flag (default enabled). TODO: move to DB setting.
    private const bool UseFullscreenHost = true;

    // Visual-based dragging (when full-screen host is enabled)
    private double _ballLeft;
    private double _ballTop;
    private Avalonia.Point _pressPointInBall;

    // Windows click-through
    private nint _hwnd;
    private bool _clickThrough;

    // Parameterless for designer
    public MiniBallWindow()
    {
        InitializeComponent();
        _longPressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _longPressTimer.Tick += OnLongPress;
        _isExpanded = false;

        // Initialize full-screen host and place ball at screen center
        this.Opened += (_, __) =>
        {
            if (UseFullscreenHost)
            {
                try
                {
                    WindowState = WindowState.Maximized;
                }
                catch { }

  
                // Center ball in the current screen working area
                var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
                var wa = screen?.WorkingArea ?? new PixelRect(0, 0, (int)Bounds.Width, (int)Bounds.Height);
                double centerX = wa.Width / (2.0 * RenderScaling);
                double centerY = wa.Height / (2.0 * RenderScaling);
                // Ball size is 64x64 DIP
                _ballLeft = centerX - 32;
                _ballTop = centerY - 32;
                Canvas.SetLeft(Ball, _ballLeft);
                Canvas.SetTop(Ball, _ballTop);

                // Ensure launcher starts hidden
                LauncherView.IsVisible = false;
                LauncherView.Opacity = 0;

                // Initialize Windows click-through (outside interactive areas)
                TryInitHwnd();
                SetWindowClickThrough(true);
            }
            else
            {
                // legacy small-window path keeps current Width/Height/Position
            }
        };

        this.KeyDown += (_, e) =>
        {
            if (e.Handled) return;
            if (e.Key == Key.Escape)
            {
                ResetToBall();
                e.Handled = true;
            }
        };
    }

    public MiniBallWindow(IMiniModeService miniModeService) : this()
    {
        _miniModeService = miniModeService;
        LauncherView.MiniModeService = miniModeService;
        // 设置球体位置获取接口
        LauncherView.BallPositionProvider = GetBallPosition;
    }

    /// <summary>
    /// 获取球体在窗口中的实际位置
    /// </summary>
    /// <returns>球体的左上角坐标</returns>
    private (double left, double top) GetBallPosition()
    {
        return (_ballLeft, _ballTop);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 使用用户配置的长按阈值
        var settings = DBContext.GetSetting();
        int ms = settings.LongPressMs;
        if (ms < 100) ms = 100;
        if (ms > 5000) ms = 5000;
        _longPressTimer.Interval = TimeSpan.FromMilliseconds(ms);

        _isDragging = false;
        _dragStartArgs = e;
        if (UseFullscreenHost)
        {
            // 记录指针相对球体左上角的偏移，便于平滑拖拽
            _pressPointInBall = e.GetPosition(Ball);
            // 捕获指针到窗口以获得全窗移动事件
            e.Pointer.Capture(this);
        }
        _longPressTimer.Start();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Ball 元素上的释放处理（保持与窗口级一致）
        HandlePointerReleased(e);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Handled)
            return;
        // 当释放发生在 Ball 之外（例如拖到外部后松开）时，也需要结束拖拽/交互
        HandlePointerReleased(e);
    }

    private void HandlePointerReleased(PointerReleasedEventArgs e)
    {
        _longPressTimer.Stop();
        if (UseFullscreenHost)
        {
            // 释放指针捕获
            e.Pointer.Capture(null);
        }
        if (_isExpanded)
        {
            bool triggered = TryTriggerItemUnderPointer(e);
            var settings = DBContext.GetSetting();
            if (!triggered || settings.MiniAutoCollapseAfterTrigger)
            {
                ResetToBall();
            }
        }
        else if (!_isDragging)
        {
            ResetToBall();
        }
        _dragStartArgs = null;
        _isDragging = false;

        if (UseFullscreenHost)
        {
            // 交互结束且未展开时恢复点击穿透
            if (!_isExpanded)
                SetWindowClickThrough(true);
        }
    }

    private void OnLongPress(object? sender, EventArgs e)
    {
        _longPressTimer.Stop();
        _isDragging = false;
        _isExpanded = true;
        if (UseFullscreenHost)
        {
            // 展开菜单期间需要可命中
            SetWindowClickThrough(false);
        }

        var settings = DBContext.GetSetting();
        var apps = DBContext.GetApplications();

        // 计算展开区域尺寸：2*(半径 + 项半径) + 16 边距
        double radius = settings.MiniRadius > 0 ? settings.MiniRadius : 60;
        double item = settings.MiniItemSize > 0 ? settings.MiniItemSize : 40;
        int targetSize = (int)Math.Round(2 * (radius + item / 2) + 16);

        if (!UseFullscreenHost)
        {
            // 记录展开前的像素中心，确保收起时完全回到同一中心（避免舍入误差导致的1px漂移）
            {
                double scale = RenderScaling;
                if (scale <= 0) scale = 1;
                int oldWidthPx = (int)Math.Round(Width * scale);
                int oldHeightPx = (int)Math.Round(Height * scale);
                var oldPosPx = Position;
                _fixedCenterPxDuringExpand = new PixelPoint(oldPosPx.X + oldWidthPx / 2, oldPosPx.Y + oldHeightPx / 2);
            }
            // 保持中心不变（DPI安全）
            ResizeWindowKeepingCenter(targetSize, targetSize);
        }
        else
        {
            // 全屏宿主：不调整窗口，仅在画布上定位 LauncherView
            PositionLauncherAroundBall(targetSize);
        }

        // 配置并淡入显示 LauncherView
        LauncherView.Opacity = 0;
        LauncherView.IsVisible = true;
        LauncherView.StartAngleDegrees = settings.MiniStartAngle;
        LauncherView.SweepAngleDegrees = settings.MiniSweepAngle;
        LauncherView.Radius = radius;
        LauncherView.ItemSize = item;
        LauncherView.SetValue(MicroDock.Views.Controls.CircularLauncherView.AutoDynamicArcProperty, settings.MiniAutoDynamicArc);
        LauncherView.Applications = apps;

        // 追加内建动作（带图标）
        LauncherView.ClearCustomItems();
        LauncherView.AddCustomItem("显示主窗", () =>
        {
            this.Hide();
            _miniModeService?.Disable();
        }, LoadAssetIcon("FloatBall.png"));
        LauncherView.AddCustomItem("置顶切换", () =>
        {
            // 占位：切换置顶通过 MainWindow 的 Topmost
            if (Owner is MainWindow mw)
            {
                mw.Topmost = !mw.Topmost;
            }
        }, LoadAssetIcon("Test.png"));
        LauncherView.AddCustomItem("打开设置", () =>
        {
            _miniModeService?.Disable();
            if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime life && life.MainWindow != null)
            {
                life.MainWindow.Show();
                life.MainWindow.Activate();
            }
        }, LoadAssetIcon("Test.png"));

        // 追加插件动作
        string appDirectory = System.AppContext.BaseDirectory;
        string pluginDirectory = System.IO.Path.Combine(appDirectory, "Plugins");
        var actions = Services.PluginLoader.LoadActions(pluginDirectory);
        foreach (var act in actions)
        {
            IImage? icon = Services.IconService.ImageFromBytes(act.IconBytes);
            LauncherView.AddCustomItem(act.Name, () =>
            {
                if (!string.IsNullOrWhiteSpace(act.Command))
                {
                    string args = string.IsNullOrWhiteSpace(act.Arguments) ? string.Empty : act.Arguments;
                    try
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo(act.Command, args)
                        {
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi);
                    }
                    catch
                    {
                    }
                }
            }, icon);
        }

        // 延后到下一帧开始淡入
        Dispatcher.UIThread.Post(() => LauncherView.Opacity = 1, DispatcherPriority.Background);
    }
    
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        ResetToBall();
    }
    
    private void ResetToBall()
    {
        // 强制结束任何进行中的拖拽/指针捕获，避免松开后仍然跟随
        if (_dragStartArgs != null)
        {
            try { _dragStartArgs.Pointer.Capture(null); } catch { }
        }
        _isDragging = false;
        _dragStartArgs = null;

        void ApplyBallSize()
        {
            if (UseFullscreenHost)
            {
                // 全屏宿主下不调整窗口尺寸，仅隐藏菜单
                _fixedCenterPxDuringExpand = null;
                return;
            }
            if (_fixedCenterPxDuringExpand.HasValue)
            {
                ResizeWindowAroundCenterPx(64, 64, _fixedCenterPxDuringExpand.Value);
                _fixedCenterPxDuringExpand = null;
            }
            else
            {
                ResizeWindowKeepingCenter(64, 64);
            }
        }

        if (LauncherView.IsVisible)
        {
            LauncherView.Opacity = 0; // 触发淡出
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(130) };
            t.Tick += (s, e) =>
            {
                t.Stop();
                LauncherView.IsVisible = false;
                ApplyBallSize();
                if (UseFullscreenHost)
                    SetWindowClickThrough(true);
            };
            t.Start();
        }
        else
        {
            ApplyBallSize();
            if (UseFullscreenHost)
                SetWindowClickThrough(true);
        }

        _isExpanded = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // 如果没有按下任何主键，确保终止拖拽状态，防止松开后仍然跟随
        var props = e.GetCurrentPoint(this).Properties;
        if (!props.IsLeftButtonPressed && !props.IsRightButtonPressed && !props.IsMiddleButtonPressed)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _dragStartArgs = null;
                if (UseFullscreenHost && !_isExpanded)
                {
                    // 交互结束，恢复点击穿透
                    SetWindowClickThrough(true);
                }
            }
            return;
        }

        if (UseFullscreenHost)
        {
            if (!_isExpanded && _longPressTimer.IsEnabled && _dragStartArgs != null)
            {
                _longPressTimer.Stop();
                _isDragging = true;
                // 进入拖拽时保证可命中
                SetWindowClickThrough(false);
            }
            if (_isDragging)
            {
                var pos = e.GetPosition(this);
                _ballLeft = pos.X - _pressPointInBall.X;
                _ballTop = pos.Y - _pressPointInBall.Y;
                Canvas.SetLeft(Ball, _ballLeft);
                Canvas.SetTop(Ball, _ballTop);
                if (LauncherView.IsVisible)
                {
                    // 保持菜单以球心为中心
                    if (_lastLauncherSize > 0)
                        PositionLauncherAroundBall(_lastLauncherSize);
                }
            }
            return;
        }
        if (!_isExpanded && _longPressTimer.IsEnabled && _dragStartArgs != null)
        {
            _longPressTimer.Stop();
            _isDragging = true;
            BeginMoveDrag(_dragStartArgs);
        }
    }

    private bool TryTriggerItemUnderPointer(PointerReleasedEventArgs e)
    {
        if (!LauncherView.IsVisible)
            return false;
        Avalonia.Controls.ItemsControl? items = LauncherView.FindControl<Avalonia.Controls.ItemsControl>("ItemsControl");
        if (items == null || items.ItemsPanelRoot == null)
            return false;
        Avalonia.Controls.Panel panel = items.ItemsPanelRoot;
        Avalonia.Point posInPanel = e.GetPosition(panel);
        for (int i = 0; i < panel.Children.Count; i++)
        {
            Avalonia.Layout.Layoutable? child = panel.Children[i] as Avalonia.Layout.Layoutable;
            Avalonia.Controls.Control? childCtrl = panel.Children[i] as Avalonia.Controls.Control;
            if (child == null || childCtrl == null)
                continue;
            Avalonia.Point? topLeft = child.TranslatePoint(new Avalonia.Point(0, 0), panel);
            if (topLeft == null)
                continue;
            Avalonia.Rect rect = new Avalonia.Rect(topLeft.Value, child.Bounds.Size);
            if (rect.Contains(posInPanel))
            {
                Views.Controls.ILauncherItem? item = childCtrl.DataContext as Views.Controls.ILauncherItem;
                if (item != null)
                {
                    item.OnTrigger?.Invoke();
                    return true;
                }
            }
        }
        return false;
    }

    // Interactive area enter/exit: toggle click-through on Windows
    private void OnInteractivePointerEntered(object? sender, PointerEventArgs e)
    {
        if (UseFullscreenHost)
            SetWindowClickThrough(false);
    }

    private void OnInteractivePointerExited(object? sender, PointerEventArgs e)
    {
        if (UseFullscreenHost)
        {
            // 只有在非交互状态下才恢复点击穿透
            if (!_isExpanded && !_isDragging)
                SetWindowClickThrough(true);
        }
    }

    private void TryInitHwnd()
    {
        if (_hwnd != 0)
            return;
        try
        {
            var handle = this.TryGetPlatformHandle();
            if (handle != null && string.Equals(handle.HandleDescriptor, "HWND", StringComparison.OrdinalIgnoreCase))
            {
                _hwnd = handle.Handle;
            }
        }
        catch { }
    }

    private void SetWindowClickThrough(bool enable)
    {
        if (_hwnd == 0)
            return;
        try
        {
            const int GWL_EXSTYLE = -20;
            const int WS_EX_TRANSPARENT = 0x00000020;
            int ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
            if (enable)
                ex |= WS_EX_TRANSPARENT;
            else
                ex &= ~WS_EX_TRANSPARENT;
            SetWindowLong(_hwnd, GWL_EXSTYLE, ex);
            _clickThrough = enable;
        }
        catch { }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    private static IImage? LoadAssetIcon(string fileName)
    {
        try
        {
            var uri = new Uri($"avares://MicroDock/Assets/Icon/{fileName}");
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private int _lastLauncherSize;
    private void PositionLauncherAroundBall(int size)
    {
        _lastLauncherSize = size;
        LauncherView.Width = size;
        LauncherView.Height = size;
        double centerX = _ballLeft + 32;
        double centerY = _ballTop + 32;
        Canvas.SetLeft(LauncherView, centerX - size / 2.0);
        Canvas.SetTop(LauncherView, centerY - size / 2.0);
    }

    // 在调整窗口大小时保持中心点不变（考虑 DPI 缩放）
    private void ResizeWindowKeepingCenter(int newWidthDip, int newHeightDip)
    {
        double scale = RenderScaling;
        if (scale <= 0)
            scale = 1;

        // 现有窗口的像素尺寸与位置
        int oldWidthPx = (int)Math.Round(Width * scale);
        int oldHeightPx = (int)Math.Round(Height * scale);
        Avalonia.PixelPoint oldPosPx = Position;

        int centerXPx = oldPosPx.X + oldWidthPx / 2;
        int centerYPx = oldPosPx.Y + oldHeightPx / 2;

        int newWidthPx = (int)Math.Round(newWidthDip * scale);
        int newHeightPx = (int)Math.Round(newHeightDip * scale);

        Avalonia.PixelPoint newPosPx = new Avalonia.PixelPoint(
            centerXPx - newWidthPx / 2,
            centerYPx - newHeightPx / 2
        );

        // 先改大小，再设置位置，避免部分平台在改大小后对位置进行一次内部校正导致跳动
        Width = newWidthDip;
        Height = newHeightDip;
        Position = newPosPx;
    }

    // 使用固定的屏幕像素中心缩放窗口（用于收起时完全回到展开前的中心）
    private void ResizeWindowAroundCenterPx(int newWidthDip, int newHeightDip, PixelPoint centerPx)
    {
        double scale = RenderScaling;
        if (scale <= 0)
            scale = 1;

        int newWidthPx = (int)Math.Round(newWidthDip * scale);
        int newHeightPx = (int)Math.Round(newHeightDip * scale);
        Avalonia.PixelPoint newPosPx = new Avalonia.PixelPoint(
            centerPx.X - newWidthPx / 2,
            centerPx.Y - newHeightPx / 2
        );

        // 先改大小，再设置位置
        Width = newWidthDip;
        Height = newHeightDip;
        Position = newPosPx;
    }
}
