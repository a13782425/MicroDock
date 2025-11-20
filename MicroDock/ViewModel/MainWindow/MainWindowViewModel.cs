using Avalonia.Controls;
using MicroDock.Model;
using MicroDock.Plugin;
using MicroDock.Service;
using MicroDock.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MicroDock.ViewModel;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private NavigationItemModel? _selectedNavItem;
    private object? _currentView;
    private bool _disposed = false;
    private bool _isLoading = false;
    private string? _loadingMessage = null;

    public MainWindowViewModel()
    {
        // 初始化导航项集合
        NavigationItems = new ObservableCollection<NavigationItemModel>();

        // 订阅事件消息
        ServiceLocator.Get<EventService>().Subscribe<NavigateToTabMessage>(OnNavigateToTab);
        ServiceLocator.Get<EventService>().Subscribe<NavigationTabVisibilityChangedMessage>(OnLogViewerVisibilityChanged);
        ServiceLocator.Get<EventService>().Subscribe<ShowLoadingMessage>(OnShowLoading);
        ServiceLocator.Get<EventService>().Subscribe<HideLoadingMessage>(OnHideLoading);
        ServiceLocator.Get<EventService>().Subscribe<PluginStateChangedMessage>(OnPluginStateChanged);
        ServiceLocator.Get<EventService>().Subscribe<PluginDeletedMessage>(OnPluginDeleted);
        ServiceLocator.Get<EventService>().Subscribe<PluginImportedMessage>(OnPluginImported);
        ServiceLocator.Get<EventService>().Subscribe<NavigationTabsConfigurationChangedMessage>(OnNavigationTabsConfigurationChanged);

        // 初始化NavigationView相关
        InitializeNavigationItems();
    }

    /// <summary>
    /// 处理导航页签配置变更
    /// </summary>
    private void OnNavigationTabsConfigurationChanged(NavigationTabsConfigurationChangedMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // 1. 获取最新配置
            var dbTabs = Database.DBContext.GetAllNavigationTabs().ToDictionary(t => t.Id);

            // 2. 处理显隐 (移除不可见的)
            var itemsToRemove = new List<NavigationItemModel>();
            foreach (var item in NavigationItems)
            {
                if (dbTabs.TryGetValue(item.UniqueId, out var tab))
                {
                    item.IsVisible = tab.IsVisible;

                }
            }



            // 3. 处理显隐 (添加可见但缺失的 - 简单起见，重新加载插件项和系统项)
            // 注意：这里简单的重新加载可能会导致状态丢失，但对于设置更改是可接受的
            // 为了避免重复，LoadPluginNavigationItems 需要做去重检查

            // 重新检查系统项
            //CheckAndAddSystemTab("microdock:ApplicationTabView", "应用", "Home", new ApplicationTabView(), 0);

            //var settings = Database.DBContext.GetSetting();
            //if (settings.ShowLogViewer)
            //{
            //    //CheckAndAddSystemTab("microdock:LogViewerTabView", "日志", "Document", new LogViewerTabView(), 998);
            //}

            //LoadPluginNavigationItems(); // LoadPluginNavigationItems 已经包含去重检查

            //// 4. 排序
            //var sortedItems = NavigationItems.OrderBy(n =>
            //    dbTabs.ContainsKey(n.UniqueId) ? dbTabs[n.UniqueId].OrderIndex : 9999)
            //    .ToList();

            //for (int i = 0; i < sortedItems.Count; i++)
            //{
            //    int oldIndex = NavigationItems.IndexOf(sortedItems[i]);
            //    if (oldIndex != i)
            //    {
            //        NavigationItems.Move(oldIndex, i);
            //    }
            //}
        });
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
            Icon = "Home",//PreviewLink,ViewAll
            UniqueId = NAVIGATION_APP_ID,
            Content = new ApplicationTabView(),
            NavType = NavigationType.Application
        };
        NavigationItems.Add(appNavItem);

        // 加载插件导航项
        LoadPluginNavigationItems();

        // 根据设置添加日志查看器标签页 (系统项)
        var settings = Database.DBContext.GetSetting();
        var logNavItem = new NavigationItemModel(null)
        {
            Title = "日志",
            Icon = "Document",
            Content = new LogViewerTabView(),
            UniqueId = NAVIGATION_LOG_ID,
            NavType = NavigationType.Settings,
            IsVisible = settings.ShowLogViewer
        };
        NavigationItems.Add(logNavItem);
        // 创建设置导航项（单独管理，不加入NavigationItems）
        SettingsNavItem = new NavigationItemModel(null)
        {
            Title = "设置",
            Icon = "Setting",
            Content = new SettingsTabView(),
            UniqueId = NAVIGATION_SETTING_ID,
            NavType = NavigationType.Settings
        };
        NavigationItems.Add(SettingsNavItem);

        // 默认选中第一个导航项
        SelectedNavItem = NavigationItems.FirstOrDefault();
    }

    /// <summary>
    /// 处理导航到标签页消息（已迁移到NavigationView）
    /// </summary>
    private void OnNavigateToTab(NavigateToTabMessage message)
    {
        // 通过名称查找并选中导航项
        if (!string.IsNullOrEmpty(message.TabName))
        {
            var navItem = NavigationItems.FirstOrDefault(n => n.Title == message.TabName);
            if (navItem != null)
            {
                SelectedNavItem = navItem;
            }
            else if (message.TabName == "设置")
            {
                SelectedNavItem = SettingsNavItem;
            }
        }
        // 通过索引查找（仅支持非设置项）
        else if (message.TabIndex.HasValue)
        {
            if (message.TabIndex.Value >= 0 && message.TabIndex.Value < NavigationItems.Count)
            {
                SelectedNavItem = NavigationItems[message.TabIndex.Value];
            }
        }
    }

    /// <summary>
    /// 处理日志查看器可见性变更消息
    /// </summary>
    private void OnLogViewerVisibilityChanged(NavigationTabVisibilityChangedMessage message)
    {
        var logItem = NavigationItems.FirstOrDefault(n => n.Title == "日志");
        if (logItem != null)
            logItem.IsVisible = message.IsVisible;
        //if (message.IsVisible)
        //{
        //    // 添加日志标签页（如果不存在）
        //    var existingLog = NavigationItems.FirstOrDefault(n => n.Title == "日志");
        //    if (existingLog == null)
        //    {
        //        var logNavItem = new NavigationItemModel(null)
        //        {
        //            Title = "日志",
        //            Icon = "Document",
        //            Content = new LogViewerTabView(),
        //            NavType = NavigationType.Settings,
        //        };
        //        //SetupNavigationItem(logNavItem, "microdock:LogViewerTabView", 998);

        //        // Insert based on OrderIndex? Or just add.
        //        // For simplicity, add to end (before settings if settings was in list, but settings is separate)
        //        NavigationItems.Add(logNavItem);
        //    }
        //}
        //else
        //{
        //    // 移除日志标签页
            
        //    if (logItem != null)
        //    {
        //        // 如果当前选中的是日志页，先切换到其他页
        //        if (SelectedNavItem == logItem)
        //        {
        //            SelectedNavItem = NavigationItems.FirstOrDefault();
        //        }
        //        NavigationItems.Remove(logItem);
        //    }
        //}
    }

    /// <summary>
    /// 处理显示Loading消息
    /// </summary>
    private void OnShowLoading(ShowLoadingMessage message)
    {
        // 需要在UI线程上更新
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            LoadingMessage = message.Message;
            IsLoading = true;
        });
    }

    /// <summary>
    /// 处理隐藏Loading消息
    /// </summary>
    private void OnHideLoading(HideLoadingMessage message)
    {
        // 需要在UI线程上更新
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsLoading = false;
            LoadingMessage = null;
        });
    }

    /// <summary>
    /// 处理插件状态变更消息
    /// </summary>
    private void OnPluginStateChanged(PluginStateChangedMessage message)
    {
        Serilog.Log.Information("收到插件状态变更消息: {PluginName}, IsEnabled: {IsEnabled}", message.PluginName, message.IsEnabled);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (message.IsEnabled)
            {
                // 插件被启用，重新加载该插件的导航项
                Serilog.Log.Information("重新加载插件导航项: {PluginName}", message.PluginName);
                ReloadPluginNavigationItems(message.PluginName);
            }
            else
            {
                // 插件被禁用，移除该插件的所有导航项
                Serilog.Log.Information("移除插件导航项: {PluginName}", message.PluginName);
                RemovePluginNavigationItems(message.PluginName);
            }
        });
    }

    /// <summary>
    /// 处理插件删除消息
    /// </summary>
    private void OnPluginDeleted(PluginDeletedMessage message)
    {
        Serilog.Log.Information("收到插件删除消息: {PluginName}", message.PluginName);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // 移除该插件的所有导航项
            Serilog.Log.Information("从导航栏移除插件: {PluginName}", message.PluginName);
            RemovePluginNavigationItems(message.PluginName);
        });
    }

    /// <summary>
    /// 处理插件导入消息
    /// </summary>
    private void OnPluginImported(PluginImportedMessage message)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // 重新加载该插件的导航项
            ReloadPluginNavigationItems(message.PluginName);
        });
    }

    /// <summary>
    /// 移除指定插件的所有导航项
    /// </summary>
    private void RemovePluginNavigationItems(string pluginUniqueName)
    {
        // 查找并移除该插件的所有导航项
        var itemsToRemove = NavigationItems
            .Where(n => n.NavType == NavigationType.Plugin && n.PluginUniqueName == pluginUniqueName)
            .ToList();

        Serilog.Log.Information("找到 {Count} 个导航项需要移除，插件: {PluginName}", itemsToRemove.Count, pluginUniqueName);

        foreach (var item in itemsToRemove)
        {
            // 如果当前选中的是要移除的导航项，先切换到第一个导航项
            if (SelectedNavItem == item)
            {
                SelectedNavItem = NavigationItems.FirstOrDefault(n => n != item);
                Serilog.Log.Information("当前选中的导航项被移除，切换到: {NewTitle}", SelectedNavItem?.Title ?? "无");
            }
            NavigationItems.Remove(item);
            Serilog.Log.Information("已移除导航项: {Title}", item.Title);
        }
    }

    /// <summary>
    /// 重新加载指定插件的导航项
    /// </summary>
    private void ReloadPluginNavigationItems(string pluginUniqueName)
    {
        // 先移除旧的导航项
        RemovePluginNavigationItems(pluginUniqueName);

        // 从 PluginLoader 获取插件信息
        var pluginLoader = ServiceLocator.Get<PluginService>();
        var pluginInfo = pluginLoader.LoadedPlugins
            .FirstOrDefault(p => p.UniqueName == pluginUniqueName);

        if (pluginInfo?.PluginInstance?.Tabs == null)
        {
            return;
        }

        // 为该插件的每个标签页创建导航项
        foreach (IMicroTab tab in pluginInfo.PluginInstance.Tabs)
        {
            if (tab is Control tabControl)
            {
                IconSymbolEnum iconSymbol = IconSymbolEnum.Library;
                try
                {
                    iconSymbol = tab.IconSymbol;
                }
                catch (Exception _)
                {
                    iconSymbol = IconSymbolEnum.Library;
                }

                string uniqueId = $"{pluginInfo.UniqueName}:{tabControl.GetType().Name}";
                var config = Database.DBContext.GetNavigationTab(uniqueId);
                NavigationItemModel pluginNavItem = new NavigationItemModel(config.GetDto() as NavigationTabDto)
                {
                    Title = tab.TabName,
                    Icon = iconSymbol.ToString(),
                    Content = tabControl,
                    NavType = NavigationType.Plugin,
                    PluginUniqueName = pluginInfo.UniqueName
                };


                if (Database.DBContext.GetNavigationTab(uniqueId)?.IsVisible ?? true)
                {
                    // 插入到日志项之前（如果有日志项的话），否则添加到末尾
                    var logItemIndex = NavigationItems.ToList().FindIndex(n => n.Title == "日志");
                    if (logItemIndex >= 0)
                    {
                        NavigationItems.Insert(logItemIndex, pluginNavItem);
                    }
                    else
                    {
                        NavigationItems.Add(pluginNavItem);
                    }
                }
            }
        }
    }

    #region NavigationView相关属性

    /// <summary>
    /// 导航项集合（不包含设置）
    /// </summary>
    public ObservableCollection<NavigationItemModel> NavigationItems { get; }

    /// <summary>
    /// 设置导航项（单独管理，固定在底部）
    /// </summary>
    public NavigationItemModel SettingsNavItem { get; private set; } = null!;

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
    /// 当前显示的视图内容
    /// </summary>
    public object? CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// 加载消息
    /// </summary>
    public string? LoadingMessage
    {
        get => _loadingMessage;
        set => this.RaiseAndSetIfChanged(ref _loadingMessage, value);
    }

    /// <summary>
    /// 更新当前视图
    /// </summary>
    private void UpdateCurrentView()
    {
        if (SelectedNavItem != null)
        {
            CurrentView = SelectedNavItem.Content;
        }
    }

    #endregion

    /// <summary>
    /// 加载插件导航项（新）
    /// </summary>
    private void LoadPluginNavigationItems()
    {
        // 加载所有插件
        List<PluginInfo> plugins = ServiceLocator.Get<PluginService>().LoadedPlugins.ToList();

        // 为每个插件的每个标签页创建导航项
        foreach (PluginInfo pluginInfo in plugins)
        {
            if (pluginInfo.PluginInstance?.Tabs == null)
            {
                continue;
            }

            foreach (IMicroTab tab in pluginInfo.PluginInstance.Tabs)
            {
                if (tab is Control tabControl)
                {
                    IconSymbolEnum iconSymbol = IconSymbolEnum.Library;
                    try
                    {
                        iconSymbol = tab.IconSymbol;
                    }
                    catch (Exception)
                    {
                        iconSymbol = IconSymbolEnum.Library;
                    }

                    string uniqueId = $"{pluginInfo.UniqueName}:{tabControl.GetType().Name.ToLower()}";
                    var config = Database.DBContext.GetNavigationTab(uniqueId);
                    // 防止重复添加
                    NavigationItemModel? pluginNavItem = NavigationItems.FirstOrDefault(n => n.UniqueId == uniqueId);
                    if (pluginNavItem != null)
                    {
                        pluginNavItem.Order = config.OrderIndex;
                        pluginNavItem.IsEnabled = pluginInfo.IsEnabled;
                        pluginNavItem.IsVisible = config.IsVisible;
                        continue;
                    }
                    
                    pluginNavItem = new NavigationItemModel(config.GetDto() as NavigationTabDto)
                    {
                        Title = tab.TabName,
                        Icon = iconSymbol.ToString(), // 插件使用Library图标
                        Content = tabControl,
                        NavType = NavigationType.Plugin,
                        Order = config.OrderIndex,
                        IsVisible = config.IsVisible,
                        IsEnabled = pluginInfo.IsEnabled,
                        UniqueId = uniqueId,
                        PluginUniqueName = pluginInfo.UniqueName
                    };
                    NavigationItems.Add(pluginNavItem);
                    LogService.Debug($"添加导航项: {tab.TabName} (插件: {pluginInfo.UniqueName})", LogService.DEFAULT_TAG);
                }
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
    }
}
