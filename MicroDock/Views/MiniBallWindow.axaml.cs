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

namespace MicroDock.Views;

public enum BallState
{
    Ball,           // 悬浮球状态
    Expanding,      // 正在展开
    Expanded,       // 已展开
    Collapsing      // 正在收起
}
/*
 * 屏幕坐标是左上角为(0,0),右下角为屏幕大小
 * 窗口是左上角为原点，右下角为最大值
 */

public partial class MiniBallWindow : Window
{
    private readonly DispatcherTimer _longPressTimer;
    private bool _isDragging;
    private BallState _currentState;
    private PointerPressedEventArgs? _dragStartArgs;
    private readonly IMiniModeService? _miniModeService;
    private PixelPoint _savedCenterPx;  // 保存悬浮球的屏幕中心点
    private PixelPoint _originPos;
    // Parameterless for designer
    public MiniBallWindow()
    {
        InitializeComponent();
        _longPressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _longPressTimer.Tick += OnLongPress;
        _currentState = BallState.Ball;
        this.KeyDown += (_, e) =>
        {
            if (e.Handled) return;
            if (e.Key == Key.Escape)
            {
                ResetToBall();
                e.Handled = true;
            }
        };
        
        this.Opened += (_, _) =>
        {

            this.Position = new PixelPoint(2560-121, 1440-64);
        };
    }

    public MiniBallWindow(IMiniModeService miniModeService) : this()
    {
        _miniModeService = miniModeService;
        LauncherView.MiniModeService = miniModeService;
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
        _longPressTimer.Start();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _longPressTimer.Stop();
        if (_currentState == BallState.Expanded)
        {
            bool triggered = TryTriggerItemUnderPointer(e);
            var settings = DBContext.GetSetting();
            if (!triggered || settings.MiniAutoCollapseAfterTrigger)
            {
                ResetToBall();
            }
        }
        else if (!_isDragging && _currentState == BallState.Ball)
        {
            // 短按切换状态（当前未实现切换逻辑）
        }
        _dragStartArgs = null;
        e.Handled = true;
    }
    private void OnLongPress(object? sender, EventArgs e)
    {
        _longPressTimer.Stop();
        _isDragging = false;

        // 防止重复触发
        if (_currentState != BallState.Ball)
            return;

        _currentState = BallState.Expanding;

        var settings = DBContext.GetSetting();
        var apps = DBContext.GetApplications();

        // 确保窗口大小正确（修复可能的异常状态）
        //if (Math.Abs(Width - 64) > 1 || Math.Abs(Height - 64) > 1)
        //{
        //    System.Diagnostics.Debug.WriteLine($"[MiniBall-新] 警告：窗口大小异常 {Width}x{Height}，修正为 64x64");
        //    Width = 64;
        //    Height = 64;
        //}

        // 计算展开窗口尺寸：2*(半径 + 项半径) + 16 边距
        double radius = settings.MiniRadius > 0 ? settings.MiniRadius : 60;
        double item = settings.MiniItemSize > 0 ? settings.MiniItemSize : 40;
        int targetSize = (int)Math.Round(2 * (radius + item / 2) + 16);

        // 计算并保存悬浮球的中心点（屏幕像素坐标）
        double scale = RenderScaling;
        if (scale <= 0) scale = 1;
        _originPos = Position;
        PixelPoint originalPosition = Position;
        _savedCenterPx = Services.WindowPositionCalculator.CalculateCenter(
            originalPosition, Width, Height, scale);

        System.Diagnostics.Debug.WriteLine($"[MiniBall-新] 展开开始: Position={originalPosition}, Size={Width}x{Height}, Center={_savedCenterPx}, DPI={scale:F2}");

        // 步骤1: 淡出悬浮球
        var fadeOutTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        Ball.Opacity = 1;

        // 启动淡出动画
        Dispatcher.UIThread.Post(() => Ball.Opacity = 0, DispatcherPriority.Render);

        fadeOutTimer.Tick += (s, args) =>
        {
            fadeOutTimer.Stop();

            // 步骤2: 悬浮球完全隐藏后，调整窗口大小和位置
            Ball.IsVisible = false;

            // 计算新窗口位置（保持中心点不变）
            PixelPoint newPosition = Services.WindowPositionCalculator.CalculatePositionAroundCenter(
                _savedCenterPx, targetSize, targetSize, scale);

            Width = targetSize;
            Height = targetSize;
            Position = newPosition;

            System.Diagnostics.Debug.WriteLine($"[MiniBall-新] 窗口调整: Position={Position}, Size={Width}x{Height}");

            // 步骤3: 配置环形菜单
            // 计算环形菜单的中心点（相对于新窗口的坐标）
            double centerX = targetSize / 2.0;
            double centerY = targetSize / 2.0;

            LauncherView.CenterPointX = centerX;
            LauncherView.CenterPointY = centerY;
            LauncherView.OriginalWindowPosition = originalPosition;
            LauncherView.StartAngleDegrees = settings.MiniStartAngle;
            LauncherView.SweepAngleDegrees = settings.MiniSweepAngle;
            LauncherView.Radius = radius;
            LauncherView.ItemSize = item;
            LauncherView.SetValue(MicroDock.Views.Controls.CircularLauncherView.AutoDynamicArcProperty, settings.MiniAutoDynamicArc);
            LauncherView.Applications = apps;

            // 配置自定义动作
            ConfigureLauncherActions();

            // 步骤4: 显示并淡入环形菜单
            LauncherView.Opacity = 0;
            LauncherView.IsVisible = true;
            Ball.IsVisible = true;
            Ball.Opacity = 0;


            Dispatcher.UIThread.Post(() =>
            {
                LauncherView.Opacity = 1;
                Ball.Opacity = 1;
                _currentState = BallState.Expanded;
                System.Diagnostics.Debug.WriteLine($"[MiniBall-新] 展开完成");
            }, DispatcherPriority.Render);
        };

        fadeOutTimer.Start();
    }

