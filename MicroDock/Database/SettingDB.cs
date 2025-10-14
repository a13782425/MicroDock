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
    public string AppTheme { get; set; } = AppConfig.THEME_SYSTEM;
    public string AccentColor { get; set; } = Colors.SlateBlue.ToString();
    /// <summary>
    /// 是否使用高对比度主题
    /// </summary>
    public bool UseHighContrastTheme { get; set; } = false;
    
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
}
