using Avalonia.Media;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Database;

internal class SettingDB
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string SelectedTheme { get; set; } = string.Empty; // 选中的主题名称（XML文件名）
    
    /// <summary>
    /// 是否开机自启动
    /// </summary>
    public bool AutoStartup { get; set; } = false;
    
    /// <summary>
    /// 是否靠边隐藏
    /// </summary>
    public bool AutoHide { get; set; } = false;
    
    /// <summary>
    /// 是否窗口置顶
    /// </summary>
    public bool AlwaysOnTop { get; set; } = false;
    
    /// <summary>
    /// 是否显示日志查看器标签页
    /// </summary>
    public bool ShowLogViewer { get; set; } = false;

    /// <summary>
    /// 窗口X坐标
    /// </summary>
    public int WindowX { get; set; } = 0;

    /// <summary>
    /// 窗口Y坐标
    /// </summary>
    public int WindowY { get; set; } = 0;

    /// <summary>
    /// 窗口宽度
    /// </summary>
    public int WindowWidth { get; set; } = 480;

    /// <summary>
    /// 窗口高度
    /// </summary>
    public int WindowHeight { get; set; } = 360;

    #region 服务器与备份设置

    /// <summary>
    /// 服务器地址（用于插件上传和数据备份）
    /// </summary>
    public string ServerAddress { get; set; } = string.Empty;

    /// <summary>
    /// 备份密码（用于数据备份和恢复）
    /// </summary>
    public string BackupPassword { get; set; } = string.Empty;

    /// <summary>
    /// 上次主程序备份时间（Unix时间戳）
    /// </summary>
    public long LastAppBackupTime { get; set; } = 0;

    #endregion
}