    private void ConfigureLauncherActions()
    {
        LauncherView.ClearCustomItems();
        LauncherView.AddCustomItem("显示主窗", () =>
        {
            this.Hide();
            _miniModeService?.Disable();
        }, LoadAssetIcon("FloatBall.png"));
        LauncherView.AddCustomItem("置顶切换", () =>
        {
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
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        ResetToBall();
    }

    private void ResetToBall()
    {
        // 防止重复触发
        if (_currentState != BallState.Expanded)
            return;

        _currentState = BallState.Collapsing;

        System.Diagnostics.Debug.WriteLine($"[MiniBall-新] 收起开始: Position={Position}, Size={Width}x{Height}");

        // 步骤1: 淡出环形菜单
        var fadeOutTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };

        // 启动淡出动画
        Dispatcher.UIThread.Post(() => LauncherView.Opacity = 0, DispatcherPriority.Render);

        fadeOutTimer.Tick += (s, args) =>
        {
            fadeOutTimer.Stop();

            // 步骤2: 环形菜单完全隐藏后，调整窗口大小和位置
            LauncherView.IsVisible = false;

            // 计算新窗口位置（基于保存的中心点）
            double scale = RenderScaling;
            if (scale <= 0) scale = 1;

            PixelPoint positionBefore = Position;
            Width = 64;
            Height = 64;
           

            // 计算位置偏差（不只是中心点）
            PixelPoint finalCenter = Services.WindowPositionCalculator.CalculateCenter(
                Position, Width, Height, scale);
            PixelPoint centerOffset = new PixelPoint(
                finalCenter.X - _savedCenterPx.X,
                finalCenter.Y - _savedCenterPx.Y
            );
            PixelPoint positionOffset = new PixelPoint(
                Position.X - positionBefore.X,
                Position.Y - positionBefore.Y
            );

            System.Diagnostics.Debug.WriteLine($"[MiniBall-新] 窗口调整: Position={Position}, Size={Width}x{Height}");
            System.Diagnostics.Debug.WriteLine($"[MiniBall-新] 中心偏差: {centerOffset}, 位置偏差: {positionOffset}");

            // 步骤3: 显示并淡入悬浮球
            Ball.Opacity = 0;
            Ball.IsVisible = true;

            Dispatcher.UIThread.Post(() =>
            {
                PixelPoint newPosition = Services.WindowPositionCalculator.CalculatePositionAroundCenter(
               _savedCenterPx, Width, Height, scale);
                Position = newPosition;
                Ball.Opacity = 1;
                _currentState = BallState.Ball;
                System.Diagnostics.Debug.WriteLine($"[MiniBall-新] 收起完成: 最终Position={Position}, Size={Width}x{Height}");
            }, DispatcherPriority.Render);
        };

        fadeOutTimer.Start();
    }
    //鼠标进入
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
    }

    //鼠标退出
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
    }
    //鼠标移动
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // 在 Ball 状态下开始拖动
        if (_currentState == BallState.Ball && _longPressTimer.IsEnabled && _dragStartArgs != null)
        {
            _longPressTimer.Stop();
            _isDragging = true;

            // 确保悬浮球可见
            if (!Ball.IsVisible)
            {
                Ball.IsVisible = true;
                Ball.Opacity = 1;
            }

            BeginMoveDrag(_dragStartArgs);
        }
        // 在 Expanded 状态下拖动时，先收起到 Ball 状态
        else if (_currentState == BallState.Expanded && _longPressTimer.IsEnabled && _dragStartArgs != null)
        {
            _longPressTimer.Stop();
            _isDragging = true;

            // 立即隐藏环形菜单并显示悬浮球
            LauncherView.IsVisible = false;
            LauncherView.Opacity = 0;

            // 调整窗口到悬浮球大小
            double scale = RenderScaling;
            if (scale <= 0) scale = 1;

            PixelPoint newPosition = Services.WindowPositionCalculator.CalculatePositionAroundCenter(
                _savedCenterPx, 64, 64, scale);

            Width = 64;
            Height = 64;
            Position = newPosition;

            // 显示悬浮球
            Ball.IsVisible = true;
            Ball.Opacity = 1;
            _currentState = BallState.Ball;

            System.Diagnostics.Debug.WriteLine($"[MiniBall-新] 拖动触发收起");

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

    // 在调整窗口大小时保持中心点不变（考虑 DPI 缩放）
    private void ResizeWindowKeepingCenter(int newWidthDip, int newHeightDip)
    {
        double scale = RenderScaling;
        if (scale <= 0)
            scale = 1;

        PixelPoint newPosition = Services.WindowPositionCalculator.CalculateNewPosition(
            Position, Width, Height, newWidthDip, newHeightDip, scale);

        // 先改大小，再设置位置，避免部分平台在改大小后对位置进行一次内部校正导致跳动
        Width = newWidthDip;
        Height = newHeightDip;
        Position = newPosition;
    }

    // 使用固定的屏幕像素中心缩放窗口（用于收起时完全回到展开前的中心）
    private void ResizeWindowAroundCenterPx(int newWidthDip, int newHeightDip, PixelPoint centerPx)
    {
        double scale = RenderScaling;
        if (scale <= 0)
            scale = 1;

        PixelPoint newPosition = Services.WindowPositionCalculator.CalculatePositionAroundCenter(
            centerPx, newWidthDip, newHeightDip, scale);

        // 先改大小，再设置位置
        Width = newWidthDip;
        Height = newHeightDip;
        Position = newPosition;
    }
}
