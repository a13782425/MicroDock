using System.Text.Json;
using MicroNotePlugin.Models;

namespace MicroNotePlugin.Services;

/// <summary>
/// 元数据服务 - 负责管理笔记和图片的元数据
/// 使用 JSON 文件存储，以 Hash 为主键
/// </summary>
public class MetadataService
{
    private const string MetadataFileName = "metadata.json";
    private readonly string _metadataFilePath;
    private readonly string _dataPath;
    private StorageMetadata _metadata;
    private readonly JsonSerializerOptions _jsonOptions;

    public MetadataService(string dataPath)
    {
        _dataPath = dataPath;
        _metadataFilePath = Path.Combine(dataPath, MetadataFileName);
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _metadata = LoadMetadata();
    }

    /// <summary>
    /// 数据根目录
    /// </summary>
    public string DataPath => _dataPath;

    #region 笔记元数据管理

    /// <summary>
    /// 根据 Hash 获取笔记元数据
    /// </summary>
    public NoteMetadata? GetNoteMetadata(string hash)
    {
        return _metadata.Notes.TryGetValue(hash, out var metadata) ? metadata : null;
    }

    /// <summary>
    /// 获取或创建笔记元数据
    /// </summary>
    public NoteMetadata GetOrCreateNoteMetadata(string hash, string name, string folder = "/")
    {
        if (!_metadata.Notes.TryGetValue(hash, out var metadata))
        {
            metadata = new NoteMetadata
            {
                Hash = hash,
                Name = name,
                Folder = folder,
                DateFolder = DateTime.Now.ToString("yyyyMMdd"),
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };
            _metadata.Notes[hash] = metadata;
            EnsureFolderExists(folder);
            SaveMetadata();
        }
        return metadata;
    }

    /// <summary>
    /// 更新笔记元数据
    /// </summary>
    public void UpdateNoteMetadata(NoteMetadata metadata)
    {
        metadata.ModifiedAt = DateTime.Now;
        _metadata.Notes[metadata.Hash] = metadata;
        SaveMetadata();
    }

    /// <summary>
    /// 删除笔记元数据
    /// </summary>
    public void DeleteNoteMetadata(string hash)
    {
        if (_metadata.Notes.Remove(hash))
        {
            SaveMetadata();
        }
    }

    /// <summary>
    /// 获取所有笔记元数据
    /// </summary>
    public IEnumerable<NoteMetadata> GetAllNoteMetadata()
    {
        return _metadata.Notes.Values;
    }

    /// <summary>
    /// 切换收藏状态
    /// </summary>
    public bool ToggleFavorite(string hash)
    {
        if (_metadata.Notes.TryGetValue(hash, out var metadata))
        {
            metadata.IsFavorite = !metadata.IsFavorite;
            SaveMetadata();
            return metadata.IsFavorite;
        }
        return false;
    }

    /// <summary>
    /// 设置收藏状态
    /// </summary>
    public void SetFavorite(string hash, bool isFavorite)
    {
        if (_metadata.Notes.TryGetValue(hash, out var metadata))
        {
            metadata.IsFavorite = isFavorite;
            SaveMetadata();
        }
    }

    /// <summary>
    /// 是否已收藏
    /// </summary>
    public bool IsFavorite(string hash)
    {
        return _metadata.Notes.TryGetValue(hash, out var metadata) && metadata.IsFavorite;
    }

    /// <summary>
    /// 记录打开笔记
    /// </summary>
    public void RecordOpen(string hash)
    {
        if (_metadata.Notes.TryGetValue(hash, out var metadata))
        {
            metadata.OpenCount++;
            metadata.LastOpenedAt = DateTime.Now;
            SaveMetadata();
        }
    }

    /// <summary>
    /// 获取打开次数
    /// </summary>
    public int GetOpenCount(string hash)
    {
        return _metadata.Notes.TryGetValue(hash, out var metadata) ? metadata.OpenCount : 0;
    }

    /// <summary>
    /// 获取所有收藏的笔记
    /// </summary>
    public List<NoteMetadata> GetFavorites()
    {
        return _metadata.Notes.Values
            .Where(m => m.IsFavorite)
            .ToList();
    }

