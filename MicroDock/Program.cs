using Avalonia;
using Avalonia.ReactiveUI;
using DesktopNotifications;
using DesktopNotifications.Avalonia;
using System;

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
            => BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
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
