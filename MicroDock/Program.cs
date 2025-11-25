using Avalonia;
using Avalonia.Controls.Notifications;
using DesktopNotifications.Avalonia;
using ReactiveUI.Avalonia;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace MicroDock
{
    internal sealed class Program
    {
        /// <summary>
        /// 系统托盘通知管理器
        /// </summary>
        public static DesktopNotifications.INotificationManager NotificationManager = null!;

        /// <summary>
        /// 应用内窗口通知管理器（Toast通知）
        /// </summary>
        public static WindowNotificationManager? WindowNotificationManager { get; set; }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // 初始化日志系统
            InitializeLogger();

            try
            {
                Log.Information("MicroDock 启动中...");
                Log.Information("应用版本: {Version}", AppConfig.AppVersion);

                // ============================================
                // 防止多实例启动 - 使用全局互斥锁
                // ============================================
#if !DEBUG
                if (!SingleInstanceService.TryAcquireMutex())
                {
                    Log.Information("检测到已有 MicroDock 实例正在运行，通知显示窗口后退出");
                    SingleInstanceService.NotifyExistingInstance();
                    Log.Information("程序退出");
                    return; // 退出程序
                }
#endif

                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
                //MicroDock.Infrastructure.ServiceLocator.Get<MicroDock.Services.LogService>().IsInit = true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "应用程序启动失败");
                throw;
            }
            finally
            {
                // 清理单实例资源
#if !DEBUG
                SingleInstanceService.ReleaseMutex();
#endif
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// 初始化Serilog日志系统
        /// </summary>
        private static void InitializeLogger()
        {
            // 日志保存在软件目录下的Log文件夹
            string logDirectory = Path.Combine(AppConfig.ROOT_PATH, "log");

            // 确保日志目录存在
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string logFilePath = Path.Combine(logDirectory, "log-.txt");

            // 提前创建 LogService 并注册到 ServiceLocator
            var logService = new Service.LogService();
            Service.ServiceLocator.Register(logService);

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB per file
                    rollOnFileSizeLimit: true)
                .WriteTo.Sink(logService)
                .CreateLogger();

            Log.Information("日志系统初始化完成，日志目录: {LogDirectory}", logDirectory);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UseWin32()
                .UseSkia()
                .WithInterFont()
                .SetupDesktopNotifications(out NotificationManager!)
                .LogToTrace()
                .UseReactiveUI();
    }
}
