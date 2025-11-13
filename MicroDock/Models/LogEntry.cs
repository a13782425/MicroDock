using Avalonia.Media;
using Serilog.Events;
using System;

namespace MicroDock.Models;

/// <summary>
/// 日志条目数据模型
/// </summary>
public class LogEntry
{
    /// <summary>
    /// 日志时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// 日志级别
    /// </summary>
    public LogEventLevel Level { get; set; }

    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 异常信息
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// 根据日志级别返回对应的颜色，供 UI 绑定
    /// </summary>
    public Color LevelColor
    {
        get
        {
            return Level switch
            {
                LogEventLevel.Verbose => Color.FromRgb(128, 128, 128), // Gray
                LogEventLevel.Debug => Color.FromRgb(128, 128, 128), // Gray
                LogEventLevel.Information => Color.FromRgb(0, 120, 212), // Blue
                LogEventLevel.Warning => Color.FromRgb(255, 140, 0), // Orange
                LogEventLevel.Error => Color.FromRgb(232, 17, 35), // Red
                LogEventLevel.Fatal => Color.FromRgb(168, 0, 0), // DarkRed
                _ => Color.FromRgb(128, 128, 128) // Default Gray
            };
        }
    }

    /// <summary>
    /// 格式化的时间戳字符串
    /// </summary>
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");

    /// <summary>
    /// 日志级别字符串
    /// </summary>
    public string LevelString => Level.ToString();
}

