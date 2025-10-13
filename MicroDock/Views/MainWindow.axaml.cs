using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using DesktopNotifications;
using System;

namespace MicroDock.Views
{
    public partial class MainWindow : Window
    {
        private TrayIcon? _trayIcon;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();
        }

        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
        private void InitializeTrayIcon()
        {
            Program.NotificationManager.NotificationActivated += OnNotificationActivated;
            Program.NotificationManager.NotificationDismissed += OnNotificationDismissed;
            _trayIcon = new TrayIcon();
            
            // 设置托盘图标（使用应用程序图标）
            _trayIcon.Icon = new WindowIcon(AssetLoader.Open(new System.Uri("avares://MicroDock/Assets/avalonia-logo.ico")));
            
            // 设置工具提示
            _trayIcon.ToolTipText = "MicroDock - 双击显示/隐藏";
            
            // 双击托盘图标显示/隐藏窗口
            _trayIcon.Clicked += (sender, args) =>
            {
                if (this.IsVisible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    this.Activate();
                }
            };

            // 创建右键菜单
            NativeMenu trayMenu = new NativeMenu();
            
            NativeMenuItem showMenuItem = new NativeMenuItem("显示");
            showMenuItem.Click += (sender, args) =>
            {
                //this.Show();
                //this.Activate();
                var nf = new DesktopNotifications.Notification
                {
                    Title = "通知标题",
                    Body = "这是一条系统托盘通知消息！",
                    // Icon = ... // 可选：设置通知图标
                    Buttons =
                    {
                        ("This is awesome!", "awesome")
                    }
                };
                Program.NotificationManager.ShowNotification(nf, DateTimeOffset.Now + TimeSpan.FromSeconds(5));
            };
            
            NativeMenuItem hideMenuItem = new NativeMenuItem("隐藏");
            hideMenuItem.Click += (sender, args) =>
            {
                this.Hide();
            };
            
            NativeMenuItem exitMenuItem = new NativeMenuItem("退出");
            exitMenuItem.Click += (sender, args) =>
            {
                this.Close();
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            };
            
            trayMenu.Items.Add(showMenuItem);
            trayMenu.Items.Add(hideMenuItem);
            trayMenu.Items.Add(new NativeMenuItemSeparator());
            trayMenu.Items.Add(exitMenuItem);
            
            _trayIcon.Menu = trayMenu;
            
            // 显示托盘图标
            _trayIcon.IsVisible = true;
        }
        private void OnNotificationDismissed(object? sender, NotificationDismissedEventArgs e)
        {
            Console.WriteLine($"Notification dismissed: {e.Reason}");
        }

        private void OnNotificationActivated(object? sender, NotificationActivatedEventArgs e)
        {
            Console.WriteLine($"Notification activated: {e.ActionId}");
        }
        /// <summary>
        /// 标题栏拖拽事件
        /// </summary>
        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // 确保是左键按下，且不是在按钮上
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                // 检查点击的是否是按钮
                object? source = e.Source;
                if (source is not Button)
                {
                    this.BeginMoveDrag(e);
                }
            }
        }

        /// <summary>
        /// 关闭按钮点击事件 - 最小化到托盘
        /// </summary>
        private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}