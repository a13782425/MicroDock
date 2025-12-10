using MicroNotePlugin.Entities;

namespace MicroNotePlugin.Services;

/// <summary>
/// 文件夹仓储接口
/// </summary>
public interface IFolderRepository
{
    /// <summary>
    /// 根据 ID 获取文件夹
    /// </summary>
    Task<Folder?> GetByIdAsync(string id);

    /// <summary>
    /// 根据路径获取文件夹
    /// </summary>
    Task<Folder?> GetByPathAsync(string path);

    /// <summary>
    /// 获取所有文件夹
    /// </summary>
    Task<IEnumerable<Folder>> GetAllAsync();

    /// <summary>
    /// 获取子文件夹
    /// </summary>
    Task<IEnumerable<Folder>> GetChildrenAsync(string? parentId);

    /// <summary>
    /// 创建文件夹
    /// </summary>
    Task<Folder> CreateAsync(Folder folder);

    /// <summary>
    /// 创建文件夹 (通过名称和父文件夹)
    /// </summary>
    Task<Folder> CreateAsync(string name, string? parentId);

    /// <summary>
    /// 更新文件夹
    /// </summary>
    Task UpdateAsync(Folder folder);

    /// <summary>
    /// 删除文件夹 (及其子文件夹)
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// 重命名文件夹
    /// </summary>
    Task RenameAsync(string id, string newName);

    /// <summary>
    /// 移动文件夹
    /// </summary>
    Task MoveAsync(string id, string? newParentId);

    /// <summary>
    /// 确保文件夹路径存在 (自动创建中间文件夹)
    /// </summary>
    Task<Folder> EnsurePathExistsAsync(string path);
}
