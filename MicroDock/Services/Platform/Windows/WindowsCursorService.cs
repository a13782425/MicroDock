using System;
using System.Runtime.InteropServices;
using Avalonia;
using Serilog;

namespace MicroDock.Services.Platform.Windows;

/// <summary>
/// Windows平台鼠标光标服务实现
/// 使用P/Invoke调用GetCursorPos获取全局鼠标位置
/// </summary>
public class WindowsCursorService : IPlatformCursorService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public bool TryGetCursorPosition(out Point position)
    {
        position = default;

        if (!IsSupported)
        {
            return false;
        }

        try
        {
            if (GetCursorPos(out POINT point))
            {
                position = new Point(point.X, point.Y);
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Windows API获取鼠标位置失败");
        }

        return false;
    }
}

