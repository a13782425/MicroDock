using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
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
        
        // 在 XAML 加载前初始化 ViewModel，确保 DataContext 绑定能正常工作
        if (_plugin.Context != null && _plugin.Services != null)
        {
            _viewModel = new MicroNoteTabViewModel(_plugin.Services);
            DataContext = _viewModel;
        }
        
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
        if (_plugin.Context == null || _plugin.Services == null) return;

        // 如果 ViewModel 尚未初始化（构造函数中可能已初始化）
        if (_viewModel == null)
        {
            _viewModel = new MicroNoteTabViewModel(_plugin.Services);
            DataContext = _viewModel;
        }

        // 获取子控件并设置 ViewModel 和事件
        _fileTreeView = this.FindControl<FileTreeView>("FileTreeView");
        _markdownEditor = this.FindControl<MarkdownEditor>("MarkdownEditor");

        if (_fileTreeView != null)
        {
            // SetViewModel 会设置内部的 _viewModel 字段，双击等功能依赖它
            _fileTreeView.SetViewModel(_viewModel.FileTree);
            _fileTreeView.FileSelected += OnFileSelected;
        }

        if (_markdownEditor != null)
        {
            // SetViewModel 会设置内部的 _viewModel 字段，编辑器功能依赖它
            _markdownEditor.SetViewModel(_viewModel.Editor);
            
            // 从 DI 获取图片服务
            var imageService = _plugin.Services.GetRequiredService<IImageService>();
            _markdownEditor.SetImageService(imageService);
        }
    }

    private async void OnFileSelected(object? sender, FileNodeViewModel node)
    {
        if (_viewModel == null) return;

        await _viewModel.OpenFileAsync(node);
    }
}
