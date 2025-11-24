using System;
using System.IO;
using System.Threading.Tasks;

namespace MicroNotePlugin.Services;

/// <summary>
/// 单例文件服务，负责读取和写入 Markdown 文件以及插件数据目录的管理。
/// </summary>
public sealed class FileService
{
    private static readonly Lazy<FileService> _instance = new(() => new FileService());
    public static FileService Instance => _instance.Value;

    private readonly string _dataRoot;

    private FileService()
    {
        // 数据根目录位于插件所在目录的子文件夹 MicroNoteData
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _dataRoot = Path.Combine(baseDir, "MicroNoteData");
        if (!Directory.Exists(_dataRoot))
        {
            Directory.CreateDirectory(_dataRoot);
        }
    }

    /// <summary>
    /// 获取完整的文件路径，确保在数据根目录下。
    /// </summary>
    private string ResolvePath(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(_dataRoot, relativePath));
        // 防止路径跳出根目录
        if (!full.StartsWith(_dataRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Attempted to access a path outside of the plugin data folder.");
        }
        return full;
    }

    public async Task<string?> ReadFileAsync(string relativePath)
    {
        var path = ResolvePath(relativePath);
        if (!File.Exists(path))
            return null;
        return await File.ReadAllTextAsync(path);
    }

    public async Task WriteFileAsync(string relativePath, string content)
    {
        var path = ResolvePath(relativePath);
        var dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(path, content);
    }
}
