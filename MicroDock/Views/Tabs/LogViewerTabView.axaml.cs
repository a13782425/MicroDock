using Avalonia.Controls;
using Avalonia.Threading;
using MicroDock.ViewModels;
using System;

namespace MicroDock.Views;

public partial class LogViewerTabView : UserControl
{
    private LogViewerTabViewModel? _viewModel;

    public LogViewerTabView()
    {
        InitializeComponent();
        _viewModel = new LogViewerTabViewModel();
        DataContext = _viewModel;

        // 订阅滚动到最新日志的事件
        _viewModel.ScrollToLatestRequested += OnScrollToLatestRequested;
    }

    /// <summary>
    /// 滚动到最新日志
    /// </summary>
    private void OnScrollToLatestRequested(object? sender, EventArgs e)
    {
        // 确保在 UI 线程上执行滚动操作
        if (Dispatcher.UIThread.CheckAccess())
        {
            DoScrollToEnd();
        }
        else
        {
            Dispatcher.UIThread.Post(() => DoScrollToEnd(), DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// 执行滚动到底部（在 UI 线程上执行）
    /// </summary>
    private void DoScrollToEnd()
    {
        // 获取 ScrollViewer 控件
        var scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
        if (scrollViewer != null)
        {
            // 滚动到底部
            scrollViewer.ScrollToEnd();
        }
    }
}

