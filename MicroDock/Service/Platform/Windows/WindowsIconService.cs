using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Serilog;

namespace MicroDock.Service.Platform.Windows;

/// <summary>
/// Windows平台图标提取服务实现
/// 使用Shell32.dll API提取文件图标
/// </summary>
public class WindowsIconService : IPlatformIconService
{
    // Windows Shell API 常量
    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;
    private const uint SHGFI_SMALLICON = 0x1;
    
    // Shell API 结构体
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
        public uint dwAttributes;
    }
    
    // Windows API 声明
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath, 
        uint dwFileAttributes, 
        ref SHFILEINFO psfi, 
        uint cbFileInfo, 
        uint uFlags);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public byte[]? ExtractIconBytes(string filePath, int preferredSize = 48)
    {
        if (!IsSupported)
        {
            return null;
        }

        try
        {
            // 使用 Shell API 获取图标（支持文件和文件夹）
            SHFILEINFO shfi = new SHFILEINFO();
            uint flags = SHGFI_ICON | (preferredSize > 32 ? SHGFI_LARGEICON : SHGFI_SMALLICON);
            
            IntPtr result = SHGetFileInfo(filePath, 0, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
            
            if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                // 从句柄创建 Icon 对象
                using (Icon icon = Icon.FromHandle(shfi.hIcon))
                using (Icon sized = new Icon(icon, preferredSize, preferredSize))
                using (Bitmap bitmap = sized.ToBitmap())
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
            finally
            {
                // 释放图标句柄
                DestroyIcon(shfi.hIcon);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Windows Shell API提取图标失败: {FilePath}", filePath);
            return null;
        }
    }
}

