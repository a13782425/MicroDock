using SQLite;
using System;

namespace MicroDock.Database;

/// <summary>
/// 插件工具统计数据模型
/// </summary>
[Table("PluginToolStatistics")]
public class PluginToolStatisticsDB
{
    /// <summary>
    /// 复合主键：PluginName:ToolName
    /// </summary>
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 插件唯一名称
    /// </summary>
    [Indexed]
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// 工具名称
    /// </summary>
    [Indexed]
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// 总调用次数
    /// </summary>
    public int CallCount { get; set; }

    /// <summary>
    /// 成功次数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败次数
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// 平均执行时间（毫秒）
    /// </summary>
    public long AverageDurationMs { get; set; }

    /// <summary>
    /// 最后调用时间戳（从2025年1月1日开始的毫秒数）
    /// </summary>
    [Indexed]
    public long LastCallTime { get; set; }

    /// <summary>
    /// 首次调用时间戳（从2025年1月1日开始的毫秒数）
    /// </summary>
    public long FirstCallTime { get; set; }

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
    /// 最后调用时间（DateTime 包装器）
    /// </summary>
    [Ignore]
    public DateTime LastCallDateTime
    {
        get => TimeStampHelper.ToDateTime(LastCallTime);
        set => LastCallTime = TimeStampHelper.ToTimestamp(value);
    }

    /// <summary>
    /// 首次调用时间（DateTime 包装器）
    /// </summary>
    [Ignore]
    public DateTime FirstCallDateTime
    {
        get => TimeStampHelper.ToDateTime(FirstCallTime);
        set => FirstCallTime = TimeStampHelper.ToTimestamp(value);
    }

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

    /// <summary>
    /// 成功率（百分比）
    /// </summary>
    [Ignore]
    public double SuccessRate
    {
        get
        {
            if (CallCount == 0) return 0;
            return (double)SuccessCount / CallCount * 100;
        }
    }
}

