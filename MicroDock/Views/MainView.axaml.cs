using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using MicroDock.Extension;
using MicroDock.ViewModels;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace MicroDock.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        InitializeNavigationItems();

        // 监听大小变化以调整导航栏宽度
        this.SizeChanged += OnSizeChanged;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        this.SizeChanged -= OnSizeChanged;

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.NavigationItems.CollectionChanged -= OnNavigationItemsCollectionChanged;
        }

        if (MainNav != null)
        {
            MainNav.SelectionChanged -= OnNavigationSelectionChanged;
        }
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (MainNav == null) return;

        double targetWidth = Math.Clamp(e.NewSize.Width * 0.2, 128, 256);
        MainNav.OpenPaneLength = targetWidth;
    }

    private void InitializeNavigationItems()
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        if (MainNav == null)
            return;

        // 清空现有菜单项
        MainNav.MenuItems.Clear();
        MainNav.FooterMenuItems.Clear();

        // 添加导航项
        foreach (var navItem in viewModel.NavigationItems)
        {
            AddNavigationMenuItem(navItem);
        }

        // 订阅选中项变更事件
        MainNav.SelectionChanged += OnNavigationSelectionChanged;

        // 监听 NavigationItems 集合的变化
        viewModel.NavigationItems.CollectionChanged += OnNavigationItemsCollectionChanged;

        // 设置初始选中项
        if (viewModel.SelectedNavItem != null)
        {
            var itemToSelect = MainNav.MenuItems.OfType<NavigationViewItem>()
                .FirstOrDefault(i => i.Tag == viewModel.SelectedNavItem) ??
                MainNav.FooterMenuItems.OfType<NavigationViewItem>()
                .FirstOrDefault(i => i.Tag == viewModel.SelectedNavItem);

            if (itemToSelect != null)
            {
                MainNav.SelectedItem = itemToSelect;
            }
        }
        else if (viewModel.NavigationItems.Count > 0)
        {
            // 如果ViewModel没有选中项，默认选中第一个
            MainNav.SelectedItem = MainNav.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();
        }

        // 初始化宽度
        double targetWidth = Math.Clamp(this.Bounds.Width * 0.2, 128, 256);
        MainNav.OpenPaneLength = targetWidth;
    }

    private void AddNavigationMenuItem(NavigationItemModel navItem)
    {
        var menuItem = new NavigationViewItem
        {
            Content = navItem.Title,
            DataContext = navItem,
            Tag = navItem
        };

        menuItem.IsVisible = navItem.IsVisible;
        menuItem.Bind(Visual.IsVisibleProperty, new Binding(nameof(NavigationItemModel.IsVisible)));
        menuItem.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(NavigationItemModel.IsVisible))
            {
                RunUIThread(() =>
                {
                    MainNav.UpdatePaneLayout();
                });
            }
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

        // 添加锁定徽章
        SetupLockBadge(menuItem, navItem);

        ToolTip.SetTip(menuItem, navItem.Title);

        if (navItem.NavType == NavigationType.Settings || navItem.NavType == NavigationType.System)
        {
            MainNav.FooterMenuItems.Add(menuItem);
        }
        else
        {
            MainNav.MenuItems.Add(menuItem);
        }
    }

    /// <summary>
    /// 设置导航项的锁定徽章
    /// </summary>
    private void SetupLockBadge(NavigationViewItem menuItem, NavigationItemModel navItem)
    {
        // 创建锁定徽章
        var lockBadge = new InfoBadge
        {
            IconSource = new SymbolIconSource { Symbol = Symbol.ProtectedDocument },
            IsVisible = navItem.IsLocked
        };

        // 绑定 IsVisible 到 IsLocked 属性
        lockBadge.Bind(InfoBadge.IsVisibleProperty, new Binding(nameof(NavigationItemModel.IsLocked)));

        // 设置徽章到导航项
        menuItem.InfoBadge = lockBadge;
    }

    private void OnNavigationItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (MainNav == null) return;

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (NavigationItemModel newItem in e.NewItems)
            {
                AddNavigationMenuItem(newItem);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (NavigationItemModel oldItem in e.OldItems)
            {
                var collection = oldItem.NavType == NavigationType.Settings ?
                    MainNav.FooterMenuItems : MainNav.MenuItems;

                var itemToRemove = collection.OfType<NavigationViewItem>()
                    .FirstOrDefault(item => item.Tag == oldItem);

                if (itemToRemove != null)
                {
                    collection.Remove(itemToRemove);
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Move && e.OldItems != null && e.NewItems != null)
        {
            // 👇 新增：处理 Move 操作
            var movedItem = e.NewItems[0] as NavigationItemModel;
            if (movedItem != null)
            {
                var collection = movedItem.NavType == NavigationType.Settings ?
                    MainNav.FooterMenuItems : MainNav.MenuItems;

                var navViewItem = collection.OfType<NavigationViewItem>()
                    .FirstOrDefault(item => item.Tag == movedItem);

                if (navViewItem != null)
                {
                    collection.RemoveAt(e.OldStartingIndex);
                    collection.Insert(e.NewStartingIndex, navViewItem);
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            MainNav.MenuItems.Clear();
            MainNav.FooterMenuItems.Clear();
            if (DataContext is MainViewModel viewModel)
            {
                foreach (var navItem in viewModel.NavigationItems)
                {
                    AddNavigationMenuItem(navItem);
                }
            }
        }
    }

    private void OnNavigationSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        if (e.SelectedItem is NavigationViewItem item && item.Tag is NavigationItemModel navItem)
        {
            viewModel.SelectedNavItem = navItem;
        }
    }
}
