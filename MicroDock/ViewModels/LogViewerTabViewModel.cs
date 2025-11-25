using Avalonia.Threading;
using MicroDock.Model;
using MicroDock.Service;
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
    private string _selectedTag = "All";
    private string _searchKeyword = string.Empty;
    private bool _disposed = false;

    public LogViewerTabViewModel()
    {
        // 初始化命令
        OpenLogFolderCommand = ReactiveCommand.Create(OpenLogFolder);
        ClearLogsCommand = ReactiveCommand.Create(ClearLogs);

        // 初始化筛选后的日志集合
        FilteredLogs = new ObservableCollection<LogEntry>();

        // 初始化可用标签集合
        AvailableTags = new ObservableCollection<string> { "All", "None" };

        // 监听 LogService 的日志变化
        ServiceLocator.Get<LogService>().Logs.CollectionChanged += OnLogsCollectionChanged;

        // 初始加载现有日志
        RefreshFilteredLogs();
        RefreshAvailableTags();
    }

    /// <summary>
    /// 筛选后的日志列表
    /// </summary>
    public ObservableCollection<LogEntry> FilteredLogs { get; }

    /// <summary>
    /// 可用的标签列表
    /// </summary>
    public ObservableCollection<string> AvailableTags { get; }

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
    /// 选中的日志标签
    /// </summary>
    public string SelectedTag
    {
        get => _selectedTag;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTag, value);
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
                UpdateAvailableTags(newLog.Tag);

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
            // 重置可用标签
            AvailableTags.Clear();
            AvailableTags.Add("All");
            AvailableTags.Add("None");
            SelectedTag = "All";
        }
        else
        {
            // 其他情况，重新筛选
            RefreshFilteredLogs();
            RefreshAvailableTags();
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

        foreach (var log in ServiceLocator.Get<LogService>().Logs)
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

        // 标签筛选
        if (SelectedTag != "All")
        {
            if (SelectedTag == "None")
            {
                if (!string.IsNullOrEmpty(log.Tag))
                {
                    return false;
                }
            }
            else if (log.Tag != SelectedTag)
            {
                return false;
            }
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
    /// 刷新可用标签列表
    /// </summary>
    private void RefreshAvailableTags()
    {
        // 确保在 UI 线程上执行
        if (Dispatcher.UIThread.CheckAccess())
        {
            DoRefreshAvailableTags();
        }
        else
        {
            Dispatcher.UIThread.Post(() => DoRefreshAvailableTags(), DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// 执行刷新可用标签列表（在 UI 线程上执行）
    /// </summary>
    private void DoRefreshAvailableTags()
    {
        var currentTags = AvailableTags.ToList();
        var newTags = ServiceLocator.Get<LogService>().Logs
            .Select(l => l.Tag)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .OrderBy(t => t)
            .ToList();
        
        foreach (var tag in newTags)
        {
            if (!currentTags.Contains(tag))
            {
                AvailableTags.Add(tag);
            }
        }
    }

    /// <summary>
    /// 更新可用标签
    /// </summary>
    private void UpdateAvailableTags(string? tag)
    {
        if (!string.IsNullOrEmpty(tag) && !AvailableTags.Contains(tag))
        {
            AvailableTags.Add(tag);
        }
    }

    /// <summary>
    /// 打开日志文件夹
    /// </summary>
    private void OpenLogFolder()
    {
        ServiceLocator.Get<LogService>().OpenLogFolder();
    }

    /// <summary>
    /// 清空当前日志
    /// </summary>
    private void ClearLogs()
    {
        ServiceLocator.Get<LogService>().ClearLogs();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // 取消订阅事件
        ServiceLocator.Get<LogService>().Logs.CollectionChanged -= OnLogsCollectionChanged;

        _disposed = true;
    }
}

