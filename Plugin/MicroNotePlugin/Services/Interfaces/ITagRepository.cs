using MicroNotePlugin.Entities;

namespace MicroNotePlugin.Services;

/// <summary>
/// 标签仓储接口
/// </summary>
public interface ITagRepository
{
    /// <summary>
    /// 根据 ID 获取标签
    /// </summary>
    Task<Tag?> GetByIdAsync(string id);

    /// <summary>
    /// 根据名称获取标签
    /// </summary>
    Task<Tag?> GetByNameAsync(string name);

    /// <summary>
    /// 获取所有标签
    /// </summary>
    Task<IEnumerable<Tag>> GetAllAsync();

    /// <summary>
    /// 创建标签
    /// </summary>
    Task<Tag> CreateAsync(Tag tag);

    /// <summary>
    /// 创建标签 (通过名称和颜色)
    /// </summary>
    Task<Tag> CreateAsync(string name, string color = "#808080");

    /// <summary>
    /// 更新标签
    /// </summary>
    Task UpdateAsync(Tag tag);

    /// <summary>
    /// 删除标签
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// 获取或创建标签 (如果不存在则创建)
    /// </summary>
    Task<Tag> GetOrCreateAsync(string name, string color = "#808080");
}
