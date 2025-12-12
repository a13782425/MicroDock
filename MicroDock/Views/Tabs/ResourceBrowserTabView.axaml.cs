using Avalonia.Controls;
using MicroDock.ViewModels;

namespace MicroDock.Views;

public partial class ResourceBrowserTabView : UserControl
{
    public ResourceBrowserTabView()
    {
        InitializeComponent();
        DataContext = new ResourceBrowserTabViewModel();
    }

    private async void onKeyDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is ResourceItemModel item)
        {
            // 复制到剪贴板
            await CopyToClipboardAsync(item.Key);
        }
    }
}