using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using DesktopNotifications;
using FluentAvalonia.UI.Controls;
using MicroDock.Database;
using MicroDock.Models;
using MicroDock.Services;
using MicroDock.ViewModels;
using MicroDock.Infrastructure;
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
            _miniModeService = new MiniModeService();

            // 注册服务到ServiceLocator
            ServiceLocator.Instance.Register(_autoStartupService);
            ServiceLocator.Instance.Register<AutoHideService>(_autoHideService);
            ServiceLocator.Instance.Register<TopMostService>(_topMostService);
            ServiceLocator.Instance.Register<MiniModeService>(_miniModeService);

            InitializeTrayIcon();
            
            // 订阅事件消息
            SubscribeToMessages();
            
            // 在窗口打开后初始化设置
            this.Opened += OnWindowOpened;
        }
        
        /// <summary>
        /// 窗口打开事件处理
        /// </summary>
        private void OnWindowOpened(object? sender, EventArgs e)
        {
            // 从数据库加载设置并应用服务状态
            InitializeServicesFromSettings();
            
            // 初始化NavigationView菜单项
            InitializeNavigationItems();
        }
        
        /// <summary>
        /// 初始化NavigationView的菜单项
        /// </summary>
        private void InitializeNavigationItems()
        {
            if (DataContext is not MainWindowViewModel viewModel)
                return;
            
            // 获取NavigationView控件
            var navView = this.FindControl<NavigationView>("MainNav");
            if (navView == null)
                return;
            
            // 清空现有菜单项
            navView.MenuItems.Clear();
            
            // 添加导航项
            foreach (var navItem in viewModel.NavigationItems)
            {
                var menuItem = new NavigationViewItem
                {
                    Content = navItem.Title,
                    Tag = navItem
                };
                
                // 设置图标
                if (!string.IsNullOrEmpty(navItem.Icon))
                {
                    try
                    {
                        var symbolName = MapIconNameToSymbol(navItem.Icon);
                        if (!string.IsNullOrEmpty(symbolName) && 
                            Enum.TryParse<Symbol>(symbolName, out var symbol))
                        {
                            menuItem.IconSource = new SymbolIconSource { Symbol = symbol };
                        }
                    }
                    catch
                    {
                        // 图标设置失败，忽略
                    }
                }
                
                navView.MenuItems.Add(menuItem);
            }
            
            // 设置选中项
            if (viewModel.NavigationItems.Count > 0)
            {
                navView.SelectedItem = navView.MenuItems[0];
            }
            
            // 订阅选中项变更事件
            navView.SelectionChanged += OnNavigationSelectionChanged;
        }
        
        /// <summary>
        /// 将图标名称映射到FluentAvalonia的Symbol
        /// </summary>
        private string? MapIconNameToSymbol(string iconName)
        {
            return iconName switch
            {
                "Apps" => "Apps",
                "Library" => "Library",
                "Setting" => "Setting",
                "Document" => "Document",
                "Folder" => "Folder",
                "Code" => "Code",
                "Globe" => "Globe",
                "Edit" => "Edit",
                _ => null
            };
        }
        
        /// <summary>
        /// 导航项选中变更事件处理
        /// </summary>
        private void OnNavigationSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel viewModel)
                return;
                
            if (e.SelectedItem is NavigationViewItem item && item.Tag is NavigationItemModel navItem)
            {
                // 更新ViewModel的选中项
                viewModel.SelectedNavItem = navItem;
            }
            else if (e.SelectedItem is NavigationViewItem settingsItem && settingsItem.Tag as string == "设置")
            {
                // 选中了设置项
                viewModel.SelectedNavItem = viewModel.SettingsNavItem;
            }
        }
        
        /// <summary>
        /// 订阅事件消息
        /// </summary>
        private void SubscribeToMessages()
        {
            EventAggregator.Instance.Subscribe<WindowShowRequestMessage>(OnWindowShowRequest);
            EventAggregator.Instance.Subscribe<WindowHideRequestMessage>(OnWindowHideRequest);
            EventAggregator.Instance.Subscribe<MiniModeChangeRequestMessage>(OnMiniModeChangeRequest);
            EventAggregator.Instance.Subscribe<AutoHideChangeRequestMessage>(OnAutoHideChangeRequest);
            EventAggregator.Instance.Subscribe<AutoStartupChangeRequestMessage>(OnAutoStartupChangeRequest);
            EventAggregator.Instance.Subscribe<WindowTopmostChangeRequestMessage>(OnTopmostChangeRequest);
        }
        
        /// <summary>
        /// 处理窗口显示请求
        /// </summary>
        private void OnWindowShowRequest(WindowShowRequestMessage message)
        {
            if (message.WindowName == "MainWindow")
            {
                this.Show();
                this.Activate();
            }
        }
        
        /// <summary>
        /// 处理窗口隐藏请求
        /// </summary>
        private void OnWindowHideRequest(WindowHideRequestMessage message)
        {
            if (message.WindowName == "MainWindow")
            {
                this.Hide();
            }
        }
        
        /// <summary>
        /// 处理迷你模式变更请求
        /// </summary>
        private void OnMiniModeChangeRequest(MiniModeChangeRequestMessage message)
        {
            if (message.Enable)
            {
                _miniModeService.Enable();
            }
            else
            {
                _miniModeService.Disable();
            }
        }
        
        /// <summary>
        /// 处理自动隐藏变更请求
        /// </summary>
        private void OnAutoHideChangeRequest(AutoHideChangeRequestMessage message)
        {
            if (message.Enable)
            {
                _autoHideService.Enable();
            }
            else
            {
                _autoHideService.Disable();
            }
            
            EventAggregator.Instance.Publish(new ServiceStateChangedMessage("AutoHide", message.Enable));
        }
        
        /// <summary>
        /// 处理开机自启动变更请求
        /// </summary>
        private void OnAutoStartupChangeRequest(AutoStartupChangeRequestMessage message)
        {
            if (message.Enable)
            {
                _autoStartupService.Enable();
            }
            else
            {
                _autoStartupService.Disable();
            }
            
            EventAggregator.Instance.Publish(new ServiceStateChangedMessage("AutoStartup", message.Enable));
        }
        
        /// <summary>
        /// 处理置顶状态变更请求
        /// </summary>
        private void OnTopmostChangeRequest(WindowTopmostChangeRequestMessage message)
        {
            if (message.Enable)
            {
                // 切换置顶状态
                this.Topmost = !this.Topmost;
                _topMostService.Enable();
            }
            else
            {
                _topMostService.Disable();
            }
            
            EventAggregator.Instance.Publish(new ServiceStateChangedMessage("AlwaysOnTop", this.Topmost));
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
                _autoStartupService.Enable();
            }
            
            if (settings.AutoHide)
            {
                _autoHideService.Enable();
            }
            
            if (settings.AlwaysOnTop)
            {
                _topMostService.Enable();
            }
            
            if (settings.IsMiniModeEnabled)
            {
                _miniModeService.Enable();
            }
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