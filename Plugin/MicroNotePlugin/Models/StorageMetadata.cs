using System.Text.Json.Serialization;

namespace MicroNotePlugin.Models;

/// <summary>
/// 存储元数据 - 统一管理所有笔记和图片的元数据
/// </summary>
public class StorageMetadata
{
    /// <summary>
    /// 笔记元数据字典（key: hash）
    /// </summary>
    [JsonPropertyName("notes")]
    public Dictionary<string, NoteMetadata> Notes { get; set; } = new();

    /// <summary>
    /// 图片元数据字典（key: hash）
    /// </summary>
    [JsonPropertyName("images")]
    public Dictionary<string, ImageMetadata> Images { get; set; } = new();

    /// <summary>
    /// 虚拟文件夹路径列表
    /// </summary>
    [JsonPropertyName("folders")]
    public List<string> Folders { get; set; } = new();

    /// <summary>
    /// 全局标签定义列表
    /// </summary>
    [JsonPropertyName("tagDefinitions")]
    public List<NoteTag> TagDefinitions { get; set; } = new();
}