    /// <summary>
    /// 获取常用笔记（按打开次数排序）
    /// </summary>
    public List<NoteMetadata> GetFrequentlyUsed(int count = 10)
    {
        return _metadata.Notes.Values
            .Where(m => m.OpenCount > 0)
            .OrderByDescending(m => m.OpenCount)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// 更新笔记的虚拟文件夹
    /// </summary>
    public void MoveNote(string hash, string newFolder)
    {
        if (_metadata.Notes.TryGetValue(hash, out var metadata))
        {
            metadata.Folder = newFolder;
            EnsureFolderExists(newFolder);
            SaveMetadata();
        }
    }

    /// <summary>
    /// 重命名笔记
    /// </summary>
    public void RenameNote(string hash, string newName)
    {
        if (_metadata.Notes.TryGetValue(hash, out var metadata))
        {
            metadata.Name = newName;
            metadata.ModifiedAt = DateTime.Now;
            SaveMetadata();
        }
    }

    /// <summary>
    /// 获取指定文件夹下的笔记
    /// </summary>
    public List<NoteMetadata> GetNotesByFolder(string folder)
    {
        return _metadata.Notes.Values
            .Where(m => m.Folder == folder)
            .ToList();
    }

    #endregion

    #region 图片元数据管理

    /// <summary>
    /// 根据 Hash 获取图片元数据
    /// </summary>
    public ImageMetadata? GetImageMetadata(string hash)
    {
        return _metadata.Images.TryGetValue(hash, out var metadata) ? metadata : null;
    }

    /// <summary>
    /// 添加图片元数据
    /// </summary>
    public ImageMetadata AddImageMetadata(string hash, string name, string mimeType, long size)
    {
        var metadata = new ImageMetadata
        {
            Hash = hash,
            Name = name,
            MimeType = mimeType,
            DateFolder = DateTime.Now.ToString("yyyyMMdd"),
            Size = size,
            CreatedAt = DateTime.Now
        };
        _metadata.Images[hash] = metadata;
        SaveMetadata();
        return metadata;
    }

    /// <summary>
    /// 删除图片元数据
    /// </summary>
    public void DeleteImageMetadata(string hash)
    {
        if (_metadata.Images.Remove(hash))
        {
            SaveMetadata();
        }
    }

    /// <summary>
    /// 获取所有图片元数据
    /// </summary>
    public IEnumerable<ImageMetadata> GetAllImageMetadata()
    {
        return _metadata.Images.Values;
    }

    #endregion

    #region 虚拟文件夹管理

    /// <summary>
    /// 获取所有虚拟文件夹
    /// </summary>
    public List<string> GetAllFolders()
    {
        return _metadata.Folders.ToList();
    }

    /// <summary>
    /// 创建虚拟文件夹
    /// </summary>
    public void CreateFolder(string path)
    {
        if (!_metadata.Folders.Contains(path))
        {
            _metadata.Folders.Add(path);
            // 确保父文件夹也存在
            EnsureParentFoldersExist(path);
            SaveMetadata();
        }
    }

    /// <summary>
    /// 删除虚拟文件夹（包括子文件夹）
    /// </summary>
    public void DeleteFolder(string path)
    {
        // 删除文件夹及其子文件夹
        var foldersToDelete = _metadata.Folders
            .Where(f => f == path || f.StartsWith(path + "/"))
            .ToList();
        
        foreach (var folder in foldersToDelete)
        {
            _metadata.Folders.Remove(folder);
        }

        // 将该文件夹下的笔记移动到根目录
        var notesToMove = _metadata.Notes.Values
            .Where(n => n.Folder == path || n.Folder.StartsWith(path + "/"))
            .ToList();

        foreach (var note in notesToMove)
        {
            note.Folder = "/";
        }

        if (foldersToDelete.Count > 0 || notesToMove.Count > 0)
        {
            SaveMetadata();
        }
    }

    /// <summary>
    /// 重命名虚拟文件夹
    /// </summary>
    public void RenameFolder(string oldPath, string newPath)
    {
        var index = _metadata.Folders.IndexOf(oldPath);
        if (index >= 0)
        {
            _metadata.Folders[index] = newPath;

            // 更新子文件夹路径
            for (int i = 0; i < _metadata.Folders.Count; i++)
            {
                if (_metadata.Folders[i].StartsWith(oldPath + "/"))
                {
                    _metadata.Folders[i] = newPath + _metadata.Folders[i].Substring(oldPath.Length);
                }
            }

            // 更新笔记的文件夹路径
            foreach (var note in _metadata.Notes.Values)
            {
                if (note.Folder == oldPath)
                {
                    note.Folder = newPath;
                }
                else if (note.Folder.StartsWith(oldPath + "/"))
                {
                    note.Folder = newPath + note.Folder.Substring(oldPath.Length);
                }
            }

            SaveMetadata();
        }
    }

    /// <summary>
    /// 获取子文件夹
    /// </summary>
    public List<string> GetSubFolders(string parentPath)
    {
        var prefix = parentPath == "/" ? "/" : parentPath + "/";
        return _metadata.Folders
            .Where(f => f.StartsWith(prefix) && f != parentPath)
            .Where(f => 
            {
                var remaining = f.Substring(prefix.Length);
                return !remaining.Contains('/'); // 只返回直接子文件夹
            })
            .ToList();
    }

    private void EnsureFolderExists(string path)
    {
        if (path != "/" && !_metadata.Folders.Contains(path))
        {
            _metadata.Folders.Add(path);
            EnsureParentFoldersExist(path);
        }
    }

    private void EnsureParentFoldersExist(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
            return;

        var parts = path.TrimStart('/').Split('/');
        var currentPath = "";
        
        for (int i = 0; i < parts.Length - 1; i++)
        {
            currentPath += "/" + parts[i];
            if (!_metadata.Folders.Contains(currentPath))
            {
                _metadata.Folders.Add(currentPath);
            }
        }
    }

    #endregion

    #region 标签管理

    /// <summary>
    /// 获取所有标签定义
    /// </summary>
    public List<NoteTag> GetAllTags()
    {
        return _metadata.TagDefinitions.ToList();
    }

    /// <summary>
    /// 创建新标签
    /// </summary>
    public NoteTag CreateTag(string name, string color = "#808080")
    {
        var existing = _metadata.TagDefinitions.FirstOrDefault(t => 
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            return existing;
        }

        var tag = new NoteTag { Name = name, Color = color };
        _metadata.TagDefinitions.Add(tag);
        SaveMetadata();
        return tag;
    }

    /// <summary>
    /// 删除标签定义（同时从所有笔记中移除）
    /// </summary>
    public void DeleteTag(string name)
    {
        var tag = _metadata.TagDefinitions.FirstOrDefault(t => 
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (tag != null)
        {
            _metadata.TagDefinitions.Remove(tag);

            foreach (var note in _metadata.Notes.Values)
            {
                note.Tags.RemoveAll(t => t.Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            SaveMetadata();
        }
    }

    /// <summary>
    /// 为笔记添加标签
    /// </summary>
    public void AddTagToNote(string hash, string tagName)
    {
        if (_metadata.Notes.TryGetValue(hash, out var metadata))
        {
            // 确保标签存在
            if (!_metadata.TagDefinitions.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
            {
                CreateTag(tagName);
            }

            if (!metadata.Tags.Contains(tagName, StringComparer.OrdinalIgnoreCase))
            {
                metadata.Tags.Add(tagName);
                SaveMetadata();
            }
        }
    }

    /// <summary>
    /// 从笔记移除标签
    /// </summary>
    public void RemoveTagFromNote(string hash, string tagName)
    {
        if (_metadata.Notes.TryGetValue(hash, out var metadata))
        {
            metadata.Tags.RemoveAll(t => t.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            SaveMetadata();
        }
    }

    /// <summary>
    /// 获取笔记的所有标签
    /// </summary>
    public List<string> GetNoteTags(string hash)
    {
        return _metadata.Notes.TryGetValue(hash, out var metadata) 
            ? metadata.Tags.ToList() 
            : new List<string>();
    }

    /// <summary>
    /// 获取带有指定标签的所有笔记
    /// </summary>
    public List<NoteMetadata> GetNotesByTag(string tagName)
    {
        return _metadata.Notes.Values
            .Where(m => m.Tags.Contains(tagName, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// 获取标签定义
    /// </summary>
    public NoteTag? GetTagDefinition(string tagName)
    {
        return _metadata.TagDefinitions.FirstOrDefault(t => 
            t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region 数据持久化

    private StorageMetadata LoadMetadata()
    {
        if (!File.Exists(_metadataFilePath))
        {
            return new StorageMetadata();
        }

        try
        {
            var json = File.ReadAllText(_metadataFilePath);
            return JsonSerializer.Deserialize<StorageMetadata>(json, _jsonOptions) ?? new StorageMetadata();
        }
        catch
        {
            return new StorageMetadata();
        }
    }

    private void SaveMetadata()
    {
        try
        {
            var directory = Path.GetDirectoryName(_metadataFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_metadata, _jsonOptions);
            File.WriteAllText(_metadataFilePath, json);
        }
        catch
        {
            // 忽略保存错误
        }
    }

    /// <summary>
    /// 强制保存元数据
    /// </summary>
    public void ForceSave()
    {
        SaveMetadata();
    }

    /// <summary>
    /// 重新加载元数据
    /// </summary>
    public void Reload()
    {
        _metadata = LoadMetadata();
    }

    #endregion
}
