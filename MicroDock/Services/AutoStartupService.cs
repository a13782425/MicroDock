using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace MicroDock.Services;

/// <summary>
/// 开机自启动服务
/// </summary>
public class AutoStartupService : IWindowService
{
    private const string RUN_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string APP_NAME = "MicroDock";
    
    /// <summary>
    /// 启用开机自启动
    /// </summary>
    public void Enable()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true))
            {
                if (key != null)
                {
                    string exePath = GetExecutablePath();
                    key.SetValue(APP_NAME, $"\"{exePath}\"");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启用自启动失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 禁用开机自启动
    /// </summary>
    public void Disable()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true))
            {
                if (key != null)
                {
                    key.DeleteValue(APP_NAME, false);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"禁用自启动失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取服务是否已启用
    /// </summary>
    public bool IsEnabled
    {
        get
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, false))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(APP_NAME);
                        return value != null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查自启动状态失败: {ex.Message}");
            }
            return false;
        }
    }

    /// <summary>
    /// 获取可执行文件路径
    /// </summary>
    private string GetExecutablePath()
    {
        string processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            return processPath;
        }
        
        // 备用方案
        return Assembly.GetEntryAssembly()?.Location ?? string.Empty;
    }
}

