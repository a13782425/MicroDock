using SQLite;

namespace MicroNotePlugin.Core.Entities;

/// <summary>
/// 笔记版本实体
/// </summary>
[Table("note_versions")]
public class NoteVersion
{
    /// <summary>
    /// 版本 ID (GUID)
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 笔记 ID
    /// </summary>
    [Column("note_id")]
    [NotNull]
    [Indexed]
    public string NoteId { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    [Column("version")]
    public int Version { get; set; }

    /// <summary>
    /// 该版本的内容
    /// </summary>
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
