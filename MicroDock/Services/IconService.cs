using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media;

namespace MicroDock.Services;

public static class IconService
{
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Icon? icon = Icon.ExtractAssociatedIcon(filePath);
                if (icon == null)
                {
                    return null;
                }

                using (Icon sized = new Icon(icon, preferredSize, preferredSize))
                using (System.Drawing.Bitmap bitmap = sized.ToBitmap())
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
            return null;
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


