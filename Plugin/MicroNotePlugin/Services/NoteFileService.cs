using System.Security.Cryptography;
using System.Text;
using MicroNotePlugin.Models;

namespace MicroNotePlugin.Services;

/// <summary>
/// 笔记文件服务 - 负责文件的读写和管理
/// 文件使用 MD5 Hash 命名，按日期分目录存储
/// 存储路径: data/note/{yyyyMMdd}/{hash}
/// </summary>
public class NoteFileService
{
    private readonly string _dataPath;
    private readonly MetadataService _metadataService;

    public NoteFileService(string dataPath, MetadataService metadataService)
    {
        _dataPath = dataPath;
        _metadataService = metadataService;
        EnsureDirectoryExists(GetNotesDirectory());
    }

    /// <summary>
    /// 笔记存储根目录
    /// </summary>
    public string NotesRootPath => GetNotesDirectory();

    /// <summary>
    /// 获取笔记存储目录
    /// </summary>
    private string GetNotesDirectory()
    {
        return Path.Combine(_dataPath, "notes");
    }

    /// <summary>
    /// 获取按日期分类的存储目录
    /// </summary>
    private string GetDateDirectory(string dateFolder)
    {
        return Path.Combine(GetNotesDirectory(), dateFolder);
    }

    /// <summary>
    /// 获取笔记的完整存储路径
    /// </summary>
    public string GetFullPath(string dateFolder, string hash)
    {
        return Path.Combine(GetDateDirectory(dateFolder), hash);
    }

    /// <summary>
    /// 获取笔记的完整存储路径（从元数据）
    /// </summary>
    public string GetFullPath(NoteMetadata metadata)
    {
        return GetFullPath(metadata.DateFolder, metadata.Hash);
    }

    /// <summary>
    /// 计算内容的 MD5 Hash
    /// </summary>
    public static string ComputeHash(string content)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = md5.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 获取所有笔记文件
    /// </summary>
    public List<NoteFile> GetAllNoteFiles()
    {
        var notes = new List<NoteFile>();
        foreach (var metadata in _metadataService.GetAllNoteMetadata())
        {
            var noteFile = NoteFile.FromMetadata(metadata);
            notes.Add(noteFile);
        }
        return notes;
    }

    /// <summary>
    /// 获取文件夹结构（从元数据构建虚拟文件夹树）
    /// </summary>
    public NoteFolder GetFolderStructure()
    {
        var root = NoteFolder.FromPath("/");
        var folders = _metadataService.GetAllFolders();
        var notes = _metadataService.GetAllNoteMetadata().ToList();

        // 构建文件夹树
        var folderDict = new Dictionary<string, NoteFolder> { { "/", root } };

        // 添加所有文件夹
        foreach (var folderPath in folders.OrderBy(f => f.Count(c => c == '/')))
        {
            if (folderPath == "/") continue;

            var folder = NoteFolder.FromPath(folderPath);
            folderDict[folderPath] = folder;

            var parentPath = folder.ParentPath;
            if (string.IsNullOrEmpty(parentPath)) parentPath = "/";

            if (folderDict.TryGetValue(parentPath, out var parent))
            {
                parent.SubFolders.Add(folder);
            }
        }

        // 添加笔记到对应文件夹
        foreach (var metadata in notes)
        {
            var noteFile = NoteFile.FromMetadata(metadata);
            var folderPath = metadata.Folder;

            if (folderDict.TryGetValue(folderPath, out var folder))
            {
                folder.Files.Add(noteFile);
            }
            else
            {
                // 文件夹不存在，放到根目录
                root.Files.Add(noteFile);
            }
        }

        return root;
    }

