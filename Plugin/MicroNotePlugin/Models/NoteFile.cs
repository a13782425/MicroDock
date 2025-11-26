namespace MicroNotePlugin.Models;

/// <summary>
/// 笔记文件模型（运行时使用，包含内容）
/// </summary>
public class NoteFile
{
    /// <summary>
    /// 文件哈希值（MD5，同时作为文件名）
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 虚拟文件夹路径
    /// </summary>
    public string Folder { get; set; } = "/";

    /// <summary>
    /// 创建日期文件夹（格式 yyyyMMdd）
    /// </summary>
    public string DateFolder { get; set; } = string.Empty;

    /// <summary>
    /// 文件内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 获取相对存储路径
    /// </summary>
    public string RelativePath => $"data/note/{DateFolder}/{Hash}";

    /// <summary>
    /// 从元数据创建 NoteFile
    /// </summary>
    public static NoteFile FromMetadata(NoteMetadata metadata)
    {
        return new NoteFile
        {
            Hash = metadata.Hash,
            Name = metadata.Name,
            Folder = metadata.Folder,
            DateFolder = metadata.DateFolder,
            CreatedAt = metadata.CreatedAt,
            ModifiedAt = metadata.ModifiedAt
        };
    }
}
