using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using MicroNotePlugin.Models;
using MicroNotePlugin.Services;

namespace MicroNotePlugin.ViewModels;

/// <summary>
/// 文件树 ViewModel
/// </summary>
public class FileTreeViewModel : ReactiveObject
{
    private readonly NoteFileService _fileService;
    private readonly MetadataService _metadataService;
    private SearchService? _searchService;

    private ObservableCollection<FileNodeViewModel> _rootNodes = new();
    private FileNodeViewModel? _selectedNode;
    private FileNodeViewModel? _favoritesNode;
    private FileNodeViewModel? _frequentNode;
    private FileNodeViewModel? _tagsNode;
    private FileNodeViewModel? _allFilesNode;
    private FileNodeViewModel? _searchResultsNode;

    private string _searchKeyword = string.Empty;
    private bool _isSearching;
    private ObservableCollection<SearchResultItem> _searchResults = new();

    /// <summary>
    /// 切换收藏命令
    /// </summary>
    public ReactiveCommand<FileNodeViewModel, Unit> ToggleFavoriteCommand { get; }

    /// <summary>
    /// 开始重命名命令
    /// </summary>
    public ReactiveCommand<FileNodeViewModel, Unit> StartRenameCommand { get; }

    /// <summary>
    /// 确认重命名命令
    /// </summary>
    public ReactiveCommand<FileNodeViewModel, Unit> ConfirmRenameCommand { get; }

    /// <summary>
    /// 取消重命名命令
    /// </summary>
    public ReactiveCommand<FileNodeViewModel, Unit> CancelRenameCommand { get; }

    /// <summary>
    /// 删除节点命令
    /// </summary>
    public ReactiveCommand<FileNodeViewModel, Unit> DeleteCommand { get; }

    /// <summary>
    /// 在指定节点下创建笔记命令
    /// </summary>
    public ReactiveCommand<FileNodeViewModel, Unit> CreateNoteCommand { get; }

    /// <summary>
    /// 在指定节点下创建文件夹命令
    /// </summary>
    public ReactiveCommand<FileNodeViewModel, Unit> CreateFolderCommand { get; }

    /// <summary>
    /// 创建笔记后的事件（用于通知 View 选中并打开新文件）
    /// </summary>
    public event EventHandler<FileNodeViewModel>? NoteCreated;

    public FileTreeViewModel(NoteFileService fileService, MetadataService metadataService)
    {
        _fileService = fileService;
        _metadataService = metadataService;
        _searchService = new SearchService(fileService);

        // 初始化命令
        ToggleFavoriteCommand = ReactiveCommand.Create<FileNodeViewModel>(node =>
        {
            if (node.IsFile)
            {
                ToggleFavorite(node);
            }
        });

        StartRenameCommand = ReactiveCommand.Create<FileNodeViewModel>(node =>
        {
            if (!node.IsRoot)
            {
                node.StartEditing();
            }
        });

        ConfirmRenameCommand = ReactiveCommand.Create<FileNodeViewModel>(node =>
        {
            if (node.IsEditing && !string.IsNullOrWhiteSpace(node.EditingName))
            {
                RenameNode(node, node.EditingName.Trim());
                node.CancelEditing();
            }
        });

        CancelRenameCommand = ReactiveCommand.Create<FileNodeViewModel>(node =>
        {
            node.CancelEditing();
        });

        DeleteCommand = ReactiveCommand.Create<FileNodeViewModel>(node =>
        {
            if (!node.IsRoot)
            {
                DeleteNode(node);
            }
        });

        CreateNoteCommand = ReactiveCommand.Create<FileNodeViewModel>(node =>
        {
            var newNote = CreateNote("新建笔记", node);
            if (newNote != null)
            {
                SelectedNode = newNote;
                NoteCreated?.Invoke(this, newNote);
            }
        });

        CreateFolderCommand = ReactiveCommand.Create<FileNodeViewModel>(node =>
        {
            CreateFolder("新建文件夹", node);
        });

        // 初始化树结构
        RefreshTree();
    }

