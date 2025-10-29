using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using MicroDock.Database;
using MicroDock.Infrastructure;
using System;
using System.Collections.Generic;

namespace MicroDock.Views;

/// <summary>
/// 功能栏窗口，用于显示应用和动作的环形菜单
/// </summary>
public partial class LauncherWindow : Window
{
    private PixelPoint _ballCenterPosition;
    private PixelPoint _ballPosition;
    
    public LauncherWindow()
    {
        InitializeComponent();
        
        // 失去焦点时自动关闭（类似右键菜单）
        this.LostFocus += OnLostFocus;
        
        // ESC键关闭
        this.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        };
    }
    
    /// <summary>
    /// 显示功能栏窗口，围绕指定的悬浮球中心点
    /// </summary>
    /// <param name="ballCenterPx">悬浮球的屏幕中心点（像素坐标）</param>
    /// <param name="ballPosition">悬浮球的窗口位置（用于边缘检测）</param>
    public void ShowAroundBall(PixelPoint ballCenterPx, PixelPoint ballPosition)
    {
        _ballCenterPosition = ballCenterPx;
        _ballPosition = ballPosition;
        
        SettingDB settings = DBContext.GetSetting();
        List<ApplicationDB> apps = DBContext.GetApplications();
        
        // 计算窗口尺寸：2*(半径 + 项半径) + 16 边距
        double radius = settings.MiniRadius > 0 ? settings.MiniRadius : 60;
        double itemSize = settings.MiniItemSize > 0 ? settings.MiniItemSize : 40;
        int windowSize = (int)Math.Round(2 * (radius + itemSize / 2) + 16);
        
        // 设置窗口大小
        Width = windowSize;
        Height = windowSize;
        
        // 计算窗口位置（保持中心点对齐悬浮球中心）
        double scale = 1.0; // 先使用默认值，窗口显示后会更新
        Position = Services.WindowPositionCalculator.CalculatePositionAroundCenter(
            ballCenterPx, windowSize, windowSize, scale);
        
        System.Diagnostics.Debug.WriteLine($"[LauncherWindow] 初始位置: Position={Position}, Size={Width}x{Height}, BallCenter={ballCenterPx}");
        
        // 配置环形菜单
        ConfigureLauncherView(settings, apps, radius, itemSize, windowSize, ballPosition);
        
        // 显示窗口
        Show();
        
        // 窗口显示后，使用实际的 DPI 缩放重新计算位置
        Opened += (_, _) =>
        {
            scale = RenderScaling;
            if (scale <= 0) scale = 1;
            
            PixelPoint correctedPosition = Services.WindowPositionCalculator.CalculatePositionAroundCenter(
                ballCenterPx, windowSize, windowSize, scale);
            
            if (correctedPosition != Position)
            {
                Position = correctedPosition;
                System.Diagnostics.Debug.WriteLine($"[LauncherWindow] DPI校正后位置: Position={Position}, DPI={scale:F2}");
            }
            
            // 激活窗口以获取焦点
            Activate();
        };
    }
    
    private void ConfigureLauncherView(SettingDB settings, List<ApplicationDB> apps, double radius, double itemSize, int windowSize, PixelPoint ballPosition)
    {
        // 计算环形菜单的中心点（相对于窗口的坐标）
        double centerX = windowSize / 2.0;
        double centerY = windowSize / 2.0;
        
        LauncherView.CenterPointX = centerX;
        LauncherView.CenterPointY = centerY;
        LauncherView.OriginalWindowPosition = ballPosition; // 传递悬浮球位置用于边缘检测
        LauncherView.StartAngleDegrees = settings.MiniStartAngle;
        LauncherView.SweepAngleDegrees = settings.MiniSweepAngle;
        LauncherView.Radius = radius;
        LauncherView.ItemSize = itemSize;
        LauncherView.SetValue(Controls.CircularLauncherView.AutoDynamicArcProperty, settings.MiniAutoDynamicArc);
        LauncherView.Applications = apps;
        
        // 配置自定义动作
        ConfigureLauncherActions();
    }
    
    private void ConfigureLauncherActions()
    {
        LauncherView.ClearCustomItems();
        
        // 显示主窗 - 通过事件禁用迷你模式
        LauncherView.AddCustomItem("显示主窗", () =>
        {
            EventAggregator.Instance.Publish(new MiniModeChangeRequestMessage(false));
            Close();
        }, LoadAssetIcon("FloatBall.png"));
        
        // 置顶切换 - 通过事件请求切换置顶状态
        LauncherView.AddCustomItem("置顶切换", () =>
        {
            EventAggregator.Instance.Publish(new WindowTopmostChangeRequestMessage(true));
            Close();
        }, LoadAssetIcon("Test.png"));
        
        // 打开设置 - 禁用迷你模式并导航到设置标签页
        LauncherView.AddCustomItem("打开设置", () =>
        {
            EventAggregator.Instance.Publish(new MiniModeChangeRequestMessage(false));
            EventAggregator.Instance.Publish(new NavigateToTabMessage("设置"));
            Close();
        }, LoadAssetIcon("Test.png"));

        // 追加插件动作
        string appDirectory = System.AppContext.BaseDirectory;
        string pluginDirectory = System.IO.Path.Combine(appDirectory, "Plugins");
        List<MicroDock.Plugin.MicroAction> actions = Services.PluginLoader.LoadActions(pluginDirectory);
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
                Close();
            }, icon);
        }
    }
    
    private static IImage? LoadAssetIcon(string fileName)
    {
        try
        {
            Uri uri = new Uri($"avares://MicroDock/Assets/Icon/{fileName}");
            using System.IO.Stream stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
    
    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        // 失去焦点时关闭窗口（类似右键菜单行为）
        Close();
    }
}

