using MicroNotePlugin.Core.Entities;

namespace MicroNotePlugin.Core.Interfaces;

/// <summary>
/// 笔记仓储接口
/// </summary>
public interface INoteRepository
{
    /// <summary>
    /// 根据 ID 获取笔记
    /// </summary>
    Task<Note?> GetByIdAsync(string id);

    /// <summary>
    /// 获取所有笔记
    /// </summary>
    Task<IEnumerable<Note>> GetAllAsync();

    /// <summary>
    /// 根据文件夹 ID 获取笔记列表
    /// </summary>
    Task<IEnumerable<Note>> GetByFolderIdAsync(string? folderId);

    /// <summary>
    /// 获取收藏的笔记
    /// </summary>
    Task<IEnumerable<Note>> GetFavoritesAsync();

    /// <summary>
    /// 获取常用笔记 (按打开次数排序)
    /// </summary>
    Task<IEnumerable<Note>> GetFrequentAsync(int limit = 10);

    /// <summary>
    /// 创建笔记
    /// </summary>
    Task<Note> CreateAsync(Note note);

    /// <summary>
    /// 更新笔记
    /// </summary>
    Task UpdateAsync(Note note);

    /// <summary>
    /// 删除笔记
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// 切换收藏状态
    /// </summary>
    Task<bool> ToggleFavoriteAsync(string id);

    /// <summary>
    /// 记录打开
    /// </summary>
    Task RecordOpenAsync(string id);

    /// <summary>
    /// 移动笔记到指定文件夹
    /// </summary>
    Task MoveAsync(string id, string? targetFolderId);

    /// <summary>
    /// 重命名笔记
    /// </summary>
    Task RenameAsync(string id, string newName);

    /// <summary>
    /// 根据标签获取笔记
    /// </summary>
    Task<IEnumerable<Note>> GetByTagAsync(string tagId);

    /// <summary>
    /// 添加标签到笔记
    /// </summary>
    Task AddTagAsync(string noteId, string tagId);

    /// <summary>
    /// 从笔记移除标签
    /// </summary>
    Task RemoveTagAsync(string noteId, string tagId);

    /// <summary>
    /// 获取笔记的所有标签
    /// </summary>
    Task<IEnumerable<Tag>> GetTagsAsync(string noteId);
}
