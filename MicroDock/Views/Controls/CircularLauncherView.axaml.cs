using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using MicroDock.Database;
using MicroDock.Services;

namespace MicroDock.Views.Controls;

public interface ILauncherItem
{
    string Name { get; }
    Action? OnTrigger { get; }
}

public class LauncherAppItem : ILauncherItem
{
    public ApplicationDB Application { get; set; }
    public string Name => Application?.Name ?? string.Empty;
    public Action? OnTrigger { get; set; }
    
    public LauncherAppItem(ApplicationDB app, Action? customAction = null)
    {
        Application = app;
        OnTrigger = customAction ?? (() => LaunchApplication(app));
    }
    
    private void LaunchApplication(ApplicationDB app)
    {
        try
        {
            Process.Start(new ProcessStartInfo(app.FilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting application: {ex.Message}");
        }
    }
}

public class LauncherActionItem : ILauncherItem
{
    public string Name { get; set; }
    public Action? OnTrigger { get; set; }
    
    public LauncherActionItem(string name, Action? action = null)
    {
        Name = name;
        OnTrigger = action;
    }
}

public partial class CircularLauncherView : UserControl
{
    public static readonly StyledProperty<IEnumerable<ApplicationDB>> ApplicationsProperty =
        AvaloniaProperty.Register<CircularLauncherView, IEnumerable<ApplicationDB>>(nameof(Applications));
    
    public static readonly StyledProperty<IMiniModeService?> MiniModeServiceProperty =
        AvaloniaProperty.Register<CircularLauncherView, IMiniModeService?>(nameof(MiniModeService));
    
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
    public void AddCustomItem(string name, Action? action)
    {
        _customItems.Add(new LauncherActionItem(name, action));
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
        var itemsControl = this.FindControl<ItemsControl>("ItemsControl");
        if (itemsControl == null || itemsControl.ItemsPanelRoot == null) return;
        
        var panel = itemsControl.ItemsPanelRoot;
        int count = panel.Children.Count;
        if (count == 0) return;

        double radius = Math.Min(panel.Bounds.Width, panel.Bounds.Height) / 2.5;
        double centerX = panel.Bounds.Width / 2;
        double centerY = panel.Bounds.Height / 2;

        for (int i = 0; i < count; i++)
        {
            var child = panel.Children[i] as Layoutable;
            if (child == null) continue;

            double angle = 2 * Math.PI / count * i;
            double x = centerX + radius * Math.Cos(angle) - child.DesiredSize.Width / 2;
            double y = centerY + radius * Math.Sin(angle) - child.DesiredSize.Height / 2;

            Canvas.SetLeft(child, x);
            Canvas.SetTop(child, y);
        }
    }
    
    private void Item_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Control element) return;
        
        if (element.DataContext is ILauncherItem item)
        {
            item.OnTrigger?.Invoke();
        }
    }
}
