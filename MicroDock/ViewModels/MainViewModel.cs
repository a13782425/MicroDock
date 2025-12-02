using Avalonia.Controls;
using MicroDock.Extension;
using MicroDock.Model;
using MicroDock.Plugin;
using MicroDock.Service;
using MicroDock.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MicroDock.ViewModels;

/// <summary>
/// MainView的ViewModel,负责导航和内容管理
/// </summary>
public class MainViewModel : ViewModelBase, IDisposable
{
    private NavigationItemModel? _selectedNavItem;
    private object? _currentView;
    private bool _disposed = false;
    private LockScreenViewModel? _lockScreenViewModel;

    public MainViewModel()
    {
        // 初始化导航项集合
        NavigationItems = new ObservableCollection<NavigationItemModel>();

        // 订阅事件消息
        ServiceLocator.Get<EventService>().Subscribe<NavigateToTabMessage>(OnNavigateToTab);
        ServiceLocator.Get<EventService>().Subscribe<NavigationTabVisibilityChangedMessage>(OnLogViewerVisibilityChanged);
        ServiceLocator.Get<EventService>().Subscribe<PluginStateChangedMessage>(OnPluginStateChanged);
        ServiceLocator.Get<EventService>().Subscribe<PluginDeletedMessage>(OnPluginDeleted);
        ServiceLocator.Get<EventService>().Subscribe<PluginImportedMessage>(OnPluginImported);
        ServiceLocator.Get<EventService>().Subscribe<NavigationTabsConfigurationChangedMessage>(OnNavigationTabsConfigurationChanged);
        
        // 订阅页签锁定相关消息
        ServiceLocator.Get<EventService>().Subscribe<TabLockedMessage>(OnTabLocked);
        ServiceLocator.Get<EventService>().Subscribe<TabUnlockedMessage>(OnTabUnlocked);

        // 初始化NavigationView相关
        InitializeNavigationItems();
    }

    /// <summary>
    /// 导航项集合
    /// </summary>
    public ObservableCollection<NavigationItemModel> NavigationItems { get; }

    /// <summary>
    /// 设置导航项(单独管理)
    /// </summary>
    public NavigationItemModel? SettingsNavItem { get; private set; }

