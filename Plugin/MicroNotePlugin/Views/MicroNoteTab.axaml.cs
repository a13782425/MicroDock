using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroDock.Plugin;
using MicroNotePlugin.ViewModels;

namespace MicroNotePlugin.Views;

public partial class MicroNoteTab : UserControl, IMicroTab
{
    public string TabName => "随手记";

    public IconSymbolEnum IconSymbol => IconSymbolEnum.Edit;

    private readonly MicroNoteTabViewModel _viewModel;

    public MicroNoteTab(MicroNotePlugin plugin)
    {
        InitializeComponent();
        _viewModel = new MicroNoteTabViewModel(plugin);
        DataContext = _viewModel;
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
