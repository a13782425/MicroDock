using System;
using System.Runtime.InteropServices;
using MicroDock.Services.Platform.Windows;

namespace MicroDock.Services.Platform;

/// <summary>
/// 平台服务工厂
/// 根据当前运行的操作系统返回相应的平台服务实现
/// </summary>
public static class PlatformServiceFactory
{
    /// <summary>
    /// 获取当前操作系统类型
    /// </summary>
    public static OSPlatform CurrentPlatform
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OSPlatform.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OSPlatform.OSX;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OSPlatform.Linux;
            
            throw new PlatformNotSupportedException("无法识别当前操作系统平台");
        }
    }

    /// <summary>
    /// 创建平台开机自启动服务
    /// </summary>
    /// <returns>平台特定的自启动服务实现，如果平台不支持则返回null</returns>
    public static IPlatformStartupService? CreateStartupService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsStartupService();
        }
        
        // macOS 和 Linux 实现待添加
        // if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        // {
        //     return new MacOSStartupService();
        // }
        
        return null;
    }

    /// <summary>
    /// 创建平台图标提取服务
    /// </summary>
    /// <returns>平台特定的图标服务实现，如果平台不支持则返回null</returns>
    public static IPlatformIconService? CreateIconService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsIconService();
        }
        
        // macOS 和 Linux 实现待添加
        // if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        // {
        //     return new MacOSIconService();
        // }
        
        return null;
    }

    /// <summary>
    /// 创建平台鼠标光标服务
    /// </summary>
    /// <returns>平台特定的光标服务实现，如果平台不支持则返回null</returns>
    public static IPlatformCursorService? CreateCursorService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsCursorService();
        }
        
        // macOS 和 Linux 实现待添加
        // if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        // {
        //     return new MacOSCursorService();
        // }
        
        return null;
    }
}

