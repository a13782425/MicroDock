using SQLite;

namespace MicroNotePlugin.Entities;

/// <summary>
/// 文件夹实体
/// </summary>
[Table("folders")]
public class Folder
{
    /// <summary>
    /// 文件夹 ID (GUID)
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 文件夹名称
    /// </summary>
    [Column("name")]
    [NotNull]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父文件夹 ID
    /// </summary>
    [Column("parent_id")]
    [Indexed]
    public string? ParentId { get; set; }

    /// <summary>
    /// 完整路径（如 /Documents/Notes）
    /// </summary>
    [Column("path")]
    [Indexed]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
