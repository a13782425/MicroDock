using SQLite;
using System;

namespace MicroDock.Database;

public class IconDB
{
    [PrimaryKey]
    public string Sha256Hash { get; set; } = string.Empty;
    
    public byte[] IconData { get; set; } = Array.Empty<byte>();
    
    public int ReferenceCount { get; set; }
    
    /// <summary>
    /// 创建时间戳（从2025年1月1日开始的毫秒数）
    /// </summary>
    public long CreatedAt { get; set; }
    
    /// <summary>
    /// 最后访问时间戳（从2025年1月1日开始的毫秒数）
    /// </summary>
    [Indexed]
    public long LastAccessedAt { get; set; }

    /// <summary>
    /// 创建时间（DateTime 包装器）
    /// </summary>
    [Ignore]
    public DateTime CreatedAtDateTime
    {
        get => TimeStampHelper.ToDateTime(CreatedAt);
        set => CreatedAt = TimeStampHelper.ToTimestamp(value);
    }

    /// <summary>
    /// 最后访问时间（DateTime 包装器）
    /// </summary>
    [Ignore]
    public DateTime LastAccessedAtDateTime
    {
        get => TimeStampHelper.ToDateTime(LastAccessedAt);
        set => LastAccessedAt = TimeStampHelper.ToTimestamp(value);
    }
}

