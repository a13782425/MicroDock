using System;
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
            
            // 2. 应用主题（在创建窗口之前）
            ApplyThemeOnStartup();
            
            // 3. 创建主窗口
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            // 4. 初始化需要窗口的服务
            Infrastructure.ServiceLocator.Get<Services.AutoHideService>().Initialize(mainWindow);
            Infrastructure.ServiceLocator.Get<Services.TopMostService>().Initialize(mainWindow);
            Infrastructure.ServiceLocator.Get<Services.TrayService>().Initialize();
            
            desktop.MainWindow = mainWindow;
            
            // 5. 退出时清理
            desktop.Exit += (s, e) =>
            {
                DBContext.Close();
                Infrastructure.ServiceLocator.Clear();
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
            var themeService = Infrastructure.ServiceLocator.Get<Services.ThemeService>();

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