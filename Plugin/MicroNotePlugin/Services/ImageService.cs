using System.Security.Cryptography;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using MicroNotePlugin.Models;

namespace MicroNotePlugin.Services;

/// <summary>
/// 图片服务 - 处理图片粘贴和保存
/// 文件使用 MD5 Hash 命名，按日期分目录存储
/// 存储路径: data/images/{yyyyMMdd}/{hash}
/// </summary>
public class ImageService
{
    private readonly string _dataPath;
    private readonly MetadataService _metadataService;

    public ImageService(string dataPath, MetadataService metadataService)
    {
        _dataPath = dataPath;
        _metadataService = metadataService;
        EnsureDirectoryExists(GetImagesDirectory());
    }

    /// <summary>
    /// 图片存储根目录
    /// </summary>
    public string ImagesPath => GetImagesDirectory();

    /// <summary>
    /// 获取图片存储目录
    /// </summary>
    private string GetImagesDirectory()
    {
        return Path.Combine(_dataPath, "images");
    }

    /// <summary>
    /// 获取按日期分类的存储目录
    /// </summary>
    private string GetDateDirectory(string dateFolder)
    {
        return Path.Combine(GetImagesDirectory(), dateFolder);
    }

    /// <summary>
    /// 获取图片的完整存储路径
    /// </summary>
    public string GetFullPath(string dateFolder, string hash)
    {
        return Path.Combine(GetDateDirectory(dateFolder), hash);
    }

    /// <summary>
    /// 获取图片的完整存储路径（从元数据）
    /// </summary>
    public string GetFullPath(ImageMetadata metadata)
    {
        return GetFullPath(metadata.DateFolder, metadata.Hash);
    }

    /// <summary>
    /// 计算数据的 MD5 Hash
    /// </summary>
    public static string ComputeHash(byte[] data)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(data);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 从剪贴板获取图片并保存
    /// </summary>
    /// <param name="clipboard">剪贴板对象（从 TopLevel.GetTopLevel(control)?.Clipboard 获取）</param>
    /// <returns>保存后的相对路径，如果没有图片则返回 null</returns>
    public async Task<string?> SaveImageFromClipboardAsync(IClipboard? clipboard)
    {
        if (clipboard == null)
            return null;

        try
        {
            // 获取剪贴板数据格式
            var formats = await clipboard.GetFormatsAsync();
            if (formats == null)
                return null;

            // 检查是否有图片
            byte[]? imageData = null;
            string mimeType = "image/png";
            string originalName = "clipboard";

            if (formats.Contains("image/png"))
            {
                imageData = await clipboard.GetDataAsync("image/png") as byte[];
                mimeType = "image/png";
            }
            else if (formats.Contains("image/jpeg"))
            {
                imageData = await clipboard.GetDataAsync("image/jpeg") as byte[];
                mimeType = "image/jpeg";
            }
            else if (formats.Contains("image/bmp"))
            {
                imageData = await clipboard.GetDataAsync("image/bmp") as byte[];
                mimeType = "image/bmp";
            }
            else
            {
                // 尝试获取文件路径
                var data = await clipboard.GetDataAsync("Files");
                if (data is IEnumerable<IStorageFile> files)
                {
                    var imageFile = files.FirstOrDefault(f => IsImageFile(f.Name));
                    if (imageFile != null)
                    {
                        return await CopyImageFileAsync(imageFile);
                    }
                }
                return null;
            }

            if (imageData != null && imageData.Length > 0)
            {
                return await SaveImageDataAsync(imageData, mimeType, originalName);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 保存图片数据
    /// </summary>
    public async Task<string> SaveImageDataAsync(byte[] imageData, string mimeType, string originalName = "image")
    {
        // 计算 Hash
        var hash = ComputeHash(imageData);

        // 检查是否已存在
        var existingMetadata = _metadataService.GetImageMetadata(hash);
        if (existingMetadata != null)
        {
            // 已存在相同图片，直接返回路径
            return existingMetadata.RelativePath;
        }

        // 创建日期目录
        var dateFolder = DateTime.Now.ToString("yyyyMMdd");
        var fullPath = GetFullPath(dateFolder, hash);
        EnsureDirectoryExists(Path.GetDirectoryName(fullPath)!);

        // 写入文件
        await File.WriteAllBytesAsync(fullPath, imageData);

        // 创建元数据
        var metadata = _metadataService.AddImageMetadata(hash, originalName, mimeType, imageData.Length);

        return metadata.RelativePath;
    }

    /// <summary>
    /// 保存图片文件
    /// </summary>
    public async Task<string> SaveImageFileAsync(string sourceFilePath)
    {
        var imageData = await File.ReadAllBytesAsync(sourceFilePath);
        var mimeType = GetMimeType(sourceFilePath);
        var originalName = Path.GetFileNameWithoutExtension(sourceFilePath);

        return await SaveImageDataAsync(imageData, mimeType, originalName);
    }

    /// <summary>
    /// 复制图片文件
    /// </summary>
    private async Task<string?> CopyImageFileAsync(IStorageFile file)
    {
        try
        {
            await using var source = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await source.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            var mimeType = GetMimeType(file.Name);
            var originalName = Path.GetFileNameWithoutExtension(file.Name);

            return await SaveImageDataAsync(imageData, mimeType, originalName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 读取图片数据
    /// </summary>
    public async Task<byte[]?> ReadImageDataAsync(string hash)
    {
        var metadata = _metadataService.GetImageMetadata(hash);
        if (metadata == null)
            return null;

        var fullPath = GetFullPath(metadata.DateFolder, hash);
        if (!File.Exists(fullPath))
            return null;

        return await File.ReadAllBytesAsync(fullPath);
    }

    /// <summary>
    /// 删除图片
    /// </summary>
    public bool DeleteImage(string hash)
    {
        var metadata = _metadataService.GetImageMetadata(hash);
        if (metadata == null)
            return false;

        var fullPath = GetFullPath(metadata.DateFolder, hash);

        // 删除文件
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        // 删除元数据
        _metadataService.DeleteImageMetadata(hash);
        return true;
    }

    /// <summary>
    /// 检查是否是图片文件
    /// </summary>
    private bool IsImageFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp";
    }

    /// <summary>
    /// 根据文件名获取 MIME 类型
    /// </summary>
    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// 获取图片的 Markdown 语法
    /// </summary>
    public static string GetMarkdownImageSyntax(string relativePath, string? altText = null)
    {
        var alt = altText ?? "图片";
        return $"![{alt}]({relativePath})";
    }

    /// <summary>
    /// 清理未使用的图片
    /// </summary>
    /// <param name="usedHashes">正在使用的图片 Hash 列表</param>
    /// <returns>删除的图片数量</returns>
    public int CleanupUnusedImages(IEnumerable<string> usedHashes)
    {
        var count = 0;
        var usedSet = new HashSet<string>(usedHashes, StringComparer.OrdinalIgnoreCase);

        foreach (var metadata in _metadataService.GetAllImageMetadata().ToList())
        {
            if (!usedSet.Contains(metadata.Hash))
            {
                DeleteImage(metadata.Hash);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 获取所有图片元数据
    /// </summary>
    public IEnumerable<ImageMetadata> GetAllImages()
    {
        return _metadataService.GetAllImageMetadata();
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
