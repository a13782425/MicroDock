using System.Security.Cryptography;
using Avalonia.Input.Platform;
using MicroNotePlugin.Entities;
using MicroNotePlugin.Database;

namespace MicroNotePlugin.Services;

/// <summary>
/// SQLite 图片仓储实现
/// </summary>
public class SqliteImageRepository : IImageService
{
    private readonly NoteDbContext _context;
    private readonly string _imagesPath;

    public SqliteImageRepository(NoteDbContext context, string dataPath)
    {
        _context = context;
        _imagesPath = Path.Combine(dataPath, "images");
        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_imagesPath))
        {
            Directory.CreateDirectory(_imagesPath);
        }
    }

    public async Task<Image?> GetByIdAsync(string id)
    {
        return await _context.Connection.FindAsync<Image>(id);
    }

    public async Task<Image?> GetByHashAsync(string hash)
    {
        return await _context.Connection.Table<Image>()
            .Where(i => i.Hash == hash)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Image>> GetAllAsync()
    {
        return await _context.Connection.Table<Image>()
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Image?> SaveFromClipboardAsync(IClipboard clipboard)
    {
        // 尝试从剪贴板获取文件 (使用旧 API)
#pragma warning disable CS0618
        var formats = await clipboard.GetFormatsAsync();
        if (formats == null) return null;

        // 检查是否有图片数据
        if (formats.Contains("image/png") || formats.Contains("PNG"))
        {
            var data = await clipboard.GetDataAsync("image/png") as byte[];
            if (data != null)
            {
                return await SaveAsync(data, "clipboard.png", "image/png");
            }
        }

        // 检查是否有文件
        if (formats.Contains("Files"))
        {
            var files = await clipboard.GetDataAsync("Files") as IEnumerable<Avalonia.Platform.Storage.IStorageItem>;
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (file is Avalonia.Platform.Storage.IStorageFile storageFile)
                    {
                        var path = storageFile.Path.LocalPath;
                        if (IsImageFile(path))
                        {
                            var fileData = await File.ReadAllBytesAsync(path);
                            var mimeType = GetMimeType(path);
                            return await SaveAsync(fileData, Path.GetFileName(path), mimeType);
                        }
                    }
                }
            }
        }
#pragma warning restore CS0618

        return null;
    }

    public async Task<Image> SaveAsync(byte[] data, string fileName, string mimeType)
    {
        var hash = ComputeHash(data);

        // 检查是否已存在相同 hash 的图片
        var existing = await GetByHashAsync(hash);
        if (existing != null)
        {
            return existing;
        }

        // 生成文件路径
        var extension = GetExtensionFromMimeType(mimeType);
        var dateFolder = DateTime.Now.ToString("yyyyMMdd");
        var relativePath = Path.Combine(dateFolder, $"{hash}{extension}");
        var fullPath = Path.Combine(_imagesPath, relativePath);

        // 确保目录存在
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // 保存文件
        await File.WriteAllBytesAsync(fullPath, data);

        // 保存元数据
        var image = new Image
        {
            Hash = hash,
            FileName = fileName,
            MimeType = mimeType,
            FilePath = relativePath,
            CreatedAt = DateTime.Now
        };

        await _context.Connection.InsertAsync(image);
        return image;
    }

    public async Task<byte[]?> ReadDataAsync(string id)
    {
        var image = await GetByIdAsync(id);
        if (image == null) return null;

        var fullPath = GetFullPath(image);
        if (!File.Exists(fullPath)) return null;

        return await File.ReadAllBytesAsync(fullPath);
    }

    public async Task DeleteAsync(string id)
    {
        var image = await GetByIdAsync(id);
        if (image == null) return;

        // 删除文件
        var fullPath = GetFullPath(image);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        // 删除数据库记录
        await _context.Connection.DeleteAsync(image);
    }

    public string GetMarkdownSyntax(Image image, string? altText = null)
    {
        var fullPath = GetFullPath(image);
        var alt = altText ?? image.FileName;
        return $"![{alt}]({fullPath})";
    }

    public string GetFullPath(Image image)
    {
        return Path.Combine(_imagesPath, image.FilePath);
    }

    public string GetImagesRootPath()
    {
        return _imagesPath;
    }

    public async Task<int> CleanupUnusedAsync(IEnumerable<string> usedHashes)
    {
        var usedHashSet = new HashSet<string>(usedHashes);
        var allImages = await GetAllAsync();
        var deleteCount = 0;

        foreach (var image in allImages)
        {
            if (!usedHashSet.Contains(image.Hash))
            {
                await DeleteAsync(image.Id);
                deleteCount++;
            }
        }

        return deleteCount;
    }

    private static string ComputeHash(byte[] data)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static bool IsImageFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp";
    }

    private static string GetMimeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string GetExtensionFromMimeType(string mimeType)
    {
        return mimeType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/webp" => ".webp",
            _ => ".bin"
        };
    }
}
