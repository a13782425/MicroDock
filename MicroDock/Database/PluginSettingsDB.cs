using SQLite;
using System;

namespace MicroDock.Database;

/// <summary>
/// 插件设置数据模型
/// </summary>
public class PluginSettingsDB
{
    /// <summary>
    /// 复合主键：PluginName + : + SettingsKey
    /// </summary>
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 插件唯一名称
    /// </summary>
    [Indexed]
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// 设置键名
    /// </summary>
    [Indexed]
    public string SettingsKey { get; set; } = string.Empty;

    /// <summary>
    /// 设置值（字符串）
    /// </summary>
    public string SettingsValue { get; set; } = string.Empty;

    /// <summary>
    /// 设置描述（如果为空则使用 SettingsKey）
    /// </summary>
    public string Description { get; set; } = string.Empty;

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

