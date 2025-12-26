using MicroDock.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Model;


/// <summary>
/// 用户偏好设置的Key枚举定义
/// 用于 DBContext.GetXxxPreference / SetXxxPreference 方法
/// </summary>
public enum UserPreferenceKeys
{

    #region 主题设置
    /// <summary>
    /// 选中的主题名称（XML文件名）
    /// </summary>
    SelectedTheme,
    #endregion

    #region 基础设置
    /// <summary>
    /// 是否开机自启动
    /// </summary>
    AutoStartup,
    /// <summary>
    /// 是否靠边隐藏
    /// </summary>
    AutoHide,
    /// <summary>
    /// 是否窗口置顶
    /// </summary>
    AlwaysOnTop,
    /// <summary>
    /// 是否显示日志查看器标签页
    /// </summary>
    ShowLogViewer,
    /// <summary>
    /// 是否显示资源Key查看器
    /// </summary>
    ShowResViewer,
    #endregion

    #region 窗口位置和大小
    /// <summary>
    /// 窗口X坐标
    /// </summary>
    WindowX,
    /// <summary>
    /// 窗口Y坐标
    /// </summary>
    WindowY,
    /// <summary>
    /// 窗口宽度
    /// </summary>
    WindowWidth,
    /// <summary>
    /// 窗口高度
    /// </summary>
    WindowHeight,
    #endregion

    #region 服务器与备份设置
    /// <summary>
    /// 服务器地址（用于插件上传和数据备份）
    /// </summary>
    ServerAddress,
    /// <summary>
    /// 备份服务器地址（专用于数据备份，可选，为空时使用 ServerAddress）
    /// </summary>
    BackupServerAddress,
    /// <summary>
    /// 备份密码（用于数据备份和恢复）
    /// </summary>
    BackupPassword,
    /// <summary>
    /// 服务器验证Key（用于插件上传，防止恶意提交）
    /// </summary>
    ServerValidationKey,
    /// <summary>
    /// 上次主程序备份时间（Unix时间戳）
    /// </summary>
    LastAppBackupTime,
    #endregion

    #region 应用列表排序设置
    /// <summary>
    /// 应用列表排序方式
    /// 值: "Type" | "Name" | "AddTime"
    /// </summary>
    ApplicationSortBy,
    /// <summary>
    /// 应用列表排序顺序
    /// 值: true = 升序, false = 降序
    /// </summary>
    ApplicationSortAscending,
    #endregion
}

/// <summary>
/// UserPreferenceKeys 枚举的扩展方法
/// </summary>
public static class UserPreferenceKeysExtensions
{
    #region Get 方法
    /// <summary>
    /// 获取字符串类型的偏好值
    /// </summary>
    /// <param name="key">偏好Key</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>偏好值</returns>
    public static string GetString(this UserPreferenceKeys key, string defaultValue = "")
    {
        return DBContext.GetStringPreference(key.ToString(), defaultValue);
    }
    /// <summary>
    /// 获取整数类型的偏好值
    /// </summary>
    /// <param name="key">偏好Key</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>偏好值</returns>
    public static int GetInt(this UserPreferenceKeys key, int defaultValue = 0)
    {
        return DBContext.GetIntPreference(key.ToString(), defaultValue);
    }
    /// <summary>
    /// 获取布尔类型的偏好值
    /// </summary>
    /// <param name="key">偏好Key</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>偏好值</returns>
    public static bool GetBool(this UserPreferenceKeys key, bool defaultValue = false)
    {
        return DBContext.GetBoolPreference(key.ToString(), defaultValue);
    }
    /// <summary>
    /// 获取长整数类型的偏好值
    /// </summary>
    /// <param name="key">偏好Key</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>偏好值</returns>
    public static long GetLong(this UserPreferenceKeys key, long defaultValue = 0)
    {
        return DBContext.GetLongPreference(key.ToString(), defaultValue);
    }
    #endregion
    #region Set 方法
    /// <summary>
    /// 设置字符串类型的偏好值
    /// </summary>
    /// <param name="key">偏好Key</param>
    /// <param name="value">偏好值</param>
    public static void Set(this UserPreferenceKeys key, string value)
    {
        DBContext.SetPreference(key.ToString(), value);
    }
    /// <summary>
    /// 设置整数类型的偏好值
    /// </summary>
    /// <param name="key">偏好Key</param>
    /// <param name="value">偏好值</param>
    public static void Set(this UserPreferenceKeys key, int value)
    {
        DBContext.SetPreference(key.ToString(), value);
    }
    /// <summary>
    /// 设置布尔类型的偏好值
    /// </summary>
    /// <param name="key">偏好Key</param>
    /// <param name="value">偏好值</param>
    public static void Set(this UserPreferenceKeys key, bool value)
    {
        DBContext.SetPreference(key.ToString(), value);
    }
    /// <summary>
    /// 设置长整数类型的偏好值
    /// </summary>
    /// <param name="key">偏好Key</param>
    /// <param name="value">偏好值</param>
    public static void Set(this UserPreferenceKeys key, long value)
    {
        DBContext.SetPreference(key.ToString(), value);
    }
    #endregion
}

