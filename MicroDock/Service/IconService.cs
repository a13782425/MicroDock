using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Media;
using MicroDock.Service.Platform;
using Serilog;

namespace MicroDock.Service;

/// <summary>
/// 图标服务
/// 使用平台抽象层，支持跨平台图标提取
/// </summary>
public static class IconService
{
    private static readonly IPlatformIconService? _platformIconService = PlatformServiceFactory.CreateIconService();

    /// <summary>
    /// 从字节数组创建图像（修复：使用using释放MemoryStream）
    /// </summary>
    public static IImage? ImageFromBytes(byte[]? data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        using (MemoryStream stream = new MemoryStream(data, writable: false))
        {
            return new Avalonia.Media.Imaging.Bitmap(stream);
        }
    }

    /// <summary>
    /// 尝试从文件提取图标字节数组
    /// </summary>
    public static byte[]? TryExtractFileIconBytes(string filePath, int preferredSize = 48)
    {
        if (_platformIconService == null || !_platformIconService.IsSupported)
        {
            Log.Warning("当前平台不支持图标提取功能");
            return null;
        }

        try
        {
            return _platformIconService.ExtractIconBytes(filePath, preferredSize);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "提取图标失败: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// 尝试启动进程
    /// </summary>
    public static bool TryStartProcess(string path)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            Process? process = Process.Start(psi);
            bool success = process != null;
            
            if (success)
            {
                Log.Information("成功启动进程: {Path}", path);
            }
            else
            {
                Log.Warning("启动进程失败(返回null): {Path}", path);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动进程失败: {Path}", path);
            return false;
        }
    }
}


