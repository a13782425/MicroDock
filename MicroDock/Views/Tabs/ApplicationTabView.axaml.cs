using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MicroDock.Database;
using MicroDock.Services;

namespace MicroDock.Views;

public partial class ApplicationTabView : UserControl
{
    public ApplicationTabView()
    {
        InitializeComponent();
    }

    private void LaunchButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ApplicationDB app)
        {
            IconService.TryStartProcess(app.FilePath);
        }
    }
}