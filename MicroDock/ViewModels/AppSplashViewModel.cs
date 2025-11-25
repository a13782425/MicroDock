using Avalonia.Media;
using FluentAvalonia.UI.Windowing;
using MicroDock.Procedure;
using MicroDock.Views;
using ReactiveUI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MicroDock.ViewModels;
public class AppSplashViewModel : ViewModelBase, IApplicationSplashScreen
{
    private int _loadingProgress = 0;
    public int LoadingProgress { get => _loadingProgress; set => this.RaiseAndSetIfChanged(ref _loadingProgress, value); }
    private string _loadingText = string.Empty;
    public string LoadingText { get => _loadingText; set => this.RaiseAndSetIfChanged(ref _loadingText, value); }
    public string VersionInfo { get; set; }

    public string AppName => "Game Hub";

    public IImage AppIcon => null;

    public object SplashScreenContent => new AppSplashView() { DataContext = this };

    public int MinimumShowTime => 1;

    // 完成事件，用于通知外部启动完成
    public event EventHandler LoadingCompleted;

    // 背景色
    //public LinearGradientBrush Background { get; }

    private BaseLaunchProcedure procedure = new LaunchInitializeProcedure();

    public AppSplashViewModel()
    {
        // 设置版本信息
        VersionInfo = $"版本 {GetAppVersion()} | 工具箱";
        LoadingText = "正在初始化...";
        LoadingProgress = 0;
    }

    private string GetAppVersion()
    {
        try
        {
            // 尝试获取应用版本
            return AppConfig.AppVersion.ToString();
        }
        catch
        {
            // 如果获取失败，返回默认值
            return "0.0.0.0";
        }
    }

    public async Task RunTasks(CancellationToken cancellationToken)
    {
        // 执行实际的初始化工作
        BaseLaunchProcedure temp = procedure;
        do
        {
            LoadingProgress = temp.Progress;
            LoadingText = temp.Description;
            await temp.ExecuteAsync();
            temp = temp.NextProcedure;
        }
        while (temp != null);

        // 确保看到100%
        LoadingProgress = 100;
        LoadingText = "加载完成!";

        this.RaisePropertyChanged(nameof(LoadingProgress));
        this.RaisePropertyChanged(nameof(LoadingText));


        await Task.Delay(100); // 最终延迟

        // 通知初始化完成
        LoadingCompleted?.Invoke(this, EventArgs.Empty);

    }
}