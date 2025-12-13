using Avalonia.Controls;
using Avalonia.Input;
using MicroDock.Plugin;
using MicroDock.Utils;

namespace MicroDock.Views.Controls;

public partial class PluginSettingCard : UserControl
{
    public PluginSettingCard()
    {
        InitializeComponent(true);
    }
    private async void PluginUniqueName_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            await UniversalUtils.CopyToClipboardAsync(textBlock.Text);
        }
    }
    private async void ToolName_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TextBlock { DataContext: ToolInfo tool })
        {
            await UniversalUtils.CopyToClipboardAsync(tool.Name);
        }
    }
}

