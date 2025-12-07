using SQLite;

namespace MicroNotePlugin.Core.Entities;

/// <summary>
/// 标签实体
/// </summary>
[Table("tags")]
public class Tag
{
    /// <summary>
    /// 标签 ID (GUID)
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 标签名称
    /// </summary>
    [Column("name")]
    [NotNull]
    [Unique]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标签颜色（十六进制）
    /// </summary>
    [Column("color")]
    public string? Color { get; set; }
}
