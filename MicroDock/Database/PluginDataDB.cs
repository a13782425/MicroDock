using SQLite;
using System;

namespace MicroDock.Database;

/// <summary>
/// 插件键值存储数据模型
/// </summary>
public class PluginDataDB
{
    /// <summary>
    /// 复合主键：PluginName + Key
    /// </summary>
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// 插件唯一名称
    /// </summary>
    [Indexed]
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// 键名
    /// </summary>
    [Indexed]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 值（字符串格式）
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间戳（从2025年1月1日开始的毫秒数）
    /// </summary>
    public long CreatedAt { get; set; }

    /// <summary>
    /// 最后更新时间戳（从2025年1月1日开始的毫秒数）
    /// </summary>
    [Indexed]
    public long UpdatedAt { get; set; }

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
    /// 最后更新时间（DateTime 包装器）
    /// </summary>
    [Ignore]
    public DateTime UpdatedAtDateTime
    {
        get => TimeStampHelper.ToDateTime(UpdatedAt);
        set => UpdatedAt = TimeStampHelper.ToTimestamp(value);
    }


}

