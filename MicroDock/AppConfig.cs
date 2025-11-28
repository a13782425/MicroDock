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

    #region 临时目录配置

    /// <summary>
    /// 临时文件根目录
    /// </summary>
    public static string TEMP_FOLDER => Path.Combine(ROOT_PATH, "temp");

    /// <summary>
    /// 插件更新临时目录（存放待安装的插件）
    /// </summary>
    public static string TEMP_PLUGIN_FOLDER => Path.Combine(TEMP_FOLDER, "plugin");

    /// <summary>
    /// 备份/恢复临时目录
    /// </summary>
    public static string TEMP_BACKUP_FOLDER => Path.Combine(TEMP_FOLDER, "backup");

    /// <summary>
    /// 插件导入解压临时目录
    /// </summary>
    public static string TEMP_IMPORT_FOLDER => Path.Combine(TEMP_FOLDER, "import");

    #endregion


    static AppConfig()
    {
        // 确保目录存在
        if (!Directory.Exists(CONFIG_FOLDER))
        {
            Directory.CreateDirectory(CONFIG_FOLDER);
        }

        // 确保临时目录存在
        if (!Directory.Exists(TEMP_FOLDER))
        {
            Directory.CreateDirectory(TEMP_FOLDER);
        }
        if (!Directory.Exists(TEMP_PLUGIN_FOLDER))
        {
            Directory.CreateDirectory(TEMP_PLUGIN_FOLDER);
        }
        if (!Directory.Exists(TEMP_BACKUP_FOLDER))
        {
            Directory.CreateDirectory(TEMP_BACKUP_FOLDER);
        }
        if (!Directory.Exists(TEMP_IMPORT_FOLDER))
        {
            Directory.CreateDirectory(TEMP_IMPORT_FOLDER);
        }
    }
}
