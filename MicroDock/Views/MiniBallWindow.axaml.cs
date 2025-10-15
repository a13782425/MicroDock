using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MicroDock.Database;
using MicroDock.Services;
using System;
using Avalonia;
using Avalonia.Media;

namespace MicroDock.Views;

public partial class MiniBallWindow : Window
{
    private readonly DispatcherTimer _longPressTimer;
    private bool _isDragging;
    private bool _isExpanded;
    private PointerPressedEventArgs? _dragStartArgs;
    private readonly IMiniModeService? _miniModeService;

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
            TryTriggerItemUnderPointer(e);
            ResetToBall();
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
        
        System.Collections.Generic.List<Database.ApplicationDB> apps = DBContext.GetApplications();
        
        // 保持中心不变：记录旧中心，调整位置后再改变尺寸
        int oldWidth = (int)Width;
        int oldHeight = (int)Height;
        Avalonia.PixelPoint oldPos = Position;
        int newWidth = 200;
        int newHeight = 200;
        Avalonia.PixelPoint newPos = new Avalonia.PixelPoint(
            oldPos.X + (oldWidth / 2) - (newWidth / 2),
            oldPos.Y + (oldHeight / 2) - (newHeight / 2)
        );
        Position = newPos;
        Width = newWidth;
        Height = newHeight;
        
        LauncherView.IsVisible = true;
        LauncherView.Applications = apps;

        // 追加内建动作
        LauncherView.ClearCustomItems();
        LauncherView.AddCustomItem("显示主窗", () =>
        {
            this.Hide();
            _miniModeService?.Disable();
        });
        LauncherView.AddCustomItem("置顶切换", () =>
        {
            // 占位：切换置顶通过 MainWindow 的 Topmost
            if (Owner is MainWindow mw)
            {
                mw.Topmost = !mw.Topmost;
            }
        });
        LauncherView.AddCustomItem("打开设置", () =>
        {
            _miniModeService?.Disable();
            if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime life && life.MainWindow != null)
            {
                life.MainWindow.Show();
                life.MainWindow.Activate();
            }
        });

        // 追加插件动作
        string appDirectory = System.AppContext.BaseDirectory;
        string pluginDirectory = System.IO.Path.Combine(appDirectory, "Plugins");
        System.Collections.Generic.List<MicroDock.Plugin.MicroAction> actions = Services.PluginLoader.LoadActions(pluginDirectory);
        foreach (MicroDock.Plugin.MicroAction act in actions)
        {
            IImage? icon = Services.IconService.ImageFromBytes(act.IconBytes);
            LauncherView.AddCustomItem(act.Name, () =>
            {
                if (!string.IsNullOrWhiteSpace(act.Command))
                {
                    string args = string.IsNullOrWhiteSpace(act.Arguments) ? string.Empty : act.Arguments;
                    try
                    {
                        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(act.Command, args)
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
        LauncherView.IsVisible = false;
        // CancelButton.IsVisible = false;
        int oldWidth = (int)Width;
        int oldHeight = (int)Height;
        Avalonia.PixelPoint oldPos = Position;
        int newWidth = 64;
        int newHeight = 64;
        Avalonia.PixelPoint newPos = new Avalonia.PixelPoint(
            oldPos.X + (oldWidth / 2) - (newWidth / 2),
            oldPos.Y + (oldHeight / 2) - (newHeight / 2)
        );
        Position = newPos;
        Width = newWidth;
        Height = newHeight;
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
}
