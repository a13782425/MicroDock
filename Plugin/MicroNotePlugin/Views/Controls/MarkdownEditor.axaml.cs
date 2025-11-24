using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MicroNotePlugin.Views.Controls;

public partial class MarkdownEditor : UserControl
{
    public MarkdownEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