    /// <summary>
    /// 当前选中的导航项
    /// </summary>
    public NavigationItemModel? SelectedNavItem
    {
        get => _selectedNavItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedNavItem, value);
            UpdateCurrentView();
        }
    }

    /// <summary>
    /// 当前显示的视图
    /// </summary>
    public object? CurrentView
    {
        get => _currentView;
        private set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    /// <summary>
    /// 初始化导航项
    /// </summary>
    private void InitializeNavigationItems()
    {
        // 添加"应用"导航项
        var appNavItem = new NavigationItemModel(null)
        {
            Title = "应用",
            Icon = "Home",
            UniqueId = NAVIGATION_APP_ID,
            Content = new ApplicationTabView(),
            NavType = NavigationType.Application
        };
        NavigationItems.Add(appNavItem);

        // 加载插件导航项
        LoadPluginNavigationItems();

        // 根据设置添加日志查看器标签页
        var settings = Database.DBContext.GetSetting();
        var logNavItem = new NavigationItemModel(null)
        {
            Title = "日志",
            Icon = "Document",
            Content = new LogViewerTabView(),
            UniqueId = NAVIGATION_LOG_ID,
            NavType = NavigationType.System,
            IsVisible = settings.ShowLogViewer
        };
        NavigationItems.Add(logNavItem);

        // 创建设置导航项
        SettingsNavItem = new NavigationItemModel(null)
        {
            Title = "设置",
            Icon = "Setting",
            Content = new SettingsTabView(),
            UniqueId = NAVIGATION_SETTING_ID,
            NavType = NavigationType.Settings
        };
        NavigationItems.Add(SettingsNavItem);

        SortNavItems();
        // 默认选中第一个导航项
        SelectedNavItem = NavigationItems.FirstOrDefault();

    }

    private void SortNavItems()
    {
        NavigationItems.Sort((a, b) =>
        {
            int typeComp = a.NavType.CompareTo(b.NavType);
            return typeComp != 0 ? typeComp : a.Order.CompareTo(b.Order);
        });
    }

    /// <summary>
    /// 加载插件导航项
    /// </summary>
    private void LoadPluginNavigationItems()
    {
        var plugins = ServiceLocator.Get<PluginService>().LoadedPlugins.ToList();

        foreach (PluginInfo pluginInfo in plugins)
        {
            if (pluginInfo.PluginInstance?.Tabs == null)
                continue;

            foreach (IMicroTab tab in pluginInfo.PluginInstance.Tabs)
            {
                if (tab is Control tabControl)
                {
                    string uniqueId = pluginInfo.GetTabUniqueId(tab);

                    // 检查是否已存在
                    if (NavigationItems.Any(n => n.UniqueId == uniqueId))
                        continue;

                    var config = Database.DBContext.GetNavigationTab(uniqueId) ?? new Database.NavigationTabDB
                    {
                        Id = uniqueId,
                        OrderIndex = 50,
                        IsVisible = true
                    };

                    var pluginNavItem = new NavigationItemModel(config.GetDto() as NavigationTabDto)
                    {
                        Title = tab.TabName,
                        Icon = tab.IconSymbol.ToString(),
                        Content = tabControl,
                        NavType = NavigationType.Plugin,
                        Order = config.OrderIndex,
                        IsVisible = config.IsVisible,
                        IsEnabled = pluginInfo.IsEnabled,
                        UniqueId = uniqueId,
                        PluginUniqueName = pluginInfo.UniqueName
                    };
                    NavigationItems.Add(pluginNavItem);
                }
            }
        }
    }

    /// <summary>
    /// 重新加载指定插件的导航项
    /// </summary>
    private void ReloadPluginNavigationItems(string pluginUniqueName)
    {
        // 先移除旧的导航项
        RemovePluginNavigationItems(pluginUniqueName);

        // 从 PluginService 获取插件信息
        var pluginService = ServiceLocator.Get<PluginService>();
        var pluginInfo = pluginService.LoadedPlugins.FirstOrDefault(p => p.UniqueName == pluginUniqueName);

        if (pluginInfo?.PluginInstance?.Tabs == null)
            return;

        // 为该插件的每个标签页创建导航项
        foreach (IMicroTab tab in pluginInfo.PluginInstance.Tabs)
        {
            if (tab is Control tabControl)
            {
                string uniqueId = $"plugin:{pluginInfo.UniqueName}:{tab.TabName}";
                var config = Database.DBContext.GetNavigationTab(uniqueId) ?? new Database.NavigationTabDB
                {
                    Id = uniqueId,
                    OrderIndex = 50,
                    IsVisible = true
                };

                var pluginNavItem = new NavigationItemModel(config.GetDto() as NavigationTabDto)
                {
                    Title = tab.TabName,
                    Icon = tab.IconSymbol.ToString(),
                    Content = tabControl,
                    NavType = NavigationType.Plugin,
                    IsEnabled = pluginInfo.IsEnabled,
                    UniqueId = uniqueId,
                    PluginUniqueName = pluginInfo.UniqueName
                };

                if (config.IsVisible)
                {
                    // 插入到日志项之前
                    var logItemIndex = NavigationItems.ToList().FindIndex(n => n.Title == "日志");
                    if (logItemIndex >= 0)
                        NavigationItems.Insert(logItemIndex, pluginNavItem);
                    else
                        NavigationItems.Add(pluginNavItem);
                }
            }
        }
    }

    /// <summary>
    /// 移除指定插件的所有导航项
    /// </summary>
    private void RemovePluginNavigationItems(string pluginUniqueName)
    {
        var itemsToRemove = NavigationItems
            .Where(n => n.NavType == NavigationType.Plugin && n.PluginUniqueName == pluginUniqueName)
            .ToList();

        foreach (var item in itemsToRemove)
        {
            if (SelectedNavItem == item)
                SelectedNavItem = NavigationItems.FirstOrDefault(n => n != item);

            NavigationItems.Remove(item);
        }
    }

    /// <summary>
    /// 更新当前视图
    /// </summary>
    private void UpdateCurrentView()
    {
        if (SelectedNavItem == null)
            return;

        // 检查页签是否需要解锁
        if (SelectedNavItem.NeedsUnlock)
        {
            // 显示锁屏界面
            ShowLockScreen(SelectedNavItem);
        }
        else
        {
            // 正常显示内容
            CurrentView = SelectedNavItem.Content;
            
            // 如果页签已解锁，刷新解锁时间
            if (SelectedNavItem.IsLocked && SelectedNavItem.IsUnlocked)
            {
                ServiceLocator.Get<TabLockService>()?.RefreshUnlockTime(SelectedNavItem.UniqueId);
            }
        }
    }

    /// <summary>
    /// 显示锁屏界面
    /// </summary>
    private void ShowLockScreen(NavigationItemModel navItem)
    {
        _lockScreenViewModel = new LockScreenViewModel(navItem.UniqueId, navItem.Title);
        _lockScreenViewModel.UnlockSucceeded += OnUnlockSucceeded;
        CurrentView = new LockScreenView { DataContext = _lockScreenViewModel };
    }

    /// <summary>
    /// 解锁成功回调
    /// </summary>
    private void OnUnlockSucceeded(object? sender, string tabId)
    {
        if (_lockScreenViewModel != null)
        {
            _lockScreenViewModel.UnlockSucceeded -= OnUnlockSucceeded;
            _lockScreenViewModel = null;
        }

        // 更新视图显示真实内容
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (SelectedNavItem?.UniqueId == tabId)
            {
                CurrentView = SelectedNavItem.Content;
            }
        });
    }

    #region 事件处理

    private void OnNavigateToTab(NavigateToTabMessage message)
    {
        if (!string.IsNullOrEmpty(message.TabName))
        {
            var navItem = NavigationItems.FirstOrDefault(n => n.Title == message.TabName);
            if (navItem != null)
                SelectedNavItem = navItem;
            else if (message.TabName == "设置")
                SelectedNavItem = SettingsNavItem;
        }
        else if (message.TabIndex.HasValue)
        {
            if (message.TabIndex.Value >= 0 && message.TabIndex.Value < NavigationItems.Count)
                SelectedNavItem = NavigationItems[message.TabIndex.Value];
        }
    }

    private void OnLogViewerVisibilityChanged(NavigationTabVisibilityChangedMessage message)
    {
        var logItem = NavigationItems.FirstOrDefault(n => n.Title == "日志");
        if (logItem != null)
            logItem.IsVisible = message.IsVisible;
    }

    private void OnPluginStateChanged(PluginStateChangedMessage message)
    {
        Serilog.Log.Information("收到插件状态变更消息: {PluginName}, IsEnabled: {IsEnabled}", message.PluginName, message.IsEnabled);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (message.IsEnabled)
                ReloadPluginNavigationItems(message.PluginName);
            else
                RemovePluginNavigationItems(message.PluginName);
        });
    }

    private void OnPluginDeleted(PluginDeletedMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => RemovePluginNavigationItems(message.PluginName));
    }

    private void OnPluginImported(PluginImportedMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => ReloadPluginNavigationItems(message.PluginName));
    }

    private void OnNavigationTabsConfigurationChanged(NavigationTabsConfigurationChangedMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var dbTabs = Database.DBContext.GetAllNavigationTabs().ToDictionary(t => t.Id);

            foreach (var item in NavigationItems)
            {
                if (dbTabs.TryGetValue(item.UniqueId, out var tab))
                    item.IsVisible = tab.IsVisible;
            }
            SortNavItems();
        });
    }

    /// <summary>
    /// 处理页签加锁消息
    /// </summary>
    private void OnTabLocked(TabLockedMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // 如果当前选中的页签被加锁，显示锁屏
            if (SelectedNavItem?.UniqueId == message.TabId && SelectedNavItem.IsLocked)
            {
                ShowLockScreen(SelectedNavItem);
            }
        });
    }

    /// <summary>
    /// 处理页签解锁消息
    /// </summary>
    private void OnTabUnlocked(TabUnlockedMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // 如果当前选中的页签被解锁，显示内容
            if (SelectedNavItem?.UniqueId == message.TabId)
            {
                CurrentView = SelectedNavItem.Content;
            }
        });
    }

    #endregion

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
    }
}
