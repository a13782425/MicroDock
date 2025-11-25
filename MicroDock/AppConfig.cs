using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;

namespace MicroDock;

/// <summary>
/// 应用配置
/// </summary>
internal static class AppConfig
{
    //主要的, 次要的, 构建的, 修订的
    public static Version AppVersion { get; set; } = new Version(0, 0, 3, 0);
    /// <summary>
    /// 是否是真实退出应用
    /// </summary>
    public static bool RealExit { get; set; } = false;

    private static string _configFolder = Path.Combine(System.AppContext.BaseDirectory, "config");
    /// <summary>
    /// 应用配置文件夹
    /// </summary>
    public static string CONFIG_FOLDER => _configFolder;

    /// <summary>
    /// 根目录
    /// </summary>
    public static string ROOT_PATH => System.AppContext.BaseDirectory;


    static AppConfig()
    {
        // 确保目录存在
        if (!Directory.Exists(CONFIG_FOLDER))
        {
            Directory.CreateDirectory(CONFIG_FOLDER);
        }
    }
}
