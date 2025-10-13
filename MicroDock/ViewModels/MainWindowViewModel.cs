using System.Collections.ObjectModel;
using System.Reactive;
using System.IO;
using Avalonia.Controls;
using MicroDock.Models;
using MicroDock.Services;
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
            Tabs.Add(new TabItemModel("应用", TabType.Application));

            // 加载插件页签
            LoadPluginTabs();

            // 添加固定的"设置"页签（不可关闭）
            Tabs.Add(new TabItemModel("设置", TabType.Settings));

            // 初始化命令
            AddCustomTabCommand = ReactiveCommand.Create(ExecuteAddCustomTab);
            // 删除页签
            RemoveTabCommand = ReactiveCommand.Create<TabItemModel>(ExecuteRemoveTab);

            // 默认选中第一个页签
            SelectedTabIndex = 0;
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
            TabItemModel newTab = new TabItemModel($"自定义 {customTabCount}", TabType.Custom);

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
                    TabType.Plugin,
                    pluginControl: plugin
                );
                Tabs.Add(pluginTab);
            }
        }

    }
}
