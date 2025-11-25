using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MicroDock.Database;
using MicroDock.Service;
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
                ServiceLocator.InitializeServices();
                // 设置 LogService 的 IsInit 标志
                var logService = ServiceLocator.GetService<Service.LogService>();
                if (logService != null)
                {
                    logService.IsInit = true;
                }
                // 插件加载现在移至 AppSplashViewViewModel 中异步执行
                // ServiceLocator.GetService<Service.PluginService>()?.LoadPlugins();
                // 2. 应用主题（在创建窗口之前）
                ApplyThemeOnStartup();

                // 3. 创建主窗口
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };

                // 4. 初始化需要窗口的服务
                ServiceLocator.Get<Service.AutoHideService>().Initialize(mainWindow);
                ServiceLocator.Get<Service.TopMostService>().Initialize(mainWindow);
                ServiceLocator.Get<Service.TrayService>().Initialize();
                
                // 初始化平台服务 (Windows Hook)
                ServiceLocator.GetService<Service.Platform.IPlatformService>()?.Initialize(mainWindow);

                desktop.MainWindow = mainWindow;

                // 5. 启动命名管道服务器，监听其他实例的显示窗口请求
#if !DEBUG
                SingleInstanceService.StartPipeServer(() =>
                {
                    // 在 UI 线程上执行窗口显示操作
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            if (mainWindow != null)
                            {
                                // 如果窗口最小化，先恢复
                                if (mainWindow.WindowState == Avalonia.Controls.WindowState.Minimized)
                                {
                                    mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                                }

                                // 显示并激活窗口
                                mainWindow.Show();
                                mainWindow.Activate();

                                Serilog.Log.Information("已显示并激活主窗口（响应其他实例请求）");
                            }
                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Error(ex, "显示主窗口失败");
                        }
                    });
                });
#endif

                // 6. 退出时清理
                desktop.Exit += (s, e) =>
                {
#if !DEBUG
                    SingleInstanceService.StopPipeServer();
#endif
                    ServiceLocator.GetService<DelayStorageService>()?.Dispose();
                    ServiceLocator.GetService<PluginService>()?.Dispose();
                    DBContext.Close();
                    ServiceLocator.Clear();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// 启动时应用主题
        /// </summary>
        private void ApplyThemeOnStartup()
        {
            try
            {
                var settings = DBContext.GetSetting();
                var themeService = ServiceLocator.Get<Service.ThemeService>();

                // 如果数据库中有保存的主题名称，应用该主题
                if (!string.IsNullOrEmpty(settings.SelectedTheme))
                {
                    if (themeService.LoadAndApplyTheme(settings.SelectedTheme))
                    {
                        Serilog.Log.Debug("启动时应用主题: {ThemeName}", settings.SelectedTheme);
                        return;
                    }
                }

                // 如果没有保存的主题，使用默认主题或第一个可用主题
                var availableThemes = themeService.GetAvailableThemes();
                if (availableThemes.Count > 0)
                {
                    // 尝试使用第一个主题作为默认
                    var defaultTheme = availableThemes[0];
                    themeService.ApplyTheme(defaultTheme);
                    Serilog.Log.Debug("启动时应用默认主题: {ThemeName}", defaultTheme.Name);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "启动时应用主题失败");
            }
        }

    }
}