using Avalonia.Threading;
using MicroDock.Models;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace MicroDock.Services;

/// <summary>
/// 日志服务，实现 Serilog 的 ILogEventSink 接口，管理内存中的日志条目
/// </summary>
public class LogService : ILogEventSink
{
    private const int MaxLogCount = 1000;
    private readonly object _lock = new object();

    /// <summary>
    /// 日志条目集合，供 UI 绑定
    /// </summary>
    public ObservableCollection<LogEntry> Logs { get; }

    public bool IsInit = false;

    /// <summary>
    /// 公共构造函数，用于 ServiceLocator 注册
    /// </summary>
    public LogService()
    {
        Logs = new ObservableCollection<LogEntry>();
    }

    /// <summary>
    /// 实现 ILogEventSink 接口，接收 Serilog 日志事件
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        if (logEvent == null)
            return;

        // 转换为 LogEntry 对象
        var entry = new LogEntry
        {
            Timestamp = logEvent.Timestamp,
            Level = logEvent.Level,
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.ToString()
        };

        // 检查是否在 UI 线程上，或者 Dispatcher 是否可用
        try
        {
            if (!IsInit || Dispatcher.UIThread.CheckAccess())
            {
                // 已经在 UI 线程上，直接添加
                AddLogEntry(entry);
            }
            else
            {
                // 在 UI 线程上添加日志
                Dispatcher.UIThread.Post(() =>
                {
                    AddLogEntry(entry);
                }, DispatcherPriority.Background);
            }
        }
        catch (Exception)
        {
            // Dispatcher 还未初始化，直接在当前线程添加
            // 这通常发生在应用启动早期
            AddLogEntry(entry);
        }
    }

    /// <summary>
    /// 添加日志条目（内部方法，已在 UI 线程上调用）
    /// </summary>
    private void AddLogEntry(LogEntry entry)
    {
        lock (_lock)
        {
            // 限制日志数量，FIFO 队列
            if (Logs.Count >= MaxLogCount)
            {
                Logs.RemoveAt(0);
            }

            Logs.Add(entry);
        }
    }

    /// <summary>
    /// 清空当前内存中的日志
    /// </summary>
    public void ClearLogs()
    {
        lock (_lock)
        {
            Logs.Clear();
        }
    }

    /// <summary>
    /// 打开日志文件夹
    /// </summary>
    public void OpenLogFolder()
    {
        try
        {
            string logDirectory = Path.Combine(AppConfig.ROOT_PATH, "Log");

            // 确保日志目录存在
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // 使用 Windows 资源管理器打开文件夹
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = logDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "打开日志文件夹失败");
        }
    }
}

