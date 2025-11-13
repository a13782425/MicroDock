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
    
    /// <summary>
    /// 是否开启迷你模式
    /// </summary>
    public bool IsMiniModeEnabled { get; set; } = false;

    // === 迷你模式配置（P1） ===
    /// <summary>
    /// 长按触发展开（毫秒）
    /// </summary>
    public int LongPressMs { get; set; } = 500;

    /// <summary>
    /// 环形半径
    /// </summary>
    public double MiniRadius { get; set; } = 60;

    /// <summary>
    /// 启动项大小（边长，像素）
    /// </summary>
    public double MiniItemSize { get; set; } = 40;

    /// <summary>
    /// 起始角度（度）
    /// </summary>
    public double MiniStartAngle { get; set; } = -90;

    /// <summary>
    /// 扫过角度（度）
    /// </summary>
    public double MiniSweepAngle { get; set; } = 360;

    /// <summary>
    /// 是否根据靠边自动半环
    /// </summary>
    public bool MiniAutoDynamicArc { get; set; } = true;

    /// <summary>
    /// 触发后是否自动收起
    /// </summary>
    public bool MiniAutoCollapseAfterTrigger { get; set; } = true;

    /// <summary>
    /// 是否显示日志查看器标签页
    /// </summary>
    public bool ShowLogViewer { get; set; } = false;
}
