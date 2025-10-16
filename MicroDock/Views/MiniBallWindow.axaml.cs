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

public partial class MiniBallWindow : Window
{
    private readonly DispatcherTimer _longPressTimer;
    private bool _isDragging;
    private bool _isExpanded;
    private PointerPressedEventArgs? _dragStartArgs;
    private readonly IMiniModeService? _miniModeService;
    private PixelPoint? _fixedCenterPxDuringExpand;

    // Parameterless for designer
    public MiniBallWindow()
    {
        InitializeComponent();
        _longPressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _longPressTimer.Tick += OnLongPress;
        _isExpanded = false;
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
        e.Handled = true;
    }

    private void OnLongPress(object? sender, EventArgs e)
    {
        _longPressTimer.Stop();
        _isDragging = false;
        _isExpanded = true;

        var settings = DBContext.GetSetting();
        var apps = DBContext.GetApplications();

        // 计算展开窗口尺寸：2*(半径 + 项半径) + 16 边距
        double radius = settings.MiniRadius > 0 ? settings.MiniRadius : 60;
        double item = settings.MiniItemSize > 0 ? settings.MiniItemSize : 40;
        int targetSize = (int)Math.Round(2 * (radius + item / 2) + 16);

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
        void ApplyBallSize()
        {
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
            };
            t.Start();
        }
        else
        {
            ApplyBallSize();
        }

        _isExpanded = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
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