    /// <summary>
    /// 根节点集合
    /// </summary>
    public ObservableCollection<FileNodeViewModel> RootNodes
    {
        get => _rootNodes;
        set => this.RaiseAndSetIfChanged(ref _rootNodes, value);
    }

    /// <summary>
    /// 当前选中的节点
    /// </summary>
    public FileNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    /// <summary>
    /// 笔记根目录路径
    /// </summary>
    public string NotesRootPath => _fileService.NotesRootPath;

    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => this.RaiseAndSetIfChanged(ref _searchKeyword, value);
    }

    /// <summary>
    /// 是否正在搜索
    /// </summary>
    public bool IsSearching
    {
        get => _isSearching;
        set => this.RaiseAndSetIfChanged(ref _isSearching, value);
    }

    /// <summary>
    /// 搜索结果
    /// </summary>
    public ObservableCollection<SearchResultItem> SearchResults
    {
        get => _searchResults;
        set => this.RaiseAndSetIfChanged(ref _searchResults, value);
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    public async Task SearchAsync(string keyword)
    {
        SearchKeyword = keyword;

        if (string.IsNullOrWhiteSpace(keyword))
        {
            ClearSearch();
            return;
        }

        IsSearching = true;

        try
        {
            if (_searchService == null)
                _searchService = new SearchService(_fileService);

            var results = await _searchService.SearchAsync(keyword);
            SearchResults.Clear();

            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            // 更新搜索结果节点
            UpdateSearchResultsNode();
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// 清除搜索
    /// </summary>
    public void ClearSearch()
    {
        SearchKeyword = string.Empty;
        SearchResults.Clear();
        
        // 移除搜索结果节点
        if (_searchResultsNode != null && RootNodes.Contains(_searchResultsNode))
        {
            RootNodes.Remove(_searchResultsNode);
            _searchResultsNode = null;
        }
    }

    /// <summary>
    /// 更新搜索结果节点
    /// </summary>
    private void UpdateSearchResultsNode()
    {
        // 移除旧的搜索结果节点
        if (_searchResultsNode != null && RootNodes.Contains(_searchResultsNode))
        {
            RootNodes.Remove(_searchResultsNode);
        }

        if (SearchResults.Count == 0)
        {
            _searchResultsNode = null;
            return;
        }

        // 创建搜索结果节点
        _searchResultsNode = FileNodeViewModel.CreateRoot($"🔍 搜索结果 ({SearchResults.Count})");
        _searchResultsNode.IsExpanded = true;

        foreach (var result in SearchResults)
        {
            var node = FileNodeViewModel.FromNoteFile(
                result.File,
                _metadataService.IsFavorite(result.File.Hash),
                result.TotalMatches);
            _searchResultsNode.Children.Add(node);
        }

        // 插入到第一个位置
        RootNodes.Insert(0, _searchResultsNode);
    }

    /// <summary>
    /// 刷新整个树结构
    /// </summary>
    public void RefreshTree()
    {
        RootNodes.Clear();

        // 创建四个根节点
        _favoritesNode = FileNodeViewModel.CreateRoot("⭐ 收藏");
        _frequentNode = FileNodeViewModel.CreateRoot("📊 常用");
        _tagsNode = FileNodeViewModel.CreateRoot("🏷️ 标签");
        _allFilesNode = FileNodeViewModel.CreateRoot("📁 全部文件");

        RootNodes.Add(_favoritesNode);
        RootNodes.Add(_frequentNode);
        RootNodes.Add(_tagsNode);
        RootNodes.Add(_allFilesNode);

        // 加载文件数据
        RefreshFavorites();
        RefreshFrequent();
        RefreshTags();
        RefreshAllFiles();
    }

    /// <summary>
    /// 刷新标签节点
    /// </summary>
    public void RefreshTags()
    {
        if (_tagsNode == null) return;

        _tagsNode.Children.Clear();

        var allTags = _metadataService.GetAllTags();

        foreach (var tag in allTags.OrderBy(t => t.Name))
        {
            var tagNode = FileNodeViewModel.CreateRoot($"🏷️ {tag.Name}");
            tagNode.IsExpanded = false;

            var notesByTag = _metadataService.GetNotesByTag(tag.Name);
            foreach (var metadata in notesByTag)
            {
                var node = FileNodeViewModel.FromNoteMetadata(metadata);
                tagNode.Children.Add(node);
            }

            // 只添加有笔记的标签
            if (tagNode.Children.Count > 0)
            {
                _tagsNode.Children.Add(tagNode);
            }
        }
    }

    /// <summary>
    /// 刷新收藏节点
    /// </summary>
    public void RefreshFavorites()
    {
        if (_favoritesNode == null) return;

        _favoritesNode.Children.Clear();

        var favorites = _metadataService.GetFavorites();

        foreach (var metadata in favorites)
        {
            var node = FileNodeViewModel.FromNoteMetadata(metadata);
            _favoritesNode.Children.Add(node);
        }
    }

    /// <summary>
    /// 刷新常用节点
    /// </summary>
    public void RefreshFrequent()
    {
        if (_frequentNode == null) return;

        _frequentNode.Children.Clear();

        var frequent = _metadataService.GetFrequentlyUsed(10);

        foreach (var metadata in frequent)
        {
            var node = FileNodeViewModel.FromNoteMetadata(metadata);
            _frequentNode.Children.Add(node);
        }
    }

    /// <summary>
    /// 刷新全部文件节点
    /// </summary>
    public void RefreshAllFiles()
    {
        if (_allFilesNode == null) return;

        _allFilesNode.Children.Clear();

        var folderStructure = _fileService.GetFolderStructure();

        // 添加子文件夹
        foreach (var subFolder in folderStructure.SubFolders.OrderBy(f => f.Name))
        {
            var node = FileNodeViewModel.FromNoteFolder(
                subFolder,
                _metadataService.IsFavorite,
                _metadataService.GetOpenCount);
            _allFilesNode.Children.Add(node);
        }

        // 添加文件
        foreach (var file in folderStructure.Files.OrderBy(f => f.Name))
        {
            var node = FileNodeViewModel.FromNoteFile(
                file,
                _metadataService.IsFavorite(file.Hash),
                _metadataService.GetOpenCount(file.Hash));
            _allFilesNode.Children.Add(node);
        }
    }

    /// <summary>
    /// 创建新笔记
    /// </summary>
    public FileNodeViewModel? CreateNote(string name, FileNodeViewModel? parentNode = null)
    {
        string folder = "/";

        if (parentNode != null)
        {
            if (parentNode.IsFolder)
            {
                folder = parentNode.FolderPath;
            }
            else if (parentNode.IsFile)
            {
                folder = parentNode.FolderPath;
            }
        }

        var noteFile = _fileService.CreateNote(name, folder);
        RefreshAllFiles();

        return FindNodeByHash(noteFile.Hash);
    }

    /// <summary>
    /// 创建新文件夹
    /// </summary>
    public FileNodeViewModel? CreateFolder(string name, FileNodeViewModel? parentNode = null)
    {
        string parentPath = "/";

        if (parentNode != null && parentNode.IsFolder)
        {
            parentPath = parentNode.FolderPath;
        }

        var folder = _fileService.CreateFolder(parentPath, name);
        RefreshAllFiles();

        return FindNodeByFolderPath(folder.Path);
    }

    /// <summary>
    /// 删除节点（文件或文件夹）
    /// </summary>
    public bool DeleteNode(FileNodeViewModel node)
    {
        if (node.IsRoot) return false;

        bool success;
        if (node.IsFile)
        {
            success = _fileService.DeleteNote(node.Hash);
        }
        else if (node.IsFolder)
        {
            _fileService.DeleteFolder(node.FolderPath);
            success = true;
        }
        else
        {
            return false;
        }

        if (success)
        {
            RefreshTree();
        }

        return success;
    }

    /// <summary>
    /// 移动文件到目标文件夹
    /// </summary>
    public bool MoveNode(FileNodeViewModel sourceNode, FileNodeViewModel targetNode)
    {
        // 不能移动根节点
        if (sourceNode.IsRoot) return false;

        // 确定目标文件夹路径
        string targetFolderPath;
        if (targetNode.IsRoot)
        {
            // 如果目标是根节点（全部文件），移动到根目录
            if (targetNode.Name.Contains("全部文件"))
            {
                targetFolderPath = "/";
            }
            else
            {
                return false; // 不能移动到收藏/常用节点
            }
        }
        else if (targetNode.IsFolder)
        {
            targetFolderPath = targetNode.FolderPath;
        }
        else if (targetNode.IsFile)
        {
            // 如果目标是文件，移动到文件所在的文件夹
            targetFolderPath = targetNode.FolderPath;
        }
        else
        {
            return false;
        }

        // 不能移动文件夹到自身或子文件夹
        if (sourceNode.IsFolder && targetFolderPath.StartsWith(sourceNode.FolderPath + "/"))
        {
            return false;
        }

        // 不能移动到同一位置
        if (sourceNode.FolderPath == targetFolderPath)
        {
            return false;
        }

        try
        {
            if (sourceNode.IsFile)
            {
                _fileService.MoveNote(sourceNode.Hash, targetFolderPath);
                RefreshTree();
                return true;
            }
            // 文件夹移动可以在这里添加
        }
        catch
        {
            return false;
        }

        return false;
    }

    /// <summary>
    /// 检查是否可以将源节点移动到目标节点
    /// </summary>
    public bool CanMove(FileNodeViewModel sourceNode, FileNodeViewModel targetNode)
    {
        if (sourceNode.IsRoot) return false;
        if (sourceNode == targetNode) return false;

        // 不能移动到收藏/常用节点
        if (targetNode.IsRoot && !targetNode.Name.Contains("全部文件"))
        {
            return false;
        }

        // 不能移动文件夹到自己的子文件夹
        if (sourceNode.IsFolder && targetNode.FolderPath.StartsWith(sourceNode.FolderPath + "/"))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 重命名节点
    /// </summary>
    public bool RenameNode(FileNodeViewModel node, string newName)
    {
        if (node.IsRoot) return false;

        if (node.IsFile)
        {
            _fileService.RenameNote(node.Hash, newName);
            RefreshTree();
            return true;
        }
        else if (node.IsFolder)
        {
            _fileService.RenameFolder(node.FolderPath, newName);
            RefreshTree();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 切换收藏状态
    /// </summary>
    public bool ToggleFavorite(FileNodeViewModel node)
    {
        if (!node.IsFile) return false;

        var isFavorite = _metadataService.ToggleFavorite(node.Hash);
        node.IsFavorite = isFavorite;

        RefreshFavorites();
        return isFavorite;
    }

    /// <summary>
    /// 记录打开文件
    /// </summary>
    public void RecordFileOpen(FileNodeViewModel node)
    {
        if (!node.IsFile) return;

        _metadataService.RecordOpen(node.Hash);
        RefreshFrequent();
    }

    /// <summary>
    /// 根据 Hash 查找节点
    /// </summary>
    public FileNodeViewModel? FindNodeByHash(string hash)
    {
        return FindNodeInCollection(RootNodes, n => n.IsFile && n.Hash == hash);
    }

    /// <summary>
    /// 根据文件夹路径查找节点
    /// </summary>
    public FileNodeViewModel? FindNodeByFolderPath(string folderPath)
    {
        return FindNodeInCollection(RootNodes, n => n.IsFolder && n.FolderPath == folderPath);
    }

    private FileNodeViewModel? FindNodeInCollection(ObservableCollection<FileNodeViewModel> nodes, Func<FileNodeViewModel, bool> predicate)
    {
        foreach (var node in nodes)
        {
            if (predicate(node))
            {
                return node;
            }

            var found = FindNodeInCollection(node.Children, predicate);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
