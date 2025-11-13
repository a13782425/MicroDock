using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MicroDock.Database;
using MicroDock.ViewModels;
using MicroDock.Views;

namespace MicroDock
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1. 初始化所有服务（应用程序启动时）
            Infrastructure.ServiceLocator.InitializeServices();
            
            // 2. 创建主窗口
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            // 3. 初始化需要窗口的服务
            Infrastructure.ServiceLocator.Get<Services.AutoHideService>().Initialize(mainWindow);
            Infrastructure.ServiceLocator.Get<Services.TopMostService>().Initialize(mainWindow);
            
            desktop.MainWindow = mainWindow;
            
            // 4. 退出时清理
            desktop.Exit += (s, e) =>
            {
                DBContext.Close();
                Infrastructure.ServiceLocator.Clear();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    }
}