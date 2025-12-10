using Avalonia.Input.Platform;
using MicroNotePlugin.Entities;

namespace MicroNotePlugin.Services;

/// <summary>
/// 图片服务接口
/// </summary>
public interface IImageService
{
    /// <summary>
    /// 根据 ID 获取图片
    /// </summary>
    Task<Image?> GetByIdAsync(string id);

    /// <summary>
    /// 根据 Hash 获取图片
    /// </summary>
    Task<Image?> GetByHashAsync(string hash);

    /// <summary>
    /// 获取所有图片
    /// </summary>
    Task<IEnumerable<Image>> GetAllAsync();

    /// <summary>
    /// 从剪贴板保存图片
    /// </summary>
    /// <param name="clipboard">剪贴板对象</param>
    /// <returns>保存的图片，如果剪贴板没有图片则返回 null</returns>
    Task<Image?> SaveFromClipboardAsync(IClipboard clipboard);

    /// <summary>
    /// 保存图片数据
    /// </summary>
    /// <param name="data">图片二进制数据</param>
    /// <param name="fileName">原始文件名</param>
    /// <param name="mimeType">MIME 类型</param>
    /// <returns>保存的图片</returns>
    Task<Image> SaveAsync(byte[] data, string fileName, string mimeType);

    /// <summary>
    /// 读取图片数据
    /// </summary>
    Task<byte[]?> ReadDataAsync(string id);

    /// <summary>
    /// 删除图片
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// 获取图片的 Markdown 语法
    /// </summary>
    string GetMarkdownSyntax(Image image, string? altText = null);

    /// <summary>
    /// 获取图片的完整文件路径
    /// </summary>
    string GetFullPath(Image image);

    /// <summary>
    /// 获取图片根目录路径
    /// </summary>
    string GetImagesRootPath();

    /// <summary>
    /// 清理未使用的图片
    /// </summary>
    /// <param name="usedHashes">正在使用的图片 Hash 集合</param>
    /// <returns>删除的图片数量</returns>
    Task<int> CleanupUnusedAsync(IEnumerable<string> usedHashes);
}
