using System.Text.Json.Serialization;

namespace MicroNotePlugin.Models;

/// <summary>
/// 笔记标签模型
/// </summary>
public class NoteTag
{
    /// <summary>
    /// 标签名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标签颜色（十六进制）
    /// </summary>
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#808080";
}

/// <summary>
/// 笔记元数据模型（用于存储收藏、打开次数等信息）
/// 以 Hash 为主键，支持虚拟文件夹
/// </summary>
public class NoteMetadata
{
    /// <summary>
    /// 文件哈希值（MD5，同时作为文件名）
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 笔记显示名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 虚拟文件夹路径（如 "/工作/项目"）
    /// </summary>
    [JsonPropertyName("folder")]
    public string Folder { get; set; } = "/";

    /// <summary>
    /// 创建日期（用于存储路径，格式 yyyyMMdd）
    /// </summary>
    [JsonPropertyName("dateFolder")]
    public string DateFolder { get; set; } = string.Empty;

    /// <summary>
    /// 是否收藏
    /// </summary>
    [JsonPropertyName("favorite")]
    public bool IsFavorite { get; set; }

    /// <summary>
    /// 打开次数
    /// </summary>
    [JsonPropertyName("openCount")]
    public int OpenCount { get; set; }

    /// <summary>
    /// 最后打开时间
    /// </summary>
    [JsonPropertyName("lastOpened")]
    public DateTime? LastOpenedAt { get; set; }

    /// <summary>
    /// 标签列表
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 修改时间
    /// </summary>
    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 获取相对存储路径
    /// </summary>
    [JsonIgnore]
    public string RelativePath => $"data/note/{DateFolder}/{Hash}";
}