    /// <summary>
    /// 读取笔记内容
    /// </summary>
    public string ReadNoteContent(string dateFolder, string hash)
    {
        var fullPath = GetFullPath(dateFolder, hash);
        if (!File.Exists(fullPath))
            return string.Empty;

        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// 读取笔记内容（从 Hash）
    /// </summary>
    public string ReadNoteContent(string hash)
    {
        var metadata = _metadataService.GetNoteMetadata(hash);
        if (metadata == null)
            return string.Empty;

        return ReadNoteContent(metadata.DateFolder, hash);
    }

    /// <summary>
    /// 保存笔记内容
    /// </summary>
    public NoteMetadata SaveNoteContent(string hash, string content)
    {
        var metadata = _metadataService.GetNoteMetadata(hash);
        if (metadata == null)
        {
            throw new InvalidOperationException($"Note with hash {hash} not found");
        }

        var fullPath = GetFullPath(metadata.DateFolder, hash);
        EnsureDirectoryExists(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);

        // 更新修改时间
        metadata.ModifiedAt = DateTime.Now;
        _metadataService.UpdateNoteMetadata(metadata);

        return metadata;
    }

    /// <summary>
    /// 创建新笔记
    /// </summary>
    public NoteFile CreateNote(string name, string folder = "/", string initialContent = "")
    {
        // 生成初始内容
        if (string.IsNullOrEmpty(initialContent))
        {
            initialContent = $"# {name}\n\n";
        }

        // 计算 Hash（使用内容 + 时间戳确保唯一性）
        var hashSource = initialContent + DateTime.Now.Ticks.ToString();
        var hash = ComputeHash(hashSource);

        // 创建日期目录
        var dateFolder = DateTime.Now.ToString("yyyyMMdd");
        var fullPath = GetFullPath(dateFolder, hash);
        EnsureDirectoryExists(Path.GetDirectoryName(fullPath)!);

        // 写入文件
        File.WriteAllText(fullPath, initialContent);

        // 创建元数据
        var metadata = _metadataService.GetOrCreateNoteMetadata(hash, name, folder);
        metadata.DateFolder = dateFolder;
        _metadataService.UpdateNoteMetadata(metadata);

        return new NoteFile
        {
            Hash = hash,
            Name = name,
            Folder = folder,
            DateFolder = dateFolder,
            Content = initialContent,
            CreatedAt = metadata.CreatedAt,
            ModifiedAt = metadata.ModifiedAt
        };
    }

    /// <summary>
    /// 删除笔记
    /// </summary>
    public bool DeleteNote(string hash)
    {
        var metadata = _metadataService.GetNoteMetadata(hash);
        if (metadata == null)
            return false;

        var fullPath = GetFullPath(metadata.DateFolder, hash);

        // 删除文件
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        // 删除元数据
        _metadataService.DeleteNoteMetadata(hash);
        return true;
    }

    /// <summary>
    /// 重命名笔记（只修改元数据）
    /// </summary>
    public void RenameNote(string hash, string newName)
    {
        _metadataService.RenameNote(hash, newName);
    }

    /// <summary>
    /// 移动笔记到新文件夹（只修改元数据）
    /// </summary>
    public void MoveNote(string hash, string newFolder)
    {
        _metadataService.MoveNote(hash, newFolder);
    }

    /// <summary>
    /// 创建虚拟文件夹
    /// </summary>
    public NoteFolder CreateFolder(string parentPath, string name)
    {
        var newPath = parentPath == "/" ? $"/{name}" : $"{parentPath}/{name}";
        _metadataService.CreateFolder(newPath);
        return NoteFolder.FromPath(newPath);
    }

    /// <summary>
    /// 删除虚拟文件夹
    /// </summary>
    public void DeleteFolder(string path)
    {
        _metadataService.DeleteFolder(path);
    }

    /// <summary>
    /// 重命名虚拟文件夹
    /// </summary>
    public void RenameFolder(string oldPath, string newName)
    {
        var parentPath = oldPath == "/" ? "" : oldPath.Substring(0, oldPath.LastIndexOf('/'));
        if (string.IsNullOrEmpty(parentPath)) parentPath = "/";

        var newPath = parentPath == "/" ? $"/{newName}" : $"{parentPath}/{newName}";
        _metadataService.RenameFolder(oldPath, newPath);
    }

    /// <summary>
    /// 获取指定文件夹下的笔记
    /// </summary>
    public List<NoteFile> GetNotesByFolder(string folder)
    {
        return _metadataService.GetNotesByFolder(folder)
            .Select(NoteFile.FromMetadata)
            .ToList();
    }

    /// <summary>
    /// 检查笔记是否存在
    /// </summary>
    public bool NoteExists(string hash)
    {
        var metadata = _metadataService.GetNoteMetadata(hash);
        if (metadata == null)
            return false;

        var fullPath = GetFullPath(metadata.DateFolder, hash);
        return File.Exists(fullPath);
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
