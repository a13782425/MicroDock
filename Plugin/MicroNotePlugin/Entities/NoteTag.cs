using SQLite;

namespace MicroNotePlugin.Entities;

/// <summary>
/// 笔记-标签关联实体
/// </summary>
[Table("note_tags")]
public class NoteTag
{
    /// <summary>
    /// 笔记 ID
    /// </summary>
    [Column("note_id")]
    [Indexed]
    public string NoteId { get; set; } = string.Empty;

    /// <summary>
    /// 标签 ID
    /// </summary>
    [Column("tag_id")]
    [Indexed]
    public string TagId { get; set; } = string.Empty;
}
