using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using MicroNotePlugin.ViewModels;
using System.Globalization;

namespace MicroNotePlugin.Views.Controls;

public partial class FileTreeView : UserControl
{
    private FileTreeViewModel? _viewModel;
    private FileNodeViewModel? _draggedNode;
    private TreeView? _fileTree;

    public FileTreeView()
    {
        AvaloniaXamlLoader.Load(this);

        // 绑定事件
        this.Loaded += OnLoaded;
    }

    /// <summary>
    /// 文件选择事件
    /// </summary>
    public event EventHandler<FileNodeViewModel>? FileSelected;

    /// <summary>
    /// 设置 ViewModel
    /// </summary>
    public void SetViewModel(FileTreeViewModel viewModel)
    {
        // 解除旧 ViewModel 的事件订阅
        if (_viewModel != null)
        {
            _viewModel.NoteCreated -= OnNoteCreated;
        }

        _viewModel = viewModel;
        DataContext = viewModel;

        // 订阅新 ViewModel 的事件
        if (_viewModel != null)
        {
            _viewModel.NoteCreated += OnNoteCreated;
        }
    }

    private void OnNoteCreated(object? sender, FileNodeViewModel node)
    {
        // 当通过右键菜单创建笔记时，触发文件选择事件
        FileSelected?.Invoke(this, node);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 绑定按钮事件
        var newNoteButton = this.FindControl<Button>("NewNoteButton");
        var newFolderButton = this.FindControl<Button>("NewFolderButton");
        var refreshButton = this.FindControl<Button>("RefreshButton");
        _fileTree = this.FindControl<TreeView>("FileTree");
        var searchBox = this.FindControl<TextBox>("SearchBox");
        var clearSearchButton = this.FindControl<Button>("ClearSearchButton");

        if (newNoteButton != null)
            newNoteButton.Click += OnNewNoteClick;

        if (newFolderButton != null)
            newFolderButton.Click += OnNewFolderClick;

        if (refreshButton != null)
            refreshButton.Click += OnRefreshClick;

        if (_fileTree != null)
        {
            _fileTree.DoubleTapped += OnTreeDoubleTapped;
            _fileTree.SelectionChanged += OnTreeSelectionChanged;

            // 启用拖拽
            SetupDragDrop();

            // 监听树内的按键事件（用于编辑框）
            _fileTree.AddHandler(KeyDownEvent, OnTreeKeyDown, RoutingStrategies.Tunnel);
        }

        if (searchBox != null)
        {
            searchBox.KeyDown += OnSearchBoxKeyDown;
        }

        if (clearSearchButton != null)
        {
            clearSearchButton.Click += OnClearSearchClick;
        }
    }

    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel?.SelectedNode == null) return;
        var node = _viewModel.SelectedNode;

        if (!node.IsEditing) return;

        if (e.Key == Key.Enter)
        {
            // 确认重命名
            _viewModel.ConfirmRenameCommand.Execute(node).Subscribe();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // 取消重命名
            _viewModel.CancelRenameCommand.Execute(node).Subscribe();
            e.Handled = true;
        }
    }

    private void SetupDragDrop()
    {
        if (_fileTree == null) return;

        // 设置拖拽事件
        _fileTree.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        _fileTree.AddHandler(DragDrop.DropEvent, OnDrop);
        _fileTree.PointerPressed += OnTreePointerPressed;
    }

    private void OnTreePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_fileTree == null || _viewModel == null) return;

        var point = e.GetCurrentPoint(_fileTree);
        if (point.Properties.IsLeftButtonPressed)
        {
            var node = _viewModel.SelectedNode;
            if (node != null && !node.IsRoot && !node.IsEditing)
            {
                _draggedNode = node;

                // 开始拖拽
                var data = new DataObject();
                data.Set("FileNode", node);

                DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (_viewModel == null || _draggedNode == null) return;

        e.DragEffects = DragDropEffects.None;

        // 获取目标节点
        var targetNode = GetNodeAtPosition(e);
        if (targetNode != null && _viewModel.CanMove(_draggedNode, targetNode))
        {
            e.DragEffects = DragDropEffects.Move;
        }

        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (_viewModel == null || _draggedNode == null) return;

        var targetNode = GetNodeAtPosition(e);
        if (targetNode != null && _viewModel.CanMove(_draggedNode, targetNode))
        {
            _viewModel.MoveNode(_draggedNode, targetNode);
        }

        _draggedNode = null;
        e.Handled = true;
    }

    private FileNodeViewModel? GetNodeAtPosition(DragEventArgs e)
    {
        if (_fileTree == null) return null;

        // 尝试从选中项获取
        if (_fileTree.SelectedItem is FileNodeViewModel node)
        {
            return node;
        }

        return null;
    }

    private async void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel != null)
        {
            await _viewModel.SearchAsync(_viewModel.SearchKeyword);
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel?.ClearSearch();
        }
    }

    private void OnClearSearchClick(object? sender, RoutedEventArgs e)
    {
        _viewModel?.ClearSearch();
    }

    private void OnNewNoteClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        
        // 简单地创建一个新笔记
        var newNote = _viewModel.CreateNote("新建笔记", _viewModel.SelectedNode);
        if (newNote != null)
        {
            _viewModel.SelectedNode = newNote;
            FileSelected?.Invoke(this, newNote);
        }
    }

    private void OnNewFolderClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        _viewModel.CreateFolder("新建文件夹", _viewModel.SelectedNode);
    }

    private void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        _viewModel?.RefreshTree();
    }

    private void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel?.SelectedNode is { IsFile: true, IsEditing: false } node)
        {
            _viewModel.RecordFileOpen(node);
            FileSelected?.Invoke(this, node);
        }
    }

    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // 当选择改变时，如果之前有节点在编辑状态，取消编辑
        if (e.RemovedItems != null)
        {
            foreach (var item in e.RemovedItems)
            {
                if (item is FileNodeViewModel oldNode && oldNode.IsEditing)
                {
                    oldNode.CancelEditing();
                }
            }
        }
    }

    /// <summary>
    /// 切换选中节点的收藏状态
    /// </summary>
    public void ToggleSelectedFavorite()
    {
        if (_viewModel?.SelectedNode is { IsFile: true } node)
        {
            _viewModel.ToggleFavorite(node);
        }
    }

    /// <summary>
    /// 删除选中的节点
    /// </summary>
    public void DeleteSelected()
    {
        if (_viewModel?.SelectedNode is { IsRoot: false } node)
        {
            _viewModel.DeleteNode(node);
        }
    }

    /// <summary>
    /// 重命名选中的节点
    /// </summary>
    public void RenameSelected(string newName)
    {
        if (_viewModel?.SelectedNode is { IsRoot: false } node)
        {
            _viewModel.RenameNode(node, newName);
        }
    }

    /// <summary>
    /// 开始重命名选中的节点
    /// </summary>
    public void StartRenameSelected()
    {
        if (_viewModel?.SelectedNode is { IsRoot: false } node)
        {
            node.StartEditing();
        }
    }
}

/// <summary>
/// 节点图标转换器
/// </summary>
public class NodeIconConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not FileNodeType nodeType)
            return "📄";

        var name = values[1] as string ?? "";

        return nodeType switch
        {
            FileNodeType.Root => name switch
            {
                "⭐ 收藏" => "",
                "📊 常用" => "",
                "📁 全部文件" => "",
                _ when name.StartsWith("🔍") => "", // 搜索结果
                _ when name.StartsWith("🏷️") => "", // 标签
                _ => "📂"
            },
            FileNodeType.Folder => "📂",
            FileNodeType.File => "📄",
            _ => "📄"
        };
    }
}

/// <summary>
/// 大于零转换器
/// </summary>
public static class ObjectConverters
{
    public static FuncValueConverter<int, bool> IsGreaterThanZero { get; } =
        new(count => count > 0);
}

/// <summary>
/// 布尔值到字体粗细的转换器
/// </summary>
public static class BoolConverters
{
    public static FuncValueConverter<bool, Avalonia.Media.FontWeight> ToFontWeight { get; } =
        new(isRoot => isRoot ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal);
}
