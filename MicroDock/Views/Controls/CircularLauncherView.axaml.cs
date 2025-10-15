using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MicroDock.Database;
using MicroDock.Services;

namespace MicroDock.Views.Controls;

public interface ILauncherItem
{
    string Name { get; }
    IImage? Icon { get; }
    Action? OnTrigger { get; }
}

public class LauncherAppItem : ILauncherItem
{
    public ApplicationDB Application { get; set; }
    public string Name => Application?.Name ?? string.Empty;
    public IImage? Icon { get; set; }
    public Action? OnTrigger { get; set; }
    
    public LauncherAppItem(ApplicationDB app, Action? customAction = null)
    {
        Application = app;
        OnTrigger = customAction ?? (() => LaunchApplication(app));
        Icon = IconService.ImageFromBytes(app.Icon);
    }
    
    private void LaunchApplication(ApplicationDB app)
    {
        IconService.TryStartProcess(app.FilePath);
    }
}

public class LauncherActionItem : ILauncherItem
{
    public string Name { get; set; }
    public IImage? Icon { get; set; }
    public Action? OnTrigger { get; set; }
    
    public LauncherActionItem(string name, Action? action = null, IImage? icon = null)
    {
        Name = name;
        OnTrigger = action;
        Icon = icon;
    }
}

public partial class CircularLauncherView : UserControl
{
    public static readonly StyledProperty<IEnumerable<ApplicationDB>> ApplicationsProperty =
        AvaloniaProperty.Register<CircularLauncherView, IEnumerable<ApplicationDB>>(nameof(Applications));
    
    public static readonly StyledProperty<IMiniModeService?> MiniModeServiceProperty =
        AvaloniaProperty.Register<CircularLauncherView, IMiniModeService?>(nameof(MiniModeService));
    
    public static readonly StyledProperty<double> StartAngleDegreesProperty =
        AvaloniaProperty.Register<CircularLauncherView, double>(nameof(StartAngleDegrees), -90);
    
    public static readonly StyledProperty<double> SweepAngleDegreesProperty =
        AvaloniaProperty.Register<CircularLauncherView, double>(nameof(SweepAngleDegrees), 360);
    
    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<CircularLauncherView, double>(nameof(Radius), 60);
    
    public static readonly StyledProperty<double> ItemSizeProperty =
        AvaloniaProperty.Register<CircularLauncherView, double>(nameof(ItemSize), 40);
    
    static CircularLauncherView()
    {
        ApplicationsProperty.Changed.AddClassHandler<CircularLauncherView>((o, e) => o.OnApplicationsChanged());
    }

    public IEnumerable<ApplicationDB> Applications
    {
        get => GetValue(ApplicationsProperty);
        set => SetValue(ApplicationsProperty, value);
    }

    public IMiniModeService? MiniModeService
    {
        get => GetValue(MiniModeServiceProperty);
        set => SetValue(MiniModeServiceProperty, value);
    }
    
    public double StartAngleDegrees
    {
        get => GetValue(StartAngleDegreesProperty);
        set => SetValue(StartAngleDegreesProperty, value);
    }
    
    public double SweepAngleDegrees
    {
        get => GetValue(SweepAngleDegreesProperty);
        set => SetValue(SweepAngleDegreesProperty, value);
    }
    
