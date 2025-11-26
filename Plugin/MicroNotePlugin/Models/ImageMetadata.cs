using System.Text.Json.Serialization;

namespace MicroNotePlugin.Models;

/// <summary>
/// 图片元数据模型
/// </summary>
public class ImageMetadata
{
    /// <summary>
    /// 文件哈希值（MD5，同时作为文件名）
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// MIME 类型（如 image/png, image/jpeg）
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// 创建日期（用于存储路径，格式 yyyyMMdd）
    /// </summary>
    [JsonPropertyName("dateFolder")]
    public string DateFolder { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 获取相对存储路径
    /// </summary>
    [JsonIgnore]
    public string RelativePath => $"data/images/{DateFolder}/{Hash}";
}

