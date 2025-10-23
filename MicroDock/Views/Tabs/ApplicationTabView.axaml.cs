using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroDock.Database;
using MicroDock.Services;
using MicroDock.ViewModels;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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

    private void AppItem_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ApplicationDB app)
        {
            IconService.TryStartProcess(app.FilePath);
        }
    }

    private void AppItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ApplicationDB app)
        {
            PointerPointProperties properties = e.GetCurrentPoint(border).Properties;
            
            // 右键点击
            if (properties.IsRightButtonPressed)
            {
                ShowContextMenu(border, app);
                e.Handled = true;
            }
        }
    }

    private void ShowContextMenu(Control control, ApplicationDB app)
    {
        ContextMenu contextMenu = new ContextMenu();
        
        // 打开
        MenuItem openItem = new MenuItem { Header = "打开" };
        openItem.Click += (s, e) => IconService.TryStartProcess(app.FilePath);
        contextMenu.Items.Add(openItem);
        
        // 打开文件位置
        MenuItem openLocationItem = new MenuItem { Header = "打开文件位置" };
        openLocationItem.Click += (s, e) => OpenFileLocation(app.FilePath);
        contextMenu.Items.Add(openLocationItem);
        
        // 分隔符
        contextMenu.Items.Add(new Separator());
        
        // 删除
        MenuItem deleteItem = new MenuItem { Header = "删除" };
        deleteItem.Click += (s, e) =>
        {
            if (DataContext is ApplicationTabViewModel viewModel)
            {
                viewModel.RemoveApplication(app);
            }
        };
        contextMenu.Items.Add(deleteItem);
        
        // 显示菜单
        contextMenu.Open(control);
    }

    private void OpenFileLocation(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                // 打开文件所在文件夹并选中该文件
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            else if (Directory.Exists(filePath))
            {
                // 打开文件夹
                Process.Start("explorer.exe", $"\"{filePath}\"");
            }
        }
        catch
        {
            // 静默失败
        }
    }
}