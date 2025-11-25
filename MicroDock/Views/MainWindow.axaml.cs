using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using DesktopNotifications;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using MicroDock;
using MicroDock.Database;
using MicroDock.Service;
using MicroDock.ViewModels;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MicroDock.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        // 订阅事件消息
        SubscribeToMessages();
        // 在窗口打开后初始化设置
        this.Opened += OnWindowOpened;
        // 恢复窗口位置和大小（需要在窗口显示前设置）
        RestoreWindowState();
        //m_initSettings();
        var splashViewModel = new AppSplashViewModel();
        splashViewModel.LoadingCompleted += OnSplashLoadingCompleted;
        SplashScreen = splashViewModel;

    }
    private void OnSplashLoadingCompleted(object? sender, EventArgs e)
    {
        // 启动完成，切换到主视图
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (sender is AppSplashViewModel splashVm)
            {
                splashVm.LoadingCompleted -= OnSplashLoadingCompleted;
            }
            // 获取 MainWindowViewModel 并设置主内容
            if (DataContext is MainWindowViewModel mainWindowVm)
            {
                // 创建并显示主视图
                mainWindowVm.MainContent = new MainViewModel();
            }
        });
    }
    /// <summary>
    /// 窗口打开事件处理
    /// </summary>
    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // 初始化应用内通知管理器
        InitializeWindowNotificationManager();

        // 注册通知事件
        Program.NotificationManager.NotificationActivated += OnNotificationActivated;
        Program.NotificationManager.NotificationDismissed += OnNotificationDismissed;

        // 从数据库加载设置并应用服务状态
        InitializeServicesFromSettings();
    }
    private void InitializeWindowNotificationManager()
    {
        Program.WindowNotificationManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.TopRight,
            MaxItems = 3
        };

        LogService.LogDebug("WindowNotificationManager 已初始化");
    }
    /// <summary>
    /// 订阅事件消息
    /// </summary>
    private void SubscribeToMessages()
    {
        ServiceLocator.Get<EventService>().Subscribe<MainWindowShowMessage>(OnWindowShowRequest);
        ServiceLocator.Get<EventService>().Subscribe<MainWindowHideMessage>(OnWindowHideRequest);
        ServiceLocator.Get<EventService>().Subscribe<AutoHideChangeRequestMessage>(OnAutoHideChangeRequest);
        ServiceLocator.Get<EventService>().Subscribe<AutoStartupChangeRequestMessage>(OnAutoStartupChangeRequest);
        ServiceLocator.Get<EventService>().Subscribe<WindowTopmostChangeRequestMessage>(OnTopmostChangeRequest);
    }

    /// <summary>
    /// 处理窗口显示请求
    /// </summary>
    private void OnWindowShowRequest(MainWindowShowMessage message)
    {
        this.Show();
        this.Activate();
    }

    /// <summary>
    /// 处理窗口隐藏请求
    /// </summary>
    private void OnWindowHideRequest(MainWindowHideMessage message)
    {
        this.Hide();
    }

    /// <summary>
    /// 处理自动隐藏变更请求
    /// </summary>
    private void OnAutoHideChangeRequest(AutoHideChangeRequestMessage message)
    {
        var autoHideService = ServiceLocator.Get<AutoHideService>();
        if (message.Enable)
        {
            autoHideService.Enable();
        }
        else
        {
            autoHideService.Disable();
        }

        ServiceLocator.Get<EventService>().Publish(new ServiceStateChangedMessage("AutoHide", message.Enable));
    }

    /// <summary>
    /// 处理开机自启动变更请求
    /// </summary>
    private void OnAutoStartupChangeRequest(AutoStartupChangeRequestMessage message)
    {
        var autoStartupService = ServiceLocator.Get<AutoStartupService>();
        if (message.Enable)
        {
            autoStartupService.Enable();
        }
        else
        {
            autoStartupService.Disable();
        }

        ServiceLocator.Get<EventService>().Publish(new ServiceStateChangedMessage("AutoStartup", message.Enable));
    }

    /// <summary>
    /// 处理置顶状态变更请求
    /// </summary>
    private void OnTopmostChangeRequest(WindowTopmostChangeRequestMessage message)
    {
        var topMostService = ServiceLocator.Get<TopMostService>();
        if (message.Enable)
        {
            // 切换置顶状态
            this.Topmost = !this.Topmost;
            topMostService.Enable();
        }
        else
        {
            topMostService.Disable();
        }

        ServiceLocator.Get<EventService>().Publish(new ServiceStateChangedMessage("AlwaysOnTop", this.Topmost));
    }

    /// <summary>
    /// 从数据库设置初始化服务状态
    /// </summary>
    private void InitializeServicesFromSettings()
    {
        SettingDB settings = DBContext.GetSetting();

        // 应用初始配置
        if (settings.AutoStartup)
        {
            ServiceLocator.Get<AutoStartupService>().Enable();
        }

        if (settings.AutoHide)
        {
            ServiceLocator.Get<AutoHideService>().Enable();
        }

        if (settings.AlwaysOnTop)
        {
            ServiceLocator.Get<TopMostService>().Enable();
        }
    }

    private void OnNotificationDismissed(object? sender, NotificationDismissedEventArgs e)
    {
        LogService.LogDebug($"通知已关闭: {e.Reason}");
    }

    private void OnNotificationActivated(object? sender, NotificationActivatedEventArgs e)
    {
        LogService.LogInformation($"通知已激活: {e.ActionId}");
    }

    /// <summary>
    /// 窗口关闭事件 - 释放资源
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (AppConfig.RealExit)
        {
            if (this.WindowState != WindowState.Minimized)
                SaveWindowState();
            return;
        }
        SaveWindowState();
        e.Cancel = true;
        this.WindowState = WindowState.Minimized;
        Hide();
        Program.NotificationManager.ShowNotification(new DesktopNotifications.Notification() { Title = "MicroDock 已最小化到托盘", Body = "您可以通过系统托盘图标重新打开主窗口。" });
    }

    /// <summary>
    /// 保存窗口位置和大小到数据库
    /// </summary>
    private void SaveWindowState()
    {
        try
        {
            var position = this.Position;
            DBContext.UpdateSetting(settings =>
            {
                settings.WindowX = position.X;
                settings.WindowY = position.Y;
                settings.WindowWidth = (int)this.Width;
                settings.WindowHeight = (int)this.Height;
            });
            Log.Debug("窗口状态已保存: X={X}, Y={Y}, Width={Width}, Height={Height}",
                position.X, position.Y, this.Width, this.Height);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存窗口状态失败");
        }
    }

    /// <summary>
    /// 从数据库恢复窗口位置和大小
    /// </summary>
    private void RestoreWindowState()
    {
        try
        {
            var settings = DBContext.GetSetting();

            // 检查是否有保存的窗口状态
            if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
            {
                int x = settings.WindowX;
                int y = settings.WindowY;
                int width = settings.WindowWidth;
                int height = settings.WindowHeight;

                // 验证位置和大小是否有效
                if (ValidateAndAdjustWindowPosition(ref x, ref y, width, height))
                {
                    // 设置窗口大小
                    this.Width = width;
                    this.Height = height;

                    // 设置窗口位置
                    this.Position = new PixelPoint(x, y);

                    Log.Debug("窗口状态已恢复: X={X}, Y={Y}, Width={Width}, Height={Height}",
                        x, y, width, height);
                }
                else
                {
                    Log.Debug("保存的窗口位置无效，使用默认位置和大小");
                }
            }
            else
            {
                Log.Debug("未找到保存的窗口状态，使用默认值");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "恢复窗口状态失败");
        }
    }

    /// <summary>
    /// 验证并调整窗口位置，确保窗口在屏幕可见范围内
    /// </summary>
    /// <param name="x">窗口X坐标（引用传递，可能被调整）</param>
    /// <param name="y">窗口Y坐标（引用传递，可能被调整）</param>
    /// <param name="width">窗口宽度</param>
    /// <param name="height">窗口高度</param>
    /// <returns>如果位置有效或已调整，返回true；否则返回false</returns>
    private bool ValidateAndAdjustWindowPosition(ref int x, ref int y, int width, int height)
    {
        try
        {
            // 获取所有屏幕
            var screens = this.Screens.All;
            if (screens == null || screens.Count == 0)
            {
                Log.Warning("无法获取屏幕信息");
                return false;
            }

            // 检查窗口是否至少有一部分在任何屏幕的可见区域内
            bool isValid = false;
            PixelRect? bestScreen = null;
            int minDistance = int.MaxValue;

            foreach (var screen in screens)
            {
                var workingArea = screen.WorkingArea;

                // 计算窗口矩形
                var windowRect = new PixelRect(x, y, width, height);

                // 检查窗口是否与工作区有交集（允许窗口部分在屏幕外，但至少有一部分可见）
                if (windowRect.Intersects(workingArea))
                {
                    isValid = true;

                    // 计算窗口中心到屏幕中心的距离，选择最近的屏幕
                    int windowCenterX = x + width / 2;
                    int windowCenterY = y + height / 2;
                    int screenCenterX = workingArea.X + workingArea.Width / 2;
                    int screenCenterY = workingArea.Y + workingArea.Height / 2;

                    int distance = Math.Abs(windowCenterX - screenCenterX) + Math.Abs(windowCenterY - screenCenterY);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestScreen = workingArea;
                    }
                }
            }

            if (!isValid)
            {
                // 窗口完全在所有屏幕外，调整到主屏幕中心
                var primaryScreen = this.Screens.Primary;
                if (primaryScreen != null)
                {
                    var workingArea = primaryScreen.WorkingArea;
                    x = workingArea.X + (workingArea.Width - width) / 2;
                    y = workingArea.Y + (workingArea.Height - height) / 2;
                    Log.Debug("窗口位置已调整到主屏幕中心");
                    return true;
                }
                return false;
            }

            // 如果窗口部分在屏幕外，调整到最佳屏幕内
            if (bestScreen.HasValue)
            {
                var workingArea = bestScreen.Value;

                // 确保窗口至少有一部分在可见区域内
                if (x < workingArea.X)
                    x = workingArea.X;
                if (y < workingArea.Y)
                    y = workingArea.Y;
                if (x + width > workingArea.Right)
                    x = workingArea.Right - width;
                if (y + height > workingArea.Bottom)
                    y = workingArea.Bottom - height;

                // 确保窗口不会太小（至少有一部分可见）
                if (x + width <= workingArea.X || y + height <= workingArea.Y)
                {
                    // 窗口太小，调整到屏幕中心
                    x = workingArea.X + (workingArea.Width - width) / 2;
                    y = workingArea.Y + (workingArea.Height - height) / 2;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "验证窗口位置时发生错误");
            return false;
        }
    }
}