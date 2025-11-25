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
    public int LoadingProgress { get; set; }
    public string LoadingText { get; set; }
    public string VersionInfo { get; set; }

    public string AppName => "Game Hub";

    public IImage AppIcon => null;

    public object SplashScreenContent => new AppSplashView() { DataContext = this };

    public int MinimumShowTime => 1;

    // 完成事件，用于通知外部启动完成
    public event EventHandler LoadingCompleted;

    // 背景色
    public LinearGradientBrush Background { get; }

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
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
        catch
        {
            // 如果获取失败，返回默认值
            return "1.0.0";
        }
    }

    public async Task RunTasks(CancellationToken cancellationToken)
    {
        // 模拟初始化任务
        await Task.Delay(500); // 初始延迟

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

        //for (int i = 0; i <= 100; i += 5)
        //{
        //    LoadingProgress = i; // IApplicationSplashScreen要求0-1范围

        //    // 更新加载文本
        //    if (i < 20)
        //        LoadingText = "正在初始化应用...";
        //    else if (i < 40)
        //        LoadingText = "正在检查 Unity 版本...";
        //    else if (i < 60)
        //        LoadingText = "正在加载项目列表...";
        //    else if (i < 80)
        //        LoadingText = "正在连接服务...";
        //    else
        //        LoadingText = "即将完成...";

        //    // 这里可以添加实际的初始化代码，例如：
        //    // - 加载配置
        //    // - 初始化数据库连接
        //    // - 检查更新
        //    // - 加载资源

        //    // 通知UI更新

        //    this.RaisePropertyChanged(nameof(LoadingProgress));
        //    this.RaisePropertyChanged(nameof(LoadingText));


        //    // 模拟任务耗时
        //    await Task.Delay(new Random().Next(50, 150));
        //}

        // 确保看到100%
        LoadingProgress = 100;
        LoadingText = "加载完成!";

        this.RaisePropertyChanged(nameof(LoadingProgress));
        this.RaisePropertyChanged(nameof(LoadingText));


        await Task.Delay(500); // 最终延迟

        // 通知初始化完成
        LoadingCompleted?.Invoke(this, EventArgs.Empty);

    }
}