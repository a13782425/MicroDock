using System.Collections.ObjectModel;
using ReactiveUI;
using MicroNotePlugin.Models;

namespace MicroNotePlugin.ViewModels;

/// <summary>
/// 文件树节点类型
/// </summary>
public enum FileNodeType
{
    /// <summary>根节点（收藏、常用、全部文件）</summary>
    Root,
    /// <summary>文件夹</summary>
    Folder,
    /// <summary>文件</summary>
    File
}

/// <summary>
/// 文件树节点 ViewModel
/// </summary>
public class FileNodeViewModel : ReactiveObject
{
    private string _name = string.Empty;
    private string _hash = string.Empty;
    private string _folderPath = string.Empty;
    private FileNodeType _nodeType;
    private bool _isFavorite;
    private bool _isExpanded;
    private bool _isSelected;
    private int _openCount;
    private ObservableCollection<FileNodeViewModel> _children = new();

    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// 文件 Hash（仅文件节点有效）
    /// </summary>
    public string Hash
    {
        get => _hash;
        set => this.RaiseAndSetIfChanged(ref _hash, value);
    }

    /// <summary>
    /// 虚拟文件夹路径（文件夹节点的完整路径，或文件节点所属的文件夹路径）
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
    /// 是否有子节点
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// 从 NoteFile 创建节点
    /// </summary>
    public static FileNodeViewModel FromNoteFile(NoteFile file, bool isFavorite = false, int openCount = 0)
    {
        return new FileNodeViewModel
        {
            Name = file.Name,
            Hash = file.Hash,
            FolderPath = file.Folder,
            NodeType = FileNodeType.File,
            IsFavorite = isFavorite,
            OpenCount = openCount
        };
    }

    /// <summary>
    /// 从 NoteMetadata 创建节点
    /// </summary>
    public static FileNodeViewModel FromNoteMetadata(NoteMetadata metadata)
    {
        return new FileNodeViewModel
        {
            Name = metadata.Name,
            Hash = metadata.Hash,
            FolderPath = metadata.Folder,
            NodeType = FileNodeType.File,
            IsFavorite = metadata.IsFavorite,
            OpenCount = metadata.OpenCount
        };
    }

    /// <summary>
    /// 从 NoteFolder 创建节点
    /// </summary>
    public static FileNodeViewModel FromNoteFolder(NoteFolder folder, Func<string, bool> isFavoriteChecker, Func<string, int> openCountGetter)
    {
        var node = new FileNodeViewModel
        {
            Name = folder.Name,
            FolderPath = folder.Path,
            NodeType = FileNodeType.Folder,
            IsExpanded = false
        };

        // 添加子文件夹
        foreach (var subFolder in folder.SubFolders.OrderBy(f => f.Name))
        {
            node.Children.Add(FromNoteFolder(subFolder, isFavoriteChecker, openCountGetter));
        }

        // 添加子文件
        foreach (var file in folder.Files.OrderBy(f => f.Name))
        {
            node.Children.Add(FromNoteFile(file, isFavoriteChecker(file.Hash), openCountGetter(file.Hash)));
        }

        return node;
    }

    /// <summary>
    /// 创建根节点
    /// </summary>
    public static FileNodeViewModel CreateRoot(string name)
    {
        return new FileNodeViewModel
        {
            Name = name,
            NodeType = FileNodeType.Root,
            IsExpanded = true
        };
    }
}
