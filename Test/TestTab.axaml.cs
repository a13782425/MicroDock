using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MicroDock.Plugin;

namespace Test;
public partial class TestTab : UserControl, IMicroTab
{
    public string TabName => "ssss";

    public IconSymbolEnum IconSymbol => IconSymbolEnum.Library;

    public TestTab()
    {
        InitializeComponent();
    }
}