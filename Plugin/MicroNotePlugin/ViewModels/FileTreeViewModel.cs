using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using MicroNotePlugin.Entities;
using MicroNotePlugin.Services;

namespace MicroNotePlugin.ViewModels;

/// <summary>
/// 文件树 ViewModel
/// </summary>
public class FileTreeViewModel : ReactiveObject
{
    private readonly INoteRepository _noteRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ISearchService _searchService;

    private ObservableCollection<FileNodeViewModel> _rootNodes = new();
    private FileNodeViewModel? _selectedNode;
    private string _searchText = string.Empty;
    private bool _isSearching;
    private FileNodeViewModel? _searchResultsNode;
    private FileNodeViewModel? _favoritesNode;
    private FileNodeViewModel? _frequentNode;
    private FileNodeViewModel? _allFilesNode;
    private FileNodeViewModel? _tagsNode;

    public FileTreeViewModel(
        INoteRepository noteRepository,
        IFolderRepository folderRepository,
        ITagRepository tagRepository,
        ISearchService searchService)
    {
        _noteRepository = noteRepository;
        _folderRepository = folderRepository;
        _tagRepository = tagRepository;
        _searchService = searchService;

        // 初始化命令
        // 注意：XAML 中 CommandParameter="{Binding}" 传递的是 FileNodeViewModel 对象
        CreateNoteCommand = ReactiveCommand.CreateFromTask<FileNodeViewModel?>(async node =>
        {
            // 从节点提取 folderId：如果是文件夹则用其 Id，如果是文件则用其所属 FolderId
            var folderId = node?.IsFolder == true ? node.Id : node?.FolderId;
            await CreateNoteAsync(folderId);
        });
        CreateFolderCommand = ReactiveCommand.CreateFromTask<FileNodeViewModel?>(async node =>
        {
            // 只有当选中的是文件夹时，才在其下创建子文件夹
            var parentId = node?.IsFolder == true ? node.Id : null;
            await CreateFolderAsync(parentId);
        });
        DeleteCommand = ReactiveCommand.CreateFromTask<FileNodeViewModel>(DeleteNodeAsync);
        StartRenameCommand = ReactiveCommand.Create<FileNodeViewModel>(node => node?.StartEditing());
        ToggleFavoriteCommand = ReactiveCommand.CreateFromTask<FileNodeViewModel>(ToggleFavoriteAsync);
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshTreeAsync);

        // 搜索防抖
        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async text =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    ClearSearch();
                }
                else
                {
                    await SearchAsync(text);
                }
            });

        // 初始化树
        _ = RefreshTreeAsync();
    }

    #region Properties

    /// <summary>
    /// 根节点列表
    /// </summary>
    public ObservableCollection<FileNodeViewModel> RootNodes
    {
        get => _rootNodes;
        set => this.RaiseAndSetIfChanged(ref _rootNodes, value);
    }

    /// <summary>
    /// 选中的节点
    /// </summary>
    public FileNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    /// <summary>
    /// 搜索文本
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    /// <summary>
    /// 是否正在搜索
    /// </summary>
    public bool IsSearching
    {
        get => _isSearching;
        set => this.RaiseAndSetIfChanged(ref _isSearching, value);
    }

    #endregion

    #region Commands

    public ReactiveCommand<FileNodeViewModel?, Unit> CreateNoteCommand { get; }
    public ReactiveCommand<FileNodeViewModel?, Unit> CreateFolderCommand { get; }
    public ReactiveCommand<FileNodeViewModel, Unit> DeleteCommand { get; }
    public ReactiveCommand<FileNodeViewModel, Unit> StartRenameCommand { get; }
    public ReactiveCommand<FileNodeViewModel, Unit> ToggleFavoriteCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// 刷新整个树结构
    /// </summary>
    public async Task RefreshTreeAsync()
    {
        RootNodes.Clear();

        // 创建收藏节点
        _favoritesNode = FileNodeViewModel.CreateRoot("收藏", "favorites", FileNodeSubType.FavoritesRoot);
        await RefreshFavoritesAsync();
        RootNodes.Add(_favoritesNode);

        // 创建常用节点
        _frequentNode = FileNodeViewModel.CreateRoot("常用", "frequent", FileNodeSubType.FrequentRoot);
        await RefreshFrequentAsync();
        RootNodes.Add(_frequentNode);

        // 创建标签节点
        _tagsNode = FileNodeViewModel.CreateRoot("标签", "tags", FileNodeSubType.TagsRoot);
        await RefreshTagsAsync();
        RootNodes.Add(_tagsNode);

        // 创建全部文件节点
        _allFilesNode = FileNodeViewModel.CreateRoot("全部文件", "all", FileNodeSubType.AllFilesRoot);
        await RefreshAllFilesAsync();
        RootNodes.Add(_allFilesNode);
    }

    /// <summary>
    /// 搜索笔记
    /// </summary>
    public async Task SearchAsync(string keyword)
    {
        IsSearching = true;

        try
        {
            var results = await _searchService.SearchAsync(keyword);

            // 更新搜索结果节点
            if (_searchResultsNode == null)
            {
                _searchResultsNode = FileNodeViewModel.CreateRoot($"搜索: {keyword}", "search", FileNodeSubType.SearchResultsRoot);
                RootNodes.Insert(0, _searchResultsNode);
            }
            else
            {
                _searchResultsNode.Name = $"搜索: {keyword}";
            }

            _searchResultsNode.Children.Clear();
            foreach (var result in results)
            {
                _searchResultsNode.Children.Add(FileNodeViewModel.FromNote(result.Note));
            }
            _searchResultsNode.IsExpanded = true;
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
        if (_searchResultsNode != null)
        {
            RootNodes.Remove(_searchResultsNode);
            _searchResultsNode = null;
        }
    }

    /// <summary>
    /// 创建新笔记
    /// </summary>
    public async Task CreateNoteAsync(string? folderId)
    {
        var note = new Note
        {
            Name = "新建笔记",
            FolderId = folderId,
            Content = "# 新建笔记\n\n"
        };

        await _noteRepository.CreateAsync(note);
        await RefreshTreeAsync();

        // 选中新建的笔记
        var node = FindNodeById(note.Id);
        if (node != null)
        {
            SelectedNode = node;
            node.StartEditing();
        }
    }

    /// <summary>
    /// 创建新文件夹
    /// </summary>
    public async Task CreateFolderAsync(string? parentId)
    {
        await _folderRepository.CreateAsync("新建文件夹", parentId);
        await RefreshTreeAsync();
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    public async Task DeleteNodeAsync(FileNodeViewModel node)
    {
        if (node.IsFile)
        {
            await _noteRepository.DeleteAsync(node.Id);
        }
        else if (node.IsFolder)
        {
            await _folderRepository.DeleteAsync(node.Id);
        }

        await RefreshTreeAsync();
    }

    /// <summary>
    /// 重命名节点
    /// </summary>
    public async Task RenameNodeAsync(FileNodeViewModel node, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;

        if (node.IsFile)
        {
            await _noteRepository.RenameAsync(node.Id, newName);
        }
        else if (node.IsFolder)
        {
            await _folderRepository.RenameAsync(node.Id, newName);
        }

        node.Name = newName;
        node.CancelEditing();
    }

    /// <summary>
    /// 切换收藏状态
    /// </summary>
    public async Task ToggleFavoriteAsync(FileNodeViewModel node)
    {
        if (!node.IsFile) return;

        var isFavorite = await _noteRepository.ToggleFavoriteAsync(node.Id);
        node.IsFavorite = isFavorite;
        await RefreshFavoritesAsync();
    }

    /// <summary>
    /// 移动节点
    /// </summary>
    public async Task MoveNodeAsync(FileNodeViewModel source, FileNodeViewModel? target)
    {
        string? targetFolderId = null;

        if (target != null)
        {
            if (target.IsFolder)
            {
                targetFolderId = target.Id;
            }
            else if (target.IsFile)
            {
                targetFolderId = target.FolderId;
            }
        }

        if (source.IsFile)
        {
            await _noteRepository.MoveAsync(source.Id, targetFolderId);
        }
        else if (source.IsFolder)
        {
            await _folderRepository.MoveAsync(source.Id, targetFolderId);
        }

        await RefreshTreeAsync();
    }

    /// <summary>
    /// 记录打开文件
    /// </summary>
    public async Task RecordFileOpenAsync(FileNodeViewModel node)
    {
        if (!node.IsFile) return;
        await _noteRepository.RecordOpenAsync(node.Id);
        node.OpenCount++;
        await RefreshFrequentAsync();
    }

    /// <summary>
    /// 根据 ID 查找节点
    /// </summary>
    public FileNodeViewModel? FindNodeById(string id)
    {
        return FindNodeInCollection(RootNodes, n => n.Id == id);
    }

    #endregion

    #region Private Methods

    private async Task RefreshFavoritesAsync()
    {
        if (_favoritesNode == null) return;

        _favoritesNode.Children.Clear();
        var favorites = await _noteRepository.GetFavoritesAsync();
        foreach (var note in favorites)
        {
            _favoritesNode.Children.Add(FileNodeViewModel.FromNote(note));
        }
    }

    private async Task RefreshFrequentAsync()
    {
        if (_frequentNode == null) return;

        _frequentNode.Children.Clear();
        var frequent = await _noteRepository.GetFrequentAsync(10);
        foreach (var note in frequent)
        {
            _frequentNode.Children.Add(FileNodeViewModel.FromNote(note));
        }
    }

    private async Task RefreshTagsAsync()
    {
        if (_tagsNode == null) return;

        _tagsNode.Children.Clear();
        var tags = await _tagRepository.GetAllAsync();
        foreach (var tag in tags)
        {
            var tagNode = FileNodeViewModel.FromTag(tag);
            
            // 加载标签下的笔记
            var notes = await _noteRepository.GetByTagAsync(tag.Id);
            foreach (var note in notes)
            {
                tagNode.Children.Add(FileNodeViewModel.FromNote(note));
            }
            
            _tagsNode.Children.Add(tagNode);
        }
    }

    private async Task RefreshAllFilesAsync()
    {
        if (_allFilesNode == null) return;

        _allFilesNode.Children.Clear();

        // 加载根目录下的文件夹
        var rootFolders = await _folderRepository.GetChildrenAsync(null);
        foreach (var folder in rootFolders.OrderBy(f => f.Name))
        {
            var folderNode = await BuildFolderNodeAsync(folder);
            _allFilesNode.Children.Add(folderNode);
        }

        // 加载根目录下的笔记（没有文件夹的）
        var rootNotes = await _noteRepository.GetByFolderIdAsync(null);
        foreach (var note in rootNotes.OrderBy(n => n.Name))
        {
            _allFilesNode.Children.Add(FileNodeViewModel.FromNote(note));
        }
    }

    private async Task<FileNodeViewModel> BuildFolderNodeAsync(Folder folder)
    {
        var node = FileNodeViewModel.FromFolder(folder);

        // 加载子文件夹
        var subFolders = await _folderRepository.GetChildrenAsync(folder.Id);
        foreach (var subFolder in subFolders.OrderBy(f => f.Name))
        {
            var subNode = await BuildFolderNodeAsync(subFolder);
            node.Children.Add(subNode);
        }

        // 加载文件夹下的笔记
        var notes = await _noteRepository.GetByFolderIdAsync(folder.Id);
        foreach (var note in notes.OrderBy(n => n.Name))
        {
            node.Children.Add(FileNodeViewModel.FromNote(note));
        }

        return node;
    }

    private static FileNodeViewModel? FindNodeInCollection(
        IEnumerable<FileNodeViewModel> nodes,
        Func<FileNodeViewModel, bool> predicate)
    {
        foreach (var node in nodes)
        {
            if (predicate(node))
                return node;

            var found = FindNodeInCollection(node.Children, predicate);
            if (found != null)
                return found;
        }
        return null;
    }

    #endregion
}
