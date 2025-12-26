using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using System;
using System.IO;

namespace MicroDock;

/// <summary>
/// 应用配置
/// </summary>
internal static class AppConfig
{
    //主要的, 次要的, 构建的, 修订的
    public static Version MicroAppVersion { get; } = new Version(1, 2, 0, 0);
    /// <summary>
    /// 是否是真实退出应用
    /// </summary>
    public static bool RealExit { get; set; } = false;
    /// <summary>
    /// 根目录
    /// </summary>
    public static string ROOT_PATH => System.AppContext.BaseDirectory;

    /// <summary>
    /// 应用配置文件夹
    /// </summary>
    public static string CONFIG_FOLDER { get; } = Path.Combine(ROOT_PATH, "config");

    /// <summary>
    /// 插件文件夹
    /// </summary>
    public static string PLUGIN_FOLDER { get; } = Path.Combine(ROOT_PATH, "plugins");
    /// <summary>
    /// 存储路径
    /// </summary>
    public static string STORGE_PATH => AppSettings.Instance.StoragePath;
    /// <summary>
    /// 所有数据路径
    /// </summary>
    public static string DATA_FOLDER { get; } = Path.Combine(STORGE_PATH, "data");
    /// <summary>
    /// 主程序数据路径
    /// </summary>
    public static string MAIN_DATA_FOLDER { get; } = Path.Combine(DATA_FOLDER, "engine");
    /// <summary>
    /// 插件的数据路径
    /// </summary>
    public static string PLUGIN_DATA_FOLDER { get; } = Path.Combine(DATA_FOLDER, "plugins");
    /// <summary>
    /// 应用日志路径
    /// </summary>
    public static string LOG_FOLDER { get; } = Path.Combine(STORGE_PATH, "logs");
    /// <summary>
    /// 临时目录
    /// </summary>
    public static string TEMP_FOLDER { get; } = Path.Combine(STORGE_PATH, "temp");
    /// <summary>
    /// 主程序临时目录
    /// </summary>
    public static string MAIN_TEMP_DATA_FOLDER { get; } = Path.Combine(TEMP_FOLDER, "engine");

    /// <summary>
    /// 插件临时目录
    /// </summary>
    public static string PLUGIN_TEMP_DATA_FOLDER { get; } = Path.Combine(TEMP_FOLDER, "plugins");



    /// <summary>
    /// 系统托盘通知管理器
    /// </summary>
    public static DesktopNotificationManager MicroNotificationManager { get; set; } = null!;

    /// <summary>
    /// 应用内窗口通知管理器（Toast通知）
    /// </summary>
    public static WindowNotificationManager? MicroWindowNotificationManager { get; set; }

    /// <summary>
    /// 主窗口实例
    /// 禁止设置
    /// </summary>
    public static Window MicroMainWindow { get; set; }

    /// <summary>
    /// 应用构建器实例
    /// 禁止设置
    /// </summary>
    public static AppBuilder MicroAppBuilder { get; set; }

    #region 临时目录配置

    /// <summary>
    /// 插件更新临时目录（存放待安装的插件）
    /// </summary>
    public static string TEMP_INSTALL_FOLDER => Path.Combine(TEMP_FOLDER, "install");

    /// <summary>
    /// 备份/恢复临时目录
    /// </summary>
    public static string TEMP_BACKUP_FOLDER => Path.Combine(TEMP_FOLDER, "backup");

    /// <summary>
    /// 临时目录每次启动清空
    /// </summary>
    public static string TEMP_IMPORT_FOLDER => Path.Combine(TEMP_FOLDER, "import");

    #endregion


#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
    static AppConfig()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
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
        if (!Directory.Exists(TEMP_INSTALL_FOLDER))
        {
            Directory.CreateDirectory(TEMP_INSTALL_FOLDER);
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
