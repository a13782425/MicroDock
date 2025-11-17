using SQLite;
using System;

namespace MicroDock.Database;

/// <summary>
/// 插件信息数据模型
/// 用于存储插件的元数据和状态
/// </summary>
public class PluginInfoDB
{
    /// <summary>
    /// 插件唯一名称（主键）
    /// 格式：com.company.pluginname
    /// </summary>
    [PrimaryKey]
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// 插件显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 插件版本号
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 插件描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 作者信息
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用（默认启用）
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否标记为待删除（默认 false）
    /// </summary>
    public bool PendingDelete { get; set; } = false;

    /// <summary>
    /// 是否有待安装的更新（默认 false）
    /// </summary>
    public bool PendingUpdate { get; set; } = false;

    /// <summary>
    /// 待安装的新版本号（当 PendingUpdate = true 时）
    /// </summary>
    public string? PendingVersion { get; set; }

    /// <summary>
    /// 安装时间戳（从2025年1月1日开始的毫秒数）
    /// </summary>
    public long InstalledAt { get; set; }

    /// <summary>
    /// 最后更新时间戳（从2025年1月1日开始的毫秒数）
    /// </summary>
    [Indexed]
    public long UpdatedAt { get; set; }

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
    /// 最后更新时间（DateTime 包装器）
    /// </summary>
    [Ignore]
    public DateTime UpdatedAtDateTime
    {
        get => TimeStampHelper.ToDateTime(UpdatedAt);
        set => UpdatedAt = TimeStampHelper.ToTimestamp(value);
    }
}

