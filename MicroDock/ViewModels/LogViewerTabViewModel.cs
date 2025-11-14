using Avalonia.Threading;
using MicroDock.Models;
using MicroDock.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;

namespace MicroDock.ViewModels;

/// <summary>
/// 日志查看器标签页的 ViewModel
/// </summary>
public class LogViewerTabViewModel : ViewModelBase, IDisposable
{
    private string _selectedLevel = "All";
    private string _searchKeyword = string.Empty;
    private bool _disposed = false;

    public LogViewerTabViewModel()
    {
        // 初始化命令
        OpenLogFolderCommand = ReactiveCommand.Create(OpenLogFolder);
        ClearLogsCommand = ReactiveCommand.Create(ClearLogs);

        // 初始化筛选后的日志集合
        FilteredLogs = new ObservableCollection<LogEntry>();

        // 监听 LogService 的日志变化
        Infrastructure.ServiceLocator.Get<LogService>().Logs.CollectionChanged += OnLogsCollectionChanged;

        // 初始加载现有日志
        RefreshFilteredLogs();
    }

    /// <summary>
    /// 筛选后的日志列表
    /// </summary>
    public ObservableCollection<LogEntry> FilteredLogs { get; }

    /// <summary>
    /// 选中的日志级别
    /// </summary>
    public string SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedLevel, value);
            RefreshFilteredLogs();
        }
    }

    /// <summary>
    /// 搜索关键字
    /// </summary>
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchKeyword, value);
            RefreshFilteredLogs();
        }
    }

    /// <summary>
    /// 日志级别选项
    /// </summary>
    public string[] LogLevels { get; } = new[] { "All", "Debug", "Information", "Warning", "Error", "Fatal" };

    /// <summary>
    /// 打开日志文件夹命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenLogFolderCommand { get; }

    /// <summary>
    /// 清空日志命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearLogsCommand { get; }

    /// <summary>
    /// 处理 LogService 日志集合变化事件
    /// </summary>
    private void OnLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 确保在 UI 线程上执行所有操作
        if (Dispatcher.UIThread.CheckAccess())
        {
            ProcessLogsCollectionChanged(e);
        }
        else
        {
            Dispatcher.UIThread.Post(() => ProcessLogsCollectionChanged(e), DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// 处理日志集合变化（在 UI 线程上执行）
    /// </summary>
    private void ProcessLogsCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            // 新增日志，检查是否符合筛选条件
            foreach (LogEntry newLog in e.NewItems)
            {
                if (IsLogMatched(newLog))
                {
                    FilteredLogs.Add(newLog);
                    // 触发滚动到最新日志的事件
                    ScrollToLatestRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // 清空日志
            FilteredLogs.Clear();
        }
        else
        {
            // 其他情况，重新筛选
            RefreshFilteredLogs();
        }
    }

    /// <summary>
    /// 滚动到最新日志请求事件
    /// </summary>
    public event EventHandler? ScrollToLatestRequested;

    /// <summary>
    /// 刷新筛选后的日志列表
    /// </summary>
    private void RefreshFilteredLogs()
    {
        // 确保在 UI 线程上执行
        if (Dispatcher.UIThread.CheckAccess())
        {
            DoRefreshFilteredLogs();
        }
        else
        {
            Dispatcher.UIThread.Post(() => DoRefreshFilteredLogs(), DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// 执行刷新筛选后的日志列表（在 UI 线程上执行）
    /// </summary>
    private void DoRefreshFilteredLogs()
    {
        FilteredLogs.Clear();

        foreach (var log in Infrastructure.ServiceLocator.Get<LogService>().Logs)
        {
            if (IsLogMatched(log))
            {
                FilteredLogs.Add(log);
            }
        }
    }

    /// <summary>
    /// 检查日志是否匹配筛选条件
    /// </summary>
    private bool IsLogMatched(LogEntry log)
    {
        // 级别筛选
        if (SelectedLevel != "All" && log.Level.ToString() != SelectedLevel)
        {
            return false;
        }

        // 关键字筛选
        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            if (!log.Message.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 打开日志文件夹
    /// </summary>
    private void OpenLogFolder()
    {
        Infrastructure.ServiceLocator.Get<LogService>().OpenLogFolder();
    }

    /// <summary>
    /// 清空当前日志
    /// </summary>
    private void ClearLogs()
    {
        Infrastructure.ServiceLocator.Get<LogService>().ClearLogs();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // 取消订阅事件
        Infrastructure.ServiceLocator.Get<LogService>().Logs.CollectionChanged -= OnLogsCollectionChanged;

        _disposed = true;
    }
}

