using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media;
using MicroDock.Database;
using MicroDock.Models;
using MicroDock.Services;
using MicroDock.Views;
using MicroDock.Infrastructure;
using ReactiveUI;

namespace MicroDock.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private int _selectedTabIndex;

        public MainWindowViewModel()
        {
            // 初始化页签集合
            Tabs = new ObservableCollection<TabItemModel>();

            // 添加固定的"应用"页签（不可关闭）
            TabItemModel applicationTab = new TabItemModel("应用", TabType.Application)
            {
                Content = new ApplicationTabView()
            };
            Tabs.Add(applicationTab);

            // 加载插件页签
            LoadPluginTabs();

            // 添加固定的"设置"页签（不可关闭）
            TabItemModel settingsTab = new TabItemModel("设置", TabType.Settings)
            {
                Content = new SettingsTabView() // SettingsTabView 有自己的 ViewModel
            };
            Tabs.Add(settingsTab);

            // 初始化命令
            AddCustomTabCommand = ReactiveCommand.Create(ExecuteAddCustomTab);
            // 删除页签
            RemoveTabCommand = ReactiveCommand.Create<TabItemModel>(ExecuteRemoveTab);

            // 默认选中第一个页签
            SelectedTabIndex = 0;
            
            // 订阅事件消息
            EventAggregator.Instance.Subscribe<NavigateToTabMessage>(OnNavigateToTab);
            EventAggregator.Instance.Subscribe<AddCustomTabRequestMessage>(OnAddCustomTabRequest);
        }
        
        /// <summary>
        /// 处理导航到标签页消息
        /// </summary>
        private void OnNavigateToTab(NavigateToTabMessage message)
        {
            if (message.TabIndex.HasValue)
            {
                if (message.TabIndex.Value >= 0 && message.TabIndex.Value < Tabs.Count)
                {
                    SelectedTabIndex = message.TabIndex.Value;
                }
            }
            else if (!string.IsNullOrEmpty(message.TabName))
            {
                for (int i = 0; i < Tabs.Count; i++)
                {
                    if (Tabs[i].Header == message.TabName)
                    {
                        SelectedTabIndex = i;
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理添加自定义标签页请求
        /// </summary>
        private void OnAddCustomTabRequest(AddCustomTabRequestMessage message)
        {
            ExecuteAddCustomTab();
        }

        /// <summary>
        /// 页签集合
        /// </summary>
        public ObservableCollection<TabItemModel> Tabs { get; }

        /// <summary>
        /// 当前选中的页签索引
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
        }

        /// <summary>
        /// 添加自定义页签命令
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddCustomTabCommand { get; }

        /// <summary>
        /// 删除页签命令
        /// </summary>
        public ReactiveCommand<TabItemModel, Unit> RemoveTabCommand { get; }

        /// <summary>
        /// 执行添加自定义页签
        /// </summary>
        private void ExecuteAddCustomTab()
        {
            int customTabCount = Tabs.Count - 1; // 减去设置页签
            string tabName = $"自定义 {customTabCount}";
            
            // 创建自定义页签的内容
            StackPanel customContent = new StackPanel();
            customContent.Children.Add(new TextBlock
            {
                Text = tabName,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            });
            customContent.Children.Add(new TextBlock
            {
                Text = "这是一个自定义页签的内容区域",
                Foreground = Brushes.Gray
            });
            
            TabItemModel newTab = new TabItemModel(tabName, TabType.Custom)
            {
                Content = customContent
            };

            // 插入到倒数第二个位置（设置页签之前）
            Tabs.Insert(Tabs.Count - 1, newTab);

            // 选中新添加的页签
            SelectedTabIndex = Tabs.Count - 2;
        }

        /// <summary>
        /// 执行删除页签
        /// </summary>
        private void ExecuteRemoveTab(TabItemModel tab)
        {
            if (tab != null && tab.IsClosable && Tabs.Contains(tab))
            {
                int index = Tabs.IndexOf(tab);
                Tabs.Remove(tab);

                // 如果删除的是当前选中的页签，调整选中索引
                if (SelectedTabIndex >= Tabs.Count)
                {
                    SelectedTabIndex = Tabs.Count - 1;
                }
            }
        }

        /// <summary>
        /// 加载插件页签
        /// </summary>
        private void LoadPluginTabs()
        {
            // 获取插件目录路径（在应用程序目录下的Plugins文件夹）
            string appDirectory = System.AppContext.BaseDirectory;
            string pluginDirectory = Path.Combine(appDirectory, "Plugins");

            // 加载所有插件
            System.Collections.Generic.List<Control> plugins = PluginLoader.LoadPlugins(pluginDirectory);

            // 为每个插件创建页签
            foreach (Control plugin in plugins)
            {
                string pluginName = PluginLoader.GetPluginName(plugin);
                TabItemModel pluginTab = new TabItemModel(
                    pluginName,
                    TabType.Plugin
                )
                {
                    Content = plugin  // 将插件控件设置为内容
                };
                Tabs.Add(pluginTab);
            }
        }

    }
}
