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
        byte[]? iconData = Database.DBContext.GetIconData(app.IconHash);
        Icon = IconService.ImageFromBytes(iconData);
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

    public static readonly StyledProperty<bool> AutoDynamicArcProperty =
        AvaloniaProperty.Register<CircularLauncherView, bool>(nameof(AutoDynamicArc), true);
    
    // 用于保存展开前的窗口位置，用于精确的边缘判定
    public static readonly StyledProperty<PixelPoint?> OriginalWindowPositionProperty =
        AvaloniaProperty.Register<CircularLauncherView, PixelPoint?>(nameof(OriginalWindowPosition), null);
    
    // 指定环形菜单的中心点（相对于窗口的坐标）
    public static readonly StyledProperty<double> CenterPointXProperty =
        AvaloniaProperty.Register<CircularLauncherView, double>(nameof(CenterPointX), -1);
    
    public static readonly StyledProperty<double> CenterPointYProperty =
        AvaloniaProperty.Register<CircularLauncherView, double>(nameof(CenterPointY), -1);
    
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

    public bool AutoDynamicArc
    {
        get => GetValue(AutoDynamicArcProperty);
        set => SetValue(AutoDynamicArcProperty, value);
    }
    
    public PixelPoint? OriginalWindowPosition
    {
        get => GetValue(OriginalWindowPositionProperty);
        set => SetValue(OriginalWindowPositionProperty, value);
    }
    
    public double CenterPointX
    {
        get => GetValue(CenterPointXProperty);
        set => SetValue(CenterPointXProperty, value);
    }
    
    public double CenterPointY
    {
        get => GetValue(CenterPointYProperty);
        set => SetValue(CenterPointYProperty, value);
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
        
        // 使用指定的中心点，如果未指定则使用面板中心
        double centerX = CenterPointX >= 0 ? CenterPointX : panel.Bounds.Width / 2;
        double centerY = CenterPointY >= 0 ? CenterPointY : panel.Bounds.Height / 2;

        // 动态边缘自适应：根据窗口靠近的边缘设置半环方向
        double dynamicStartDeg = StartAngleDegrees;
        double dynamicSweepDeg = SweepAngleDegrees;

        if (AutoDynamicArc)
        {
            Window? window = this.VisualRoot as Window;
            if (window != null)
            {
                Avalonia.Platform.Screen? screen = window.Screens.ScreenFromWindow(window);
                if (screen != null)
                {
                    Avalonia.PixelRect wa = screen.WorkingArea;
                    int margin = 48; // 边缘阈值
                    double scale = window.RenderScaling;
                    if (scale <= 0) scale = 1;

                    // 使用原始窗口位置（展开前）进行边缘判定，如果没有则使用当前位置
                    Avalonia.PixelPoint positionToCheck = OriginalWindowPosition ?? window.Position;
                    
                    // 使用 WindowPositionCalculator 进行边缘检测
                    (bool nearLeft, bool nearRight, bool nearTop, bool nearBottom) = 
                        Services.WindowPositionCalculator.CheckEdgeProximity(
                            positionToCheck, window.Width, window.Height, wa, margin, scale);

                    // 使用 WindowPositionCalculator 计算最佳弧线方向
                    (dynamicStartDeg, dynamicSweepDeg) = 
                        Services.WindowPositionCalculator.CalculateOptimalArc(
                            nearLeft, nearRight, nearTop, nearBottom,
                            StartAngleDegrees, SweepAngleDegrees);
                }
            }
        }
        else
        {
            dynamicStartDeg = StartAngleDegrees;
            dynamicSweepDeg = SweepAngleDegrees;
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
        // 防止事件冒泡导致父级兜底触发（例如 MiniBallWindow 命中检测）
        e.Handled = true;
    }
}
