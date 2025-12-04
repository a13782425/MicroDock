using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using MicroDock.ViewModels;
using System.Collections.Generic;
using System.Linq;

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
            IEnumerable<IStorageItem>? files = e.Data.GetFiles();
            if (files != null && DataContext is ApplicationTabViewModel viewModel)
            {
                foreach (var file in files)
                {
                    string? path = file.TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        viewModel.AddApplicationFromPath(path);
                    }
                }
            }
        }
    }
}