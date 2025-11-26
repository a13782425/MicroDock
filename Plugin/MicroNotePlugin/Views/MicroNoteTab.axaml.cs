using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroDock.Plugin;
using MicroNotePlugin.Services;
using MicroNotePlugin.ViewModels;
using MicroNotePlugin.Views.Controls;

namespace MicroNotePlugin.Views;

public partial class MicroNoteTab : UserControl, IMicroTab
{
    private readonly MicroNotePlugin _plugin;
    private MicroNoteTabViewModel? _viewModel;
    private FileTreeView? _fileTreeView;
    private MarkdownEditor? _markdownEditor;

    public MicroNoteTab(MicroNotePlugin plugin)
    {
        _plugin = plugin;
        AvaloniaXamlLoader.Load(this);
        this.Loaded += OnLoaded;
    }

    /// <summary>
    /// 页签名称
    /// </summary>
    public string TabName => "随手记";

    /// <summary>
    /// 页签图标
    /// </summary>
    public IconSymbolEnum IconSymbol => IconSymbolEnum.Edit;

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        InitializeServices();
    }

    private void InitializeServices()
    {
        if (_plugin.Context == null) return;

        // 创建服务（MetadataService 不再依赖 IPluginContext，改用 JSON 文件存储）
        var dataPath = _plugin.Context.DataPath;
        var metadataService = new MetadataService(dataPath);
        var fileService = new NoteFileService(dataPath, metadataService);
        var markdownService = new MarkdownService();
        var imageService = new ImageService(dataPath, metadataService);

        // 创建 ViewModel
        _viewModel = new MicroNoteTabViewModel(fileService, metadataService, markdownService);
        DataContext = _viewModel;

        // 获取子控件并设置 ViewModel
        _fileTreeView = this.FindControl<FileTreeView>("FileTreeView");
        _markdownEditor = this.FindControl<MarkdownEditor>("MarkdownEditor");

        if (_fileTreeView != null)
        {
            _fileTreeView.SetViewModel(_viewModel.FileTree);
            _fileTreeView.FileSelected += OnFileSelected;
        }

        if (_markdownEditor != null)
        {
            _markdownEditor.SetViewModel(_viewModel.Editor);
            _markdownEditor.SetImageService(imageService);
        }
    }

    private void OnFileSelected(object? sender, FileNodeViewModel node)
    {
        if (_viewModel == null) return;

        _viewModel.OpenFile(node);
    }
}
