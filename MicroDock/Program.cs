using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.WebView.Desktop;
using DesktopNotifications.Avalonia;
using MicroDock.Service;
using ReactiveUI.Avalonia;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace MicroDock
{
    internal sealed class Program
    {
        /// <summary>
        /// 系统托盘通知管理器
        /// </summary>
        private static DesktopNotificationManager _notificationManager = null!;


        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            Startup(args).Wait();
        }

        private static async Task<bool> Startup(string[] args)
        {
            try
            {
                //初始化服务
                ServiceLocator.InitializeServices();
                await ServiceLocator.OnRegistered();
                LogInformation("MicroDock 启动中...");
                LogInformation($"应用版本: {AppConfig.MicroAppVersion}");

                // ============================================
                // 防止多实例启动 - 使用全局互斥锁
                // ============================================
                if (ServiceLocator.Get<SingleInstanceService>().IsExit)
                {
                    LogInformation("检测到已有 MicroDock 实例正在运行，通知显示窗口后退出");
                    ServiceLocator.Get<SingleInstanceService>().NotifyExistingInstance();
                    LogInformation("程序退出");
                    return false; // 退出程序
                }

                AppConfig.MicroAppBuilder = BuildAvaloniaApp();
                MicroNotificationManager = _notificationManager;
                await ServiceLocator.OnAfterAppBuilder();
                await ServiceLocator.OnBeforeSplashScreen();
                AppConfig.MicroAppBuilder.StartWithClassicDesktopLifetime(args);
                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "应用程序启动失败");
                return false;
            }
            finally
            {
                // 清理单实例资源

                ServiceLocator.Get<SingleInstanceService>()?.ReleaseMutex();
                Log.CloseAndFlush();
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UseWin32()
                .UseSkia()
                .WithInterFont()
                .SetupDesktopNotifications(out _notificationManager!)
                .LogToTrace()
                .UseDesktopWebView()
                .UseReactiveUI();
    }
}
