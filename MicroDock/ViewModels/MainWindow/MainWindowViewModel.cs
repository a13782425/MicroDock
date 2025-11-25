using Avalonia.Controls;
using MicroDock.Model;
using MicroDock.Plugin;
using MicroDock.Service;
using MicroDock.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;

namespace MicroDock.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private object? _mainContent;
    private bool _disposed = false;
    private bool _isLoading = false;
    private string? _loadingMessage = null;

    public MainWindowViewModel()
    {
        // 订阅全局 Loading 消息
        ServiceLocator.Get<EventService>().Subscribe<ShowLoadingMessage>(OnShowLoading);
        ServiceLocator.Get<EventService>().Subscribe<HideLoadingMessage>(OnHideLoading);
    }

    /// <summary>
    /// 开始启动流程
    /// </summary>
    public async Task StartStartupProcessAsync()
    {
        if (MainContent is AppSplashViewModel splashVm)
        {
            await splashVm.RunTasks(CancellationToken.None);
        }
    }


    /// <summary>
    /// 当前显示的主内容 (SplashView 或 MainView)
    /// </summary>
    public object? MainContent
    {
        get => _mainContent;
        set => this.RaiseAndSetIfChanged(ref _mainContent, value);
    }

    /// <summary>
    /// 处理显示Loading消息
    /// </summary>
    private void OnShowLoading(ShowLoadingMessage message)
    {
        // 需要在UI线程上更新
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            LoadingMessage = message.Message;
            IsLoading = true;
        });
    }

    /// <summary>
    /// 处理隐藏Loading消息
    /// </summary>
    private void OnHideLoading(HideLoadingMessage message)
    {
        // 需要在UI线程上更新
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsLoading = false;
            LoadingMessage = null;
        });
    }


    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// 加载消息
    /// </summary>
    public string? LoadingMessage
    {
        get => _loadingMessage;
        set => this.RaiseAndSetIfChanged(ref _loadingMessage, value);
    }


    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // 如果当前内容是 MainViewModel，也需要释放
        if (MainContent is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}
