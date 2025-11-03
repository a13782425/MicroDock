using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Serilog;

namespace MicroDock.Services.Platform.Windows;

/// <summary>
/// Windows平台开机自启动服务实现
/// 通过修改注册表实现开机自启动
/// </summary>
public class WindowsStartupService : IPlatformStartupService
{
    private const string RUN_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
    
    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public void Enable(string appName, string executablePath)
    {
        if (!IsSupported)
        {
            throw new PlatformNotSupportedException("开机自启动功能仅在Windows平台支持");
        }

        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true))
            {
                if (key != null)
                {
                    key.SetValue(appName, $"\"{executablePath}\"");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Windows注册表操作失败: 启用自启动");
            throw;
        }
    }

    public void Disable(string appName)
    {
        if (!IsSupported)
        {
            throw new PlatformNotSupportedException("开机自启动功能仅在Windows平台支持");
        }

        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true))
            {
                if (key != null)
                {
                    key.DeleteValue(appName, false);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Windows注册表操作失败: 禁用自启动");
            throw;
        }
    }

    public bool IsEnabled(string appName)
    {
        if (!IsSupported)
        {
            return false;
        }

        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, false))
            {
                if (key != null)
                {
                    object? value = key.GetValue(appName);
                    return value != null;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Windows注册表操作失败: 检查自启动状态");
        }
        
        return false;
    }
}

