using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media;

namespace MicroDock.Services;

public static class IconService
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
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static IImage? ImageFromBytes(byte[]? data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        MemoryStream stream = new MemoryStream(data);
        return new Avalonia.Media.Imaging.Bitmap(stream);
    }

    public static byte[]? TryExtractFileIconBytes(string filePath, int preferredSize = 48)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

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
                using (System.Drawing.Bitmap bitmap = sized.ToBitmap())
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
        catch
        {
            return null;
        }
    }

    public static bool TryStartProcess(string path)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            Process? process = Process.Start(psi);
            return process != null;
        }
        catch
        {
            return false;
        }
    }
}


