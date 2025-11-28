using MicroDock.Model;
using SQLite;
using System.Collections.Generic;

namespace MicroDock.Database;

/// <summary>
/// 导航页签配置表
/// </summary>
public class NavigationTabDB : IDatabase
{
    /// <summary>
    /// 唯一ID (主键)
    /// 格式: "PluginUniqueName:ClassName" 或 "microdock:ClassName"
    /// </summary>
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 排序索引
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 全局快捷键 (如 "Ctrl+Alt+T")
    /// </summary>
    public string? ShortcutKey { get; set; }

    /// <summary>
    /// 是否启用密码锁定
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// 密码哈希值 (SHA256)
    /// </summary>
    public string? PasswordHash { get; set; }

    public static Dictionary<string, NavigationTabDB> Cache = new Dictionary<string, NavigationTabDB>();

    private NavigationTabDto? _dto;
    public IDatabaseDto GetDto()
    {
        if (_dto == null)
            _dto = new NavigationTabDto(this);
        return _dto;
    }
}

