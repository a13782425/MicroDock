using Avalonia.Controls;
using MicroDock.Infrastructure;
using MicroDock.Models;
using MicroDock.Plugin;
using MicroDock.Services;
using MicroDock.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MicroDock.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private NavigationItemModel? _selectedNavItem;
        private object? _currentView;
        private bool _disposed = false;

        public MainWindowViewModel()
        {
            // 初始化导航项集合
            NavigationItems = new ObservableCollection<NavigationItemModel>();

            // 订阅事件消息
            EventAggregator.Instance.Subscribe<NavigateToTabMessage>(OnNavigateToTab);
            EventAggregator.Instance.Subscribe<AddCustomTabRequestMessage>(OnAddCustomTabRequest);
            EventAggregator.Instance.Subscribe<LogViewerVisibilityChangedMessage>(OnLogViewerVisibilityChanged);

            // 初始化NavigationView相关
            InitializeNavigationItems();
        }

        /// <summary>
        /// 初始化导航项
        /// </summary>
        private void InitializeNavigationItems()
        {
            // 添加"应用"导航项
            var appNavItem = new NavigationItemModel
            {
                Title = "应用",
                Icon = "Home",//PreviewLink,ViewAll
                Content = new ApplicationTabView(),
                NavType = NavigationType.Application
            };
            NavigationItems.Add(appNavItem);

            // 加载插件导航项
            LoadPluginNavigationItems();

            // 根据设置添加日志查看器标签页
            var settings = Database.DBContext.GetSetting();
            if (settings.ShowLogViewer)
            {
                var logNavItem = new NavigationItemModel
                {
                    Title = "日志",
                    Icon = "Document",
                    Content = new LogViewerTabView(),
                    NavType = NavigationType.Settings,
                    UseParentScrollViewer = false // 日志查看器自己管理滚动
                };
                NavigationItems.Add(logNavItem);
            }

            // 创建设置导航项（单独管理，不加入NavigationItems）
            SettingsNavItem = new NavigationItemModel
            {
                Title = "设置",
                Icon = "Setting",
                Content = new SettingsTabView(),
                NavType = NavigationType.Settings
            };

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
        /// 处理添加自定义标签页请求
        /// </summary>
        private void OnAddCustomTabRequest(AddCustomTabRequestMessage message)
        {
            // TODO: 实现添加自定义导航项的功能
            // 暂时不支持动态添加导航项
        }

        /// <summary>
        /// 处理日志查看器可见性变更消息
        /// </summary>
        private void OnLogViewerVisibilityChanged(LogViewerVisibilityChangedMessage message)
        {
            if (message.IsVisible)
            {
                // 添加日志标签页（如果不存在）
                var existingLog = NavigationItems.FirstOrDefault(n => n.Title == "日志");
                if (existingLog == null)
                {
                    var logNavItem = new NavigationItemModel
                    {
                        Title = "日志",
                        Icon = "Document",
                        Content = new LogViewerTabView(),
                        NavType = NavigationType.Settings,
                        UseParentScrollViewer = false // 日志查看器自己管理滚动
                    };
                    // 在设置项之前插入（即最后一个位置）
                    NavigationItems.Add(logNavItem);
                }
            }
            else
            {
                // 移除日志标签页
                var logItem = NavigationItems.FirstOrDefault(n => n.Title == "日志");
                if (logItem != null)
                {
                    // 如果当前选中的是日志页，先切换到其他页
                    if (SelectedNavItem == logItem)
                    {
                        SelectedNavItem = NavigationItems.FirstOrDefault();
                    }
                    NavigationItems.Remove(logItem);
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
            // 获取插件目录路径（在应用程序目录下的Plugins文件夹）
            string appDirectory = System.AppContext.BaseDirectory;
            string pluginDirectory = Path.Combine(appDirectory, "Plugins");

            // 加载所有插件
            List<PluginInfo> plugins = Infrastructure.ServiceLocator.Get<PluginLoader>().LoadPlugins(pluginDirectory);

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
                        finally
                        {
                            iconSymbol = IconSymbolEnum.Library;
                        }

                        NavigationItemModel pluginNavItem = new NavigationItemModel
                        {
                            Title = tab.TabName,
                            Icon = iconSymbol.ToString(), // 插件使用Library图标
                            Content = tabControl,
                            NavType = NavigationType.Plugin
                        };
                        NavigationItems.Add(pluginNavItem);
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

            Infrastructure.ServiceLocator.GetService<PluginLoader>()?.Dispose();
            _disposed = true;
        }
    }
}
