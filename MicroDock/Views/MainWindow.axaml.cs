using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using DesktopNotifications;
using FluentAvalonia.UI.Controls;
using MicroDock.Database;
using MicroDock.Infrastructure;
using MicroDock.Models;
using MicroDock.Plugin;
using MicroDock.Services;
using MicroDock.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MicroDock.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 订阅事件消息
            SubscribeToMessages();

            // 在窗口打开后初始化设置
            this.Opened += OnWindowOpened;
            
            // 监听窗口大小变化
            this.SizeChanged += OnWindowSizeChanged;
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

            // 初始化NavigationView菜单项
            InitializeNavigationItems();
        }
        
        /// <summary>
        /// 初始化窗口通知管理器
        /// </summary>
        private void InitializeWindowNotificationManager()
        {
            Program.WindowNotificationManager = new WindowNotificationManager(this)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 3
            };
            
            Log.Debug("WindowNotificationManager 已初始化");
        }

        /// <summary>
        /// 窗口大小变化事件处理
        /// </summary>
        private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // 获取NavigationView控件
            var navView = this.FindControl<NavigationView>("MainNav");
            if (navView == null)
                return;

            // 计算NavigationView宽度：窗口宽度的20%，但限制在128-256之间
            double targetWidth = Math.Clamp(e.NewSize.Width * 0.2, 128, 256);
            navView.OpenPaneLength = targetWidth;
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
            navView.FooterMenuItems.Clear();
            // 添加导航项
            foreach (var navItem in viewModel.NavigationItems)
            {
                AddNavigationMenuItem(navView, navItem);
            }

            // 设置选中项
            if (viewModel.NavigationItems.Count > 0)
            {
                navView.SelectedItem = navView.MenuItems[0];
            }

            // 订阅选中项变更事件
            navView.SelectionChanged += OnNavigationSelectionChanged;

            // 监听 NavigationItems 集合的变化
            viewModel.NavigationItems.CollectionChanged += OnNavigationItemsCollectionChanged;
            
            // 初始化NavigationView宽度
            double targetWidth = Math.Clamp(this.Width * 0.2, 128, 256);
            navView.OpenPaneLength = targetWidth;
        }

        /// <summary>
        /// 添加导航菜单项
        /// </summary>
        private void AddNavigationMenuItem(NavigationView navView, NavigationItemModel navItem)
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
                    if (Enum.TryParse<Symbol>(navItem.Icon, out var symbol))
                    {
                        menuItem.IconSource = new SymbolIconSource { Symbol = symbol };
                    }
                }
                catch
                {
                    // 图标设置失败，忽略
                }
            }

            // 设置ToolTip显示完整标题
            ToolTip.SetTip(menuItem, navItem.Title);
            ToolTip.SetShowDelay(menuItem, 500); // 悬停500ms后显示

            if (navItem.NavType == NavigationType.Settings)
            {
                navView.FooterMenuItems.Add(menuItem);
            }
            else
            {
                navView.MenuItems.Add(menuItem);
            }
        }

        /// <summary>
        /// 处理 NavigationItems 集合变化
        /// </summary>
        private void OnNavigationItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel viewModel)
                return;

            var navView = this.FindControl<NavigationView>("MainNav");
            if (navView == null)
                return;

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                // 添加新项
                foreach (NavigationItemModel newItem in e.NewItems)
                {
                    AddNavigationMenuItem(navView, newItem);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                // 移除项
                foreach (NavigationItemModel oldItem in e.OldItems)
                {
                    IList<object>? items = default;
                    if (oldItem.NavType == NavigationType.Settings)
                    {
                        items = navView.FooterMenuItems;
                    }
                    else
                    {
                        items = navView.MenuItems;
                    }
                    var menuItemToRemove = items
                            .OfType<NavigationViewItem>()
                            .FirstOrDefault(item => item.Tag == oldItem);

                    if (menuItemToRemove != null)
                    {
                        items.Remove(menuItemToRemove);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // 重置，清空并重新加载
                navView.MenuItems.Clear();
                foreach (var navItem in viewModel.NavigationItems)
                {
                    AddNavigationMenuItem(navView, navItem);
                }
            }
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

            EventAggregator.Instance.Publish(new ServiceStateChangedMessage("AutoHide", message.Enable));
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

            EventAggregator.Instance.Publish(new ServiceStateChangedMessage("AutoStartup", message.Enable));
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
            Log.Debug("通知已关闭: {Reason}", e.Reason);
        }

        private void OnNotificationActivated(object? sender, NotificationActivatedEventArgs e)
        {
            Log.Information("通知已激活: {ActionId}", e.ActionId);
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

            // 统一释放所有服务资源
            try
            {
                Log.Information("MainWindow 正在释放服务资源...");
                ServiceLocator.GetService<AutoHideService>()?.Dispose();
                ServiceLocator.GetService<TopMostService>()?.Dispose();
                // AutoStartupService不需要Dispose，因为它只操作注册表

                // 释放ViewModel（包括插件加载器）
                if (this.DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.Dispose();
                }

                Log.Information("MainWindow 所有服务资源已成功释放");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MainWindow 释放资源时发生错误");
            }
        }
    }
}