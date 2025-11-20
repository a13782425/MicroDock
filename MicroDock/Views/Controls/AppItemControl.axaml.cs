using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using MicroDock.Database;
using MicroDock.Service;
using MicroDock.ViewModel;

namespace MicroDock.Views.Controls;

public partial class AppItemControl : UserControl
{
    public AppItemControl()
    {
        InitializeComponent();
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
            IconService.TryStartProcess(app.FilePath);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ApplicationDB app)
        {
            PointerPointProperties properties = e.GetCurrentPoint(this).Properties;
            
            // 右键点击
            if (properties.IsRightButtonPressed)
            {
                ShowContextMenu(app);
                e.Handled = true;
            }
        }
    }

    private void ShowContextMenu(ApplicationDB app)
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
            // 通过 ViewModel 删除应用（MVVM 模式）
            ApplicationTabViewModel? viewModel = GetViewModel();
            viewModel?.RemoveApplication(app);
        };
        contextMenu.Items.Add(deleteItem);
        
        // 显示菜单
        contextMenu.Open(this);
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

