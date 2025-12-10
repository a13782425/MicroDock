using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using MicroNotePlugin.ViewModels;
using System.Globalization;

using FluentAvalonia.UI.Controls;

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
        _viewModel = viewModel;
        DataContext = viewModel;
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

    private async void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel?.SelectedNode == null) return;
        var node = _viewModel.SelectedNode;

        if (!node.IsEditing) return;

        if (e.Key == Key.Enter)
        {
            // 确认重命名
            await _viewModel.RenameNodeAsync(node, node.EditingName);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // 取消重命名
            node.CancelEditing();
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
        if (targetNode != null && CanMove(_draggedNode, targetNode))
        {
            e.DragEffects = DragDropEffects.Move;
        }

        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (_viewModel == null || _draggedNode == null) return;

        var targetNode = GetNodeAtPosition(e);
        if (targetNode != null && CanMove(_draggedNode, targetNode))
        {
            await _viewModel.MoveNodeAsync(_draggedNode, targetNode);
        }

        _draggedNode = null;
        e.Handled = true;
    }

    /// <summary>
    /// 检查是否可以移动节点
    /// </summary>
    private bool CanMove(FileNodeViewModel source, FileNodeViewModel target)
    {
        // 不能移动到自己
        if (source.Id == target.Id) return false;

        // 不能移动到自己的子节点
        if (target.IsFolder && IsDescendantOf(target, source)) return false;

        // 文件夹可以移动到其他文件夹或根目录
        // 文件可以移动到文件夹
        return target.IsFolder || target.IsRoot;
    }

    private bool IsDescendantOf(FileNodeViewModel node, FileNodeViewModel potentialAncestor)
    {
        if (node.FolderId == potentialAncestor.Id) return true;

        foreach (var child in potentialAncestor.Children)
        {
            if (IsDescendantOf(node, child)) return true;
        }

        return false;
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
            await _viewModel.SearchAsync(_viewModel.SearchText);
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

    private async void OnNewNoteClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        
        // 获取当前选中的文件夹 ID
        string? folderId = null;
        if (_viewModel.SelectedNode?.IsFolder == true)
        {
            folderId = _viewModel.SelectedNode.Id;
        }
        else if (_viewModel.SelectedNode?.IsFile == true)
        {
            folderId = _viewModel.SelectedNode.FolderId;
        }
        
        await _viewModel.CreateNoteAsync(folderId);
        
        // 触发文件选择事件
        if (_viewModel.SelectedNode != null)
        {
            FileSelected?.Invoke(this, _viewModel.SelectedNode);
        }
    }

    private async void OnNewFolderClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        string? parentId = null;
        if (_viewModel.SelectedNode?.IsFolder == true)
        {
            parentId = _viewModel.SelectedNode.Id;
        }

        await _viewModel.CreateFolderAsync(parentId);
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.RefreshTreeAsync();
        }
    }

    private async void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel?.SelectedNode is { IsFile: true, IsEditing: false } node)
        {
            await _viewModel.RecordFileOpenAsync(node);
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
    public async void ToggleSelectedFavorite()
    {
        if (_viewModel?.SelectedNode is { IsFile: true } node)
        {
            await _viewModel.ToggleFavoriteAsync(node);
        }
    }

    /// <summary>
    /// 删除选中的节点
    /// </summary>
    public async void DeleteSelected()
    {
        if (_viewModel?.SelectedNode is { IsRoot: false } node)
        {
            await _viewModel.DeleteNodeAsync(node);
        }
    }

    /// <summary>
    /// 重命名选中的节点
    /// </summary>
    public async void RenameSelected(string newName)
    {
        if (_viewModel?.SelectedNode is { IsRoot: false } node)
        {
            await _viewModel.RenameNodeAsync(node, newName);
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

/// <summary>
/// 视图模式转换器，用于控制编辑器和预览的布局
/// </summary>
public static class ViewModeConverters
{
    /// <summary>
    /// 编辑器列跨度：分屏模式时跨1列，编辑模式时跨3列(占满)
    /// </summary>
    public static FuncValueConverter<bool, int> EditorColumnSpan { get; } =
        new(isSplitMode => isSplitMode ? 1 : 3);

    /// <summary>
    /// 预览列位置：分屏模式时在第2列，预览模式时在第0列
    /// </summary>
    public static FuncValueConverter<bool, int> PreviewColumn { get; } =
        new(isSplitMode => isSplitMode ? 2 : 0);

    /// <summary>
    /// 预览列跨度：分屏模式时跨1列，预览模式时跨3列(占满)
    /// </summary>
    public static FuncValueConverter<bool, int> PreviewColumnSpan { get; } =
        new(isSplitMode => isSplitMode ? 1 : 3);
}
