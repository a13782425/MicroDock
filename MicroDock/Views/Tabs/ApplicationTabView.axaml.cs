using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MicroDock.Database;
using MicroDock.Services;
using MicroDock.ViewModels;
using System.Collections.Generic;

namespace MicroDock.Views;

public partial class ApplicationTabView : UserControl
{
    public ApplicationTabView()
    {
        InitializeComponent();
        
        // 绑定拖放事件
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // 检查是否包含文件
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            IEnumerable<string>? fileNames = e.Data.GetFileNames();
            if (fileNames != null && DataContext is ApplicationTabViewModel viewModel)
            {
                foreach (string path in fileNames)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        viewModel.AddApplicationFromPath(path);
                    }
                }
            }
        }
    }

    private void LaunchButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ApplicationDB app)
        {
            IconService.TryStartProcess(app.FilePath);
        }
    }
}