using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using DesktopNotifications;
using MicroDock.Database;
using MicroDock.Models;
using MicroDock.Services;
using MicroDock.ViewModels;
using System;
using System.ComponentModel;

namespace MicroDock.Views
{
    public partial class MainWindow : Window
    {
        private TrayIcon? _trayIcon;
        private readonly AutoStartupService _autoStartupService;
        private readonly AutoHideService _autoHideService;
        private readonly TopMostService _topMostService;
        private readonly MiniModeService _miniModeService;

        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化服务
            _autoStartupService = new AutoStartupService();
            _autoHideService = new AutoHideService(this);
            _topMostService = new TopMostService(this);
            _miniModeService = new MiniModeService(this);

            InitializeTrayIcon();
            
            // 在窗口打开后初始化设置
            this.Opened += OnWindowOpened;
        }
        
        /// <summary>
        /// 窗口打开事件处理
        /// </summary>
        private void OnWindowOpened(object? sender, EventArgs e)
        {
            InitializeSettings();
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

        /// <summary>
        /// 初始化设置并订阅配置变更事件
        /// </summary>
        private void InitializeSettings()
        {
            // 查找 SettingsTabView 并订阅其 ViewModel 的事件
            SettingsTabView? settingsTab = FindSettingsTabView();
            if (settingsTab?.ViewModel != null)
            {
                // 将服务实例传递给 ViewModel
                settingsTab.ViewModel.InitializeServices(_autoStartupService, _autoHideService, _topMostService, _miniModeService);
                // settingsTab.ViewModel.PropertyChanged += OnSettingChanged;
            }
        }

        /*private void OnSettingChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsTabViewModel.IsMiniModeEnabled))
            {
                if ((sender as SettingsTabViewModel).IsMiniModeEnabled)
                    _miniModeService.Enable();
                else
                    _miniModeService.Disable();
            }
        }*/
        
        /// <summary>
        /// 查找 SettingsTabView
        /// </summary>
        private SettingsTabView? FindSettingsTabView()
        {
            if (this.DataContext is MainWindowViewModel viewModel)
            {
                foreach (TabItemModel tab in viewModel.Tabs)
                {
                    if (tab.Content is SettingsTabView settingsView)
                    {
                        return settingsView;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 窗口关闭事件 - 释放资源
        /// </summary>
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            
            // 释放服务资源
            _autoHideService?.Dispose();
            
            System.Diagnostics.Debug.WriteLine("[MainWindow] 资源已释放");
        }
    }
}