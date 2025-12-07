using SQLite;

namespace MicroNotePlugin.Core.Entities;

/// <summary>
/// 图片实体
/// </summary>
[Table("images")]
public class Image
{
    /// <summary>
    /// 图片 ID (GUID)
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 图片内容 Hash (MD5)
    /// </summary>
    [Column("hash")]
    [NotNull]
    [Unique]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME 类型
    /// </summary>
    [Column("mime_type")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// 相对文件路径
    /// </summary>
    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
