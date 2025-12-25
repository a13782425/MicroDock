using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
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
        // ✅ 使用新 API: DataFormats.Storage
        if (e.DataTransfer.Contains(DataFormat.File))
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
        var storageItems = e.DataTransfer?.TryGetFiles();
        if (storageItems is not null && DataContext is ApplicationTabViewModel viewModel)
        {
            foreach (var file in storageItems)
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