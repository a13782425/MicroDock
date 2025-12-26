using MicroDock.Model;
using MicroDock.Utils;
using SQLite;
using System;

namespace MicroDock.Database;

public class ApplicationDB
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; } = 0;


    [Indexed]
    public string? IconHash { get; set; }

    public int Type { get; set; } = 0;
    /// <summary>
    /// 安装时间戳（从2025年1月1日开始的毫秒数）
    /// </summary>
    public long InstalledAt { get; set; }

    /// <summary>
    /// 最后使用时间（从2025年1月1日开始的毫秒数）
    /// </summary>
    public long LastUseAt { get; set; } = 0;


    [Ignore]
    public FileType AppType => (FileType)Type;
    /// <summary>
    /// 安装时间（DateTime 包装器）
    /// </summary>
    [Ignore]
    public DateTime InstalledAtDateTime
    {
        get => TimeStampHelper.ToDateTime(InstalledAt);
        set => InstalledAt = TimeStampHelper.ToTimestamp(value);
    }
    /// <summary>
    /// 最后使用时间（DateTime 包装器）
    /// </summary>
    [Ignore]
    public DateTime LastUseAtDateTime
    {
        get => TimeStampHelper.ToDateTime(LastUseAt);
        set => LastUseAt = TimeStampHelper.ToTimestamp(value);
    }
}