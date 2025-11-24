using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MicroNotePlugin.Views.Controls;

public partial class FileTreeView : UserControl
{
    public FileTreeView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
