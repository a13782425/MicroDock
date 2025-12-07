using SQLite;

namespace MicroNotePlugin.Core.Entities;

/// <summary>
/// 笔记实体
/// </summary>
[Table("notes")]
public class Note
{
    /// <summary>
    /// 笔记 ID (GUID)
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 笔记名称
    /// </summary>
    [Column("name")]
    [NotNull]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 所属文件夹 ID
    /// </summary>
    [Column("folder_id")]
    [Indexed]
    public string? FolderId { get; set; }

    /// <summary>
    /// Markdown 内容
    /// </summary>
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 是否收藏
    /// </summary>
    [Column("is_favorite")]
    [Indexed]
    public bool IsFavorite { get; set; }

    /// <summary>
    /// 打开次数
    /// </summary>
    [Column("open_count")]
    public int OpenCount { get; set; }

    /// <summary>
    /// 最后打开时间
    /// </summary>
    [Column("last_opened_at")]
    public DateTime? LastOpenedAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 修改时间
    /// </summary>
    [Column("modified_at")]
    [Indexed]
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
}