///// <summary>
///// 用户偏好设置的Key常量定义
///// 用于 DBContext.GetXxxPreference / SetXxxPreference 方法
///// </summary>
//public static class UserPreferenceKeys
//{
//    #region 主题设置

//    /// <summary>
//    /// 选中的主题名称（XML文件名）
//    /// </summary>
//    public const string SelectedTheme = "SelectedTheme";

//    #endregion

//    #region 基础设置

//    /// <summary>
//    /// 是否开机自启动
//    /// </summary>
//    public const string AutoStartup = "AutoStartup";

//    /// <summary>
//    /// 是否靠边隐藏
//    /// </summary>
//    public const string AutoHide = "AutoHide";

//    /// <summary>
//    /// 是否窗口置顶
//    /// </summary>
//    public const string AlwaysOnTop = "AlwaysOnTop";

//    /// <summary>
//    /// 是否显示日志查看器标签页
//    /// </summary>
//    public const string ShowLogViewer = "ShowLogViewer";

//    /// <summary>
//    /// 是否显示资源Key查看器
//    /// </summary>
//    public const string ShowResViewer = "ShowResViewer";

//    #endregion

//    #region 窗口位置和大小

//    /// <summary>
//    /// 窗口X坐标
//    /// </summary>
//    public const string WindowX = "WindowX";

//    /// <summary>
//    /// 窗口Y坐标
//    /// </summary>
//    public const string WindowY = "WindowY";

//    /// <summary>
//    /// 窗口宽度
//    /// </summary>
//    public const string WindowWidth = "WindowWidth";

//    /// <summary>
//    /// 窗口高度
//    /// </summary>
//    public const string WindowHeight = "WindowHeight";

//    #endregion

//    #region 服务器与备份设置

//    /// <summary>
//    /// 服务器地址（用于插件上传和数据备份）
//    /// </summary>
//    public const string ServerAddress = "ServerAddress";

//    /// <summary>
//    /// 备份服务器地址（专用于数据备份，可选，为空时使用 ServerAddress）
//    /// </summary>
//    public const string BackupServerAddress = "BackupServerAddress";

//    /// <summary>
//    /// 备份密码（用于数据备份和恢复）
//    /// </summary>
//    public const string BackupPassword = "BackupPassword";

//    /// <summary>
//    /// 服务器验证Key（用于插件上传，防止恶意提交）
//    /// </summary>
//    public const string ServerValidationKey = "ServerValidationKey";

//    /// <summary>
//    /// 上次主程序备份时间（Unix时间戳）
//    /// </summary>
//    public const string LastAppBackupTime = "LastAppBackupTime";

//    #endregion
//}