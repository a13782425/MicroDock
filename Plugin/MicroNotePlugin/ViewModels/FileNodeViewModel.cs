using System.Collections.ObjectModel;
using ReactiveUI;
using MicroNotePlugin.Entities;
using FluentAvalonia.UI.Controls;

namespace MicroNotePlugin.ViewModels;

/// <summary>
/// 文件树节点类型
/// </summary>
public enum FileNodeType
{
    /// <summary>根节点（收藏、常用、全部文件、标签）</summary>
    Root,
    /// <summary>文件夹</summary>
    Folder,
    /// <summary>文件</summary>
    File,
    /// <summary>标签</summary>
    Tag
}



/// <summary>
/// 文件树节点子类型（用于特殊图标显示）
/// </summary>
public enum FileNodeSubType
{
    /// <summary>普通节点（默认）</summary>
    None,
    /// <summary>收藏根节点</summary>
    FavoritesRoot,
    /// <summary>常用根节点</summary>
    FrequentRoot,
    /// <summary>标签根节点</summary>
    TagsRoot,
    /// <summary>全部文件根节点</summary>
    AllFilesRoot,
    /// <summary>搜索结果根节点</summary>
    SearchResultsRoot
}

/// <summary>
/// 文件树节点 ViewModel
/// </summary>
public class FileNodeViewModel : ReactiveObject
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string? _folderId;
    private string _folderPath = string.Empty;
    private FileNodeType _nodeType;
    private FileNodeSubType _subType;
    private bool _isFavorite;
    private bool _isExpanded;
    private bool _isSelected;
    private int _openCount;
    private bool _isEditing;
    private string _editingName = string.Empty;
    private ObservableCollection<FileNodeViewModel> _children = new();

    /// <summary>
    /// 节点 ID（笔记 ID、文件夹 ID 或标签 ID）
    /// </summary>
    public string Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// 所属文件夹 ID（仅文件节点有效）
    /// </summary>
    public string? FolderId
    {
        get => _folderId;
        set => this.RaiseAndSetIfChanged(ref _folderId, value);
    }

    /// <summary>
    /// 虚拟文件夹路径（文件夹节点的完整路径）
    /// </summary>
    public string FolderPath
    {
        get => _folderPath;
        set => this.RaiseAndSetIfChanged(ref _folderPath, value);
    }

    /// <summary>
    /// 节点类型
    /// </summary>
    public FileNodeType NodeType
    {
        get => _nodeType;
        set => this.RaiseAndSetIfChanged(ref _nodeType, value);
    }

    /// <summary>
    /// 节点子类型
    /// </summary>
    public FileNodeSubType SubType
    {
        get => _subType;
        set => this.RaiseAndSetIfChanged(ref _subType, value);
    }

    /// <summary>
    /// 图标符号
    /// </summary>
    public Symbol IconSymbol
    {
        get
        {
            if (SubType != FileNodeSubType.None)
            {
                return SubType switch
                {
                    FileNodeSubType.FavoritesRoot => Symbol.Star,
                    FileNodeSubType.FrequentRoot => Symbol.Clock,
                    FileNodeSubType.AllFilesRoot => Symbol.Library,
                    FileNodeSubType.TagsRoot => Symbol.Tag,
                    FileNodeSubType.SearchResultsRoot => Symbol.Find,
                    _ => Symbol.Folder
                };
            }

            return NodeType switch
            {
                FileNodeType.Folder => Symbol.Folder,
                FileNodeType.File => Symbol.Document,
                FileNodeType.Tag => Symbol.Tag,
                _ => Symbol.Document
            };
        }
    }

    /// <summary>
    /// 是否收藏
    /// </summary>
    public bool IsFavorite
    {
        get => _isFavorite;
        set => this.RaiseAndSetIfChanged(ref _isFavorite, value);
    }

    /// <summary>
    /// 是否展开
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    /// <summary>
    /// 是否选中
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    /// 打开次数（用于常用显示）
    /// </summary>
    public int OpenCount
    {
        get => _openCount;
        set => this.RaiseAndSetIfChanged(ref _openCount, value);
    }

    /// <summary>
    /// 是否正在编辑名称
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set => this.RaiseAndSetIfChanged(ref _isEditing, value);
    }

    /// <summary>
    /// 编辑中的名称（临时存储）
    /// </summary>
    public string EditingName
    {
        get => _editingName;
        set => this.RaiseAndSetIfChanged(ref _editingName, value);
    }

    /// <summary>
    /// 子节点
    /// </summary>
    public ObservableCollection<FileNodeViewModel> Children
    {
        get => _children;
        set => this.RaiseAndSetIfChanged(ref _children, value);
    }

    /// <summary>
    /// 是否是文件节点
    /// </summary>
    public bool IsFile => NodeType == FileNodeType.File;

    /// <summary>
    /// 是否是文件夹节点
    /// </summary>
    public bool IsFolder => NodeType == FileNodeType.Folder;

    /// <summary>
    /// 是否是根节点
    /// </summary>
    public bool IsRoot => NodeType == FileNodeType.Root;

    /// <summary>
    /// 是否是标签节点
    /// </summary>
    public bool IsTag => NodeType == FileNodeType.Tag;

    /// <summary>
    /// 是否有子节点
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// 开始编辑名称
    /// </summary>
    public void StartEditing()
    {
        EditingName = Name;
        IsEditing = true;
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    public void CancelEditing()
    {
        IsEditing = false;
        EditingName = string.Empty;
    }

    /// <summary>
    /// 从 Note 实体创建节点
    /// </summary>
    public static FileNodeViewModel FromNote(Note note)
    {
        return new FileNodeViewModel
        {
            Id = note.Id,
            Name = note.Name,
            FolderId = note.FolderId,
            NodeType = FileNodeType.File,
            IsFavorite = note.IsFavorite,
            OpenCount = note.OpenCount
        };
    }

    /// <summary>
    /// 从 Folder 实体创建节点
    /// </summary>
    public static FileNodeViewModel FromFolder(Folder folder)
    {
        return new FileNodeViewModel
        {
            Id = folder.Id,
            Name = folder.Name,
            FolderPath = folder.Path,
            NodeType = FileNodeType.Folder,
            IsExpanded = false
        };
    }

    /// <summary>
    /// 从 Tag 实体创建节点
    /// </summary>
    public static FileNodeViewModel FromTag(Tag tag)
    {
        return new FileNodeViewModel
        {
            Id = tag.Id,
            Name = tag.Name,
            NodeType = FileNodeType.Tag,
            IsExpanded = false
        };
    }

    /// <summary>
    /// 创建根节点
    /// </summary>
    public static FileNodeViewModel CreateRoot(string name, string id = "", FileNodeSubType subType = FileNodeSubType.None)
    {
        return new FileNodeViewModel
        {
            Id = id,
            Name = name,
            NodeType = FileNodeType.Root,
            SubType = subType,
            IsExpanded = true
        };
    }
}
