using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MicroDock.Database;
using MicroDock.Service;
using MicroDock.ViewModels;

namespace MicroDock.Views.Controls;

public partial class AppItemControl : UserControl
{
    public AppItemControl()
    {
        InitializeComponent();
        // 绑定菜单项点击事件
        OpenMenuItem.Click += OnOpenClick;
        OpenLocationMenuItem.Click += OnOpenLocationClick;
        DeleteMenuItem.Click += OnDeleteClick;
    }

    private ApplicationTabViewModel? GetViewModel()
    {
        // 通过可视树查找父级的 DataContext
        Visual? parent = this.GetVisualParent();
        while (parent != null)
        {
            if (parent is Control control && control.DataContext is ApplicationTabViewModel viewModel)
            {
                return viewModel;
            }
            parent = parent.GetVisualParent();
        }
        return null;
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ApplicationDB app)
        {
            ServiceLocator.Get<IPlatformService>()?.TryStartProcess(app.FilePath);
        }
    }

    private void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ApplicationDB app)
        {
            ServiceLocator.Get<IPlatformService>()?.TryStartProcess(app.FilePath);
        }
    }

    private void OnOpenLocationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ApplicationDB app)
        {
            ServiceLocator.Get<IPlatformService>()?.OpenExplorer(app.FilePath);
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ApplicationDB app)
        {
            ApplicationTabViewModel? viewModel = GetViewModel();
            viewModel?.RemoveApplication(app);
        }
    }


}