    public double Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }
    
    public double ItemSize
    {
        get => GetValue(ItemSizeProperty);
        set => SetValue(ItemSizeProperty, value);
    }

    public CircularLauncherView()
    {
        InitializeComponent();
        LayoutUpdated += OnLayoutUpdated;
        
        // 确保初始化时也更新一次
        Loaded += (s, e) => UpdateItems();
    }
    
    private List<ILauncherItem> _customItems = new List<ILauncherItem>();
    
    /// <summary>
    /// 添加自定义动作项
    /// </summary>
    public void AddCustomItem(string name, Action? action, IImage? icon = null)
    {
        _customItems.Add(new LauncherActionItem(name, action, icon));
        UpdateItems();
    }
    
    /// <summary>
    /// 清空自定义动作项
    /// </summary>
    public void ClearCustomItems()
    {
        _customItems.Clear();
        UpdateItems();
    }
    
    private void OnApplicationsChanged()
    {
        InvalidateArrange();
        UpdateItems();
    }
    
    private void UpdateItems()
    {
        List<ILauncherItem> items = new List<ILauncherItem>();
        
        // 添加应用项
        if (Applications != null)
        {
            foreach (ApplicationDB app in Applications)
            {
                items.Add(new LauncherAppItem(app));
            }
        }
        
        // 添加自定义项
        items.AddRange(_customItems);
        
        // 添加退出mini模式按钮
        items.Add(new LauncherActionItem("退出", () => MiniModeService?.Disable()));
        
        ItemsControl.ItemsSource = items;
    }
    
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        ItemsControl itemsControl = this.FindControl<ItemsControl>("ItemsControl");
        if (itemsControl == null || itemsControl.ItemsPanelRoot == null) return;
        
        Panel panel = itemsControl.ItemsPanelRoot;
        int count = panel.Children.Count;
        if (count == 0) return;

        double radius = Radius > 0 ? Radius : Math.Min(panel.Bounds.Width, panel.Bounds.Height) / 2.5;
        double centerX = panel.Bounds.Width / 2;
        double centerY = panel.Bounds.Height / 2;

        // 动态边缘自适应：根据窗口靠近的边缘设置半环方向
        double dynamicStartDeg = StartAngleDegrees;
        double dynamicSweepDeg = SweepAngleDegrees;

        Window? window = this.VisualRoot as Window;
        if (window != null)
        {
            Avalonia.PixelPoint winPos = window.Position;
            Avalonia.Platform.Screen? screen = window.Screens.ScreenFromWindow(window);
            if (screen != null)
            {
                Avalonia.PixelRect wa = screen.WorkingArea;
                int margin = 48; // 边缘阈值

                bool nearLeft = winPos.X <= wa.X + margin;
                bool nearRight = winPos.X + (int)window.Width >= wa.Right - margin;
                bool nearTop = winPos.Y <= wa.Y + margin;
                bool nearBottom = winPos.Y + (int)window.Height >= wa.Bottom - margin;

                if (nearLeft && !nearRight)
                {
                    // 右半环（-90..90）
                    dynamicStartDeg = -90;
                    dynamicSweepDeg = 180;
                }
                else if (nearRight && !nearLeft)
                {
                    // 左半环（90..270）
                    dynamicStartDeg = 90;
                    dynamicSweepDeg = 180;
                }
                else if (nearTop && !nearBottom)
                {
                    // 下半环（0..180）
                    dynamicStartDeg = 0;
                    dynamicSweepDeg = 180;
                }
                else if (nearBottom && !nearTop)
                {
                    // 上半环（180..360）
                    dynamicStartDeg = 180;
                    dynamicSweepDeg = 180;
                }
                else
                {
                    dynamicStartDeg = StartAngleDegrees;
                    dynamicSweepDeg = SweepAngleDegrees;
                }
            }
        }

        double startRad = dynamicStartDeg * Math.PI / 180.0;
        double sweepRad = dynamicSweepDeg * Math.PI / 180.0;
        double step = count > 1 ? (sweepRad / count) : 0;

        for (int i = 0; i < count; i++)
        {
            Layoutable child = panel.Children[i] as Layoutable;
            if (child == null) continue;

            double angle = startRad + step * i;
            double x = centerX + radius * Math.Cos(angle) - child.DesiredSize.Width / 2;
            double y = centerY + radius * Math.Sin(angle) - child.DesiredSize.Height / 2;

            Canvas.SetLeft(child, x);
            Canvas.SetTop(child, y);
        }
    }
    
    private void Item_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Control element = sender as Control;
        if (element == null) return;
        ILauncherItem item = element.DataContext as ILauncherItem;
        if (item == null) return;
        item.OnTrigger?.Invoke();
    }
}
