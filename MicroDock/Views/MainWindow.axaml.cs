using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using DesktopNotifications;
using FluentAvalonia.UI.Controls;
using MicroDock.Database;
using MicroDock.Service;
using MicroDock.ViewModel;
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
            // 恢复窗口位置和大小（需要在窗口显示前设置）
            RestoreWindowState();

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
                DataContext = navItem,
                Tag = navItem
            };
            menuItem.IsVisible = navItem.IsVisible;
            menuItem.Bind(Visual.IsVisibleProperty, new Binding(nameof(NavigationItemModel.IsVisible)));
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
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                // 移动项
                // 由于我们将 LogViewer 也改为了普通 Application 类型，
                // NavigationItems 现在应该与 navView.MenuItems 一一对应（忽略 SettingsNavItem，它不包含在 NavigationItems 中）
                
                // 使用 AvaloniaList 的 Move 方法
                // 注意：e.OldStartingIndex 和 e.NewStartingIndex 对应 NavigationItems 的索引
                
                if (e.OldStartingIndex >= 0 && e.OldStartingIndex < navView.MenuItems.Count &&
                    e.NewStartingIndex >= 0 && e.NewStartingIndex < navView.MenuItems.Count)
                {
                    // 如果 MenuItems 是 AvaloniaList，可以直接 Move
                    // FluentAvalonia 的 NavigationView.MenuItems 实际上是 IList，底层通常是 AvaloniaList<object>
                    // 我们尝试转换或者手动移除插入
                    
                    if (navView.MenuItems is Avalonia.Collections.AvaloniaList<object> avaloniaList)
                    {
                        avaloniaList.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else
                    {
                        // 通用 IList 处理
                        var item = navView.MenuItems[e.OldStartingIndex];
                        navView.MenuItems.RemoveAt(e.OldStartingIndex);
                        navView.MenuItems.Insert(e.NewStartingIndex, item);
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
            ServiceLocator.Get<EventService>().Subscribe<WindowShowRequestMessage>(OnWindowShowRequest);
            ServiceLocator.Get<EventService>().Subscribe<WindowHideRequestMessage>(OnWindowHideRequest);
            ServiceLocator.Get<EventService>().Subscribe<AutoHideChangeRequestMessage>(OnAutoHideChangeRequest);
            ServiceLocator.Get<EventService>().Subscribe<AutoStartupChangeRequestMessage>(OnAutoStartupChangeRequest);
            ServiceLocator.Get<EventService>().Subscribe<WindowTopmostChangeRequestMessage>(OnTopmostChangeRequest);
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
            SaveWindowState();
            this.Hide(); 
            Program.NotificationManager.ShowNotification(new DesktopNotifications.Notification() { Title = "MicroDock 已最小化到托盘", Body = "您可以通过系统托盘图标重新打开主窗口。" });
        }

        /// <summary>
        /// 窗口关闭事件 - 释放资源
        /// </summary>
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            // 保存窗口位置和大小（仅在窗口可见时保存，避免最小化时保存）
            try
            {
                if (this.IsVisible && this.WindowState == WindowState.Normal)
                {
                    SaveWindowState();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "保存窗口状态时发生错误");
            }

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
}