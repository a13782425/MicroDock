using MicroNotePlugin.Core.Entities;

namespace MicroNotePlugin.Core.Interfaces;

/// <summary>
/// 版本历史服务接口
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// 创建版本快照
    /// </summary>
    /// <param name="noteId">笔记 ID</param>
    /// <param name="content">内容</param>
    Task<NoteVersion> CreateVersionAsync(string noteId, string content);

    /// <summary>
    /// 获取笔记的所有版本
    /// </summary>
    Task<IEnumerable<NoteVersion>> GetVersionsAsync(string noteId);

    /// <summary>
    /// 获取指定版本
    /// </summary>
    Task<NoteVersion?> GetVersionAsync(string versionId);

    /// <summary>
    /// 获取笔记的最新版本号
    /// </summary>
    Task<int> GetLatestVersionNumberAsync(string noteId);

    /// <summary>
    /// 恢复到指定版本
    /// </summary>
    /// <param name="noteId">笔记 ID</param>
    /// <param name="versionId">目标版本 ID</param>
    /// <returns>恢复后的内容</returns>
    Task<string> RestoreVersionAsync(string noteId, string versionId);

    /// <summary>
    /// 清理旧版本 (保留最近 N 个)
    /// </summary>
    Task CleanupOldVersionsAsync(string noteId, int keepCount = 50);

    /// <summary>
    /// 删除笔记的所有版本
    /// </summary>
    Task DeleteAllVersionsAsync(string noteId);
}
