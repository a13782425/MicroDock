using Avalonia;
using ReactiveUI.Avalonia;
using DesktopNotifications;
using DesktopNotifications.Avalonia;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace MicroDock
{
    internal sealed class Program
    {
        public static INotificationManager NotificationManager = null!;
        
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
                
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "应用程序启动失败");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        
        /// <summary>
        /// 初始化Serilog日志系统
        /// </summary>
        private static void InitializeLogger()
        {
            // 日志保存在软件目录下的Log文件夹
            string logDirectory = Path.Combine(AppContext.BaseDirectory, "Log");
            
            // 确保日志目录存在
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            string logFilePath = Path.Combine(logDirectory, "log-.txt");
            
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
