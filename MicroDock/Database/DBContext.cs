using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicroDock.Service;

namespace MicroDock.Database;

internal static class DBContext
{
    private static SQLiteConnection _database;
    static DBContext()
    {   // 获取应用数据目录
        string dbPath = Path.Combine(AppConfig.CONFIG_FOLDER, "microdock");
        _database = new SQLiteConnection(dbPath);
        // 自动创建表，如果表结构有变化会自动更新
        // 使用 MigrateTable 来支持添加新列
        try
        {
            _database.CreateTable<SettingDB>();
            _database.CreateTable<ApplicationDB>();
            _database.CreateTable<IconDB>();
            _database.CreateTable<PluginDataDB>();
            _database.CreateTable<PluginSettingsDB>();
            _database.CreateTable<PluginToolStatisticsDB>();
            _database.CreateTable<PluginInfoDB>();
            _database.CreateTable<NavigationTabDB>();
        }
        catch
        {
            // 表创建失败时的处理
        }

    }

    public static void RunInTransaction(Action action)
    {
        if (action == null)
            return;
        _database.RunInTransaction(action);
    }

    // 获取全局唯一设置
    public static SettingDB GetSetting()
    {
        var settings = _database.Table<SettingDB>().FirstOrDefault();
        if (settings == null)
        {
            // 如果不存在设置记录，创建一个新的并保存
            settings = new SettingDB();
            _database.Insert(settings);
        }
        return settings;
    }
    // 更新设置
    public static void UpdateSetting(Action<SettingDB> updateAction)
    {
        var settings = GetSetting();
        updateAction(settings);
        _database.Update(settings);
    }

    #region 图标管理

    /// <summary>
    /// 添加或获取图标，返回其 SHA256 哈希值
    /// </summary>
    public static string? AddOrGetIcon(byte[]? iconData)
    {
        if (iconData == null || iconData.Length == 0)
        {
            return null;
        }

        string hash = IconHashService.ComputeSha256Hash(iconData);

        // 检查图标是否已存在
        IconDB? existingIcon = _database.Table<IconDB>()
            .FirstOrDefault(i => i.Sha256Hash == hash);

        if (existingIcon != null)
        {
            // 图标已存在，增加引用计数并更新访问时间
            existingIcon.ReferenceCount++;
            existingIcon.LastAccessedAt = TimeStampHelper.GetCurrentTimestamp();
            _database.Update(existingIcon);
        }
        else
        {
            // 新图标，插入数据库
            IconDB newIcon = new IconDB
            {
                Sha256Hash = hash,
                IconData = iconData,
                ReferenceCount = 1,
                CreatedAt = TimeStampHelper.GetCurrentTimestamp(),
                LastAccessedAt = TimeStampHelper.GetCurrentTimestamp()
            };
            _database.Insert(newIcon);
        }

        return hash;
    }

    /// <summary>
    /// 根据哈希值获取图标数据
    /// </summary>
    public static byte[]? GetIconData(string? iconHash)
    {
        if (string.IsNullOrEmpty(iconHash))
        {
            return null;
        }

        IconDB? icon = _database.Table<IconDB>()
            .FirstOrDefault(i => i.Sha256Hash == iconHash);

        if (icon != null)
        {
            // 更新最后访问时间
            icon.LastAccessedAt = TimeStampHelper.GetCurrentTimestamp();
            _database.Update(icon);
            return icon.IconData;
        }

        return null;
    }

    /// <summary>
    /// 减少图标引用计数
    /// </summary>
    public static void DecreaseIconReference(string? iconHash)
    {
        if (string.IsNullOrEmpty(iconHash))
        {
            return;
        }

        IconDB? icon = _database.Table<IconDB>()
            .FirstOrDefault(i => i.Sha256Hash == iconHash);

        if (icon != null)
        {
            icon.ReferenceCount--;

            if (icon.ReferenceCount <= 0)
            {
                // 引用计数为0，删除图标
                _database.Delete<IconDB>(icon.Sha256Hash);
            }
            else
            {
                _database.Update(icon);
            }
        }
    }

    /// <summary>
    /// 清理未使用的图标（引用计数为0的）
    /// </summary>
    public static int CleanupUnusedIcons()
    {
        List<IconDB> unusedIcons = _database.Table<IconDB>()
            .Where(i => i.ReferenceCount <= 0)
            .ToList();

        foreach (IconDB icon in unusedIcons)
        {
            _database.Delete<IconDB>(icon.Sha256Hash);
        }

        return unusedIcons.Count;
    }

    #endregion

    #region 应用管理

    // 获取所有应用
    public static List<ApplicationDB> GetApplications()
    {
        return _database.Table<ApplicationDB>().ToList();
    }

    // 添加应用
    public static void AddApplication(ApplicationDB application, byte[]? iconData)
    {
        // 先保存图标并获取哈希值
        application.IconHash = AddOrGetIcon(iconData);

        // 再保存应用
        _database.Insert(application);
    }

    // 删除应用
    public static void DeleteApplication(int id)
    {
        ApplicationDB? app = _database.Table<ApplicationDB>()
            .FirstOrDefault(a => a.Id == id);

        if (app != null)
        {
            // 减少图标引用计数
            DecreaseIconReference(app.IconHash);

            // 删除应用
            _database.Delete<ApplicationDB>(id);
        }
    }

    #endregion

    #region 插件数据管理

    /// <summary>
    /// 获取插件键值
    /// </summary>
    public static string? GetPluginValue(string pluginName, string key)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(key))
        {
            return null;
        }

        string id = $"{pluginName}:{key}";
        PluginDataDB? data = _database.Table<PluginDataDB>()
            .FirstOrDefault(d => d.Id == id);

        return data?.Value;
    }

    /// <summary>
    /// 设置插件键值
    /// </summary>
    public static void SetPluginValue(string pluginName, string key, string value)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(key))
        {
            return;
        }

        string id = $"{pluginName}:{key}";
        PluginDataDB? existingData = null;
        if (_database.Table<PluginDataDB>() != null)
        {
            existingData = _database.Table<PluginDataDB>()?.FirstOrDefault(d => d.Id == id);
        }

        if (existingData != null)
        {
            // 更新现有数据
            existingData.Value = value;
            existingData.UpdatedAt = TimeStampHelper.GetCurrentTimestamp();
            _database.Update(existingData);
        }
        else
        {
            // 插入新数据
            PluginDataDB newData = new PluginDataDB
            {
                Id = id,  // 添加这一行
                PluginName = pluginName,
                Key = key,
                Value = value,
                CreatedAt = TimeStampHelper.GetCurrentTimestamp(),
                UpdatedAt = TimeStampHelper.GetCurrentTimestamp()
            };
            _database.Insert(newData);
        }
    }

    /// <summary>
    /// 删除插件键值
    /// </summary>
    public static void DeletePluginValue(string pluginName, string key)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(key))
        {
            return;
        }

        string id = $"{pluginName}:{key}";
        _database.Delete<PluginDataDB>(id);
    }

    /// <summary>
    /// 获取插件的所有键
    /// </summary>
    public static List<string> GetPluginKeys(string pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return new List<string>();
        }

        return _database.Table<PluginDataDB>()
            .Where(d => d.PluginName == pluginName)
            .Select(d => d.Key)
            .ToList();
    }

    /// <summary>
    /// 删除插件的所有数据
    /// </summary>
    public static void DeleteAllPluginData(string pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return;
        }

        // 删除所有键值对数据
        _database.Execute("DELETE FROM PluginDataDB WHERE PluginName = ?", pluginName);

        // 删除所有图片数据（以 {PluginName}:IMG: 开头的图标）
        string imagePrefix = $"{pluginName}:IMG:";
        List<IconDB> pluginImages = _database.Table<IconDB>()
            .Where(i => i.Sha256Hash.StartsWith(imagePrefix))
            .ToList();

        foreach (IconDB image in pluginImages)
        {
            _database.Delete<IconDB>(image.Sha256Hash);
        }
    }

    /// <summary>
    /// 保存插件图片，返回图片键
    /// </summary>
    public static string? SavePluginImage(string pluginName, string imageKey, byte[]? imageData)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(imageKey) || imageData == null || imageData.Length == 0)
        {
            return null;
        }

        // 使用 {PluginName}:IMG:{ImageKey} 格式作为哈希值
        string hash = $"{pluginName}:IMG:{imageKey}";

        // 检查图片是否已存在
        IconDB? existingIcon = _database.Table<IconDB>().FirstOrDefault(i => i.Sha256Hash == hash);

        if (existingIcon != null)
        {
            // 图片已存在，更新数据和访问时间
            existingIcon.IconData = imageData;
            existingIcon.LastAccessedAt = TimeStampHelper.GetCurrentTimestamp();
            _database.Update(existingIcon);
        }
        else
        {
            // 新图片，插入数据库
            IconDB newIcon = new IconDB
            {
                Sha256Hash = hash,
                IconData = imageData,
                ReferenceCount = 1,
                CreatedAt = TimeStampHelper.GetCurrentTimestamp(),
                LastAccessedAt = TimeStampHelper.GetCurrentTimestamp()
            };
            _database.Insert(newIcon);
        }

        return imageKey;
    }

    /// <summary>
    /// 加载插件图片
    /// </summary>
    public static byte[]? LoadPluginImage(string pluginName, string imageKey)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(imageKey))
        {
            return null;
        }

        string hash = $"{pluginName}:IMG:{imageKey}";
        IconDB? icon = _database.Table<IconDB>().FirstOrDefault(i => i.Sha256Hash == hash);

        if (icon != null)
        {
            // 更新最后访问时间
            icon.LastAccessedAt = TimeStampHelper.GetCurrentTimestamp();
            _database.Update(icon);
            return icon.IconData;
        }

        return null;
    }

    /// <summary>
    /// 删除插件图片
    /// </summary>
    public static void DeletePluginImage(string pluginName, string imageKey)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(imageKey))
        {
            return;
        }

        string hash = $"{pluginName}:IMG:{imageKey}";
        _database.Delete<IconDB>(hash);
    }

    #endregion

    #region 插件设置管理

    /// <summary>
    /// 获取插件设置
    /// </summary>
    public static string? GetPluginSettings(string pluginName, string key)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(key))
        {
            return null;
        }

        string id = $"{pluginName}:{key}";
        PluginSettingsDB? data = _database.Table<PluginSettingsDB>().FirstOrDefault(a => a.Id == id);

        return data?.SettingsValue;
    }

    /// <summary>
    /// 设置插件设置
    /// </summary>
    public static void SetPluginSettings(string pluginName, string key, string value, string? description = null)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(key))
        {
            return;
        }

        string id = $"{pluginName}:{key}";
        PluginSettingsDB? existingData = _database.Table<PluginSettingsDB>().FirstOrDefault(a => a.Id == id);

        if (existingData != null)
        {
            // 更新现有数据
            existingData.SettingsValue = value;
            if (!string.IsNullOrEmpty(description))
            {
                existingData.Description = description;
            }
            existingData.UpdatedAt = TimeStampHelper.GetCurrentTimestamp();
            _database.Update(existingData);
        }
        else
        {
            // 插入新数据
            PluginSettingsDB newData = new PluginSettingsDB
            {
                Id = id,  // 添加这一行
                PluginName = pluginName,
                SettingsKey = key,
                SettingsValue = value,
                Description = description ?? key, // 如果没有描述，使用 SettingsKey
                CreatedAt = TimeStampHelper.GetCurrentTimestamp(),
                UpdatedAt = TimeStampHelper.GetCurrentTimestamp()
            };
            _database.Insert(newData);
        }
    }

    /// <summary>
    /// 删除插件设置
    /// </summary>
    public static void DeletePluginSettings(string pluginName, string key)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(key))
        {
            return;
        }

        string id = $"{pluginName}:{key}";
        _database.Delete<PluginSettingsDB>(id);
    }

    /// <summary>
    /// 获取插件的所有设置
    /// </summary>
    public static List<PluginSettingsDB> GetAllPluginSettings(string pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return new List<PluginSettingsDB>();
        }

        return _database.Table<PluginSettingsDB>()
            .Where(d => d.PluginName == pluginName)
            .ToList();
    }

    /// <summary>
    /// 获取插件的所有设置键
    /// </summary>
    public static List<string> GetAllPluginSettingsKeys(string pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return new List<string>();
        }

        return _database.Table<PluginSettingsDB>()
            .Where(d => d.PluginName == pluginName)
            .Select(d => d.SettingsKey)
            .ToList();
    }

    #endregion

    #region 插件工具统计

    /// <summary>
    /// 保存或更新工具统计
    /// </summary>
    public static void SavePluginToolStatistics(string pluginName, string toolName, Plugin.ToolStatistics stats)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(toolName) || stats == null)
        {
            return;
        }

        string id = $"{pluginName}:{toolName}";
        PluginToolStatisticsDB? db = _database.Table<PluginToolStatisticsDB>()
            .FirstOrDefault(s => s.Id == id);

        long now = TimeStampHelper.GetCurrentTimestamp();

        if (db == null)
        {
            // 新建
            db = new PluginToolStatisticsDB
            {
                Id = id,
                PluginName = pluginName,
                ToolName = toolName,
                FirstCallTime = TimeStampHelper.ToTimestamp(stats.LastCallTime),
                CreatedAt = now
            };
        }

        // 更新统计数据
        db.CallCount = stats.CallCount;
        db.SuccessCount = stats.SuccessCount;
        db.FailureCount = stats.FailureCount;
        db.AverageDurationMs = (long)stats.AverageDuration.TotalMilliseconds;
        db.LastCallTime = TimeStampHelper.ToTimestamp(stats.LastCallTime);
        db.UpdatedAt = now;

        _database.InsertOrReplace(db);
    }

    /// <summary>
    /// 加载单个工具统计
    /// </summary>
    public static Plugin.ToolStatistics? LoadPluginToolStatistics(string pluginName, string toolName)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(toolName))
        {
            return null;
        }

        string id = $"{pluginName}:{toolName}";
        PluginToolStatisticsDB? db = _database.Table<PluginToolStatisticsDB>()
            .FirstOrDefault(s => s.Id == id);

        if (db == null) return null;

        return new Plugin.ToolStatistics
        {
            ToolName = db.ToolName,
            PluginName = db.PluginName,
            CallCount = db.CallCount,
            SuccessCount = db.SuccessCount,
            FailureCount = db.FailureCount,
            AverageDuration = TimeSpan.FromMilliseconds(db.AverageDurationMs),
            LastCallTime = db.LastCallDateTime
        };
    }

    /// <summary>
    /// 加载所有工具统计
    /// </summary>
    public static List<Plugin.ToolStatistics> LoadAllPluginToolStatistics()
    {
        List<PluginToolStatisticsDB> dbStats = _database.Table<PluginToolStatisticsDB>().ToList();

        return dbStats.Select(db => new Plugin.ToolStatistics
        {
            ToolName = db.ToolName,
            PluginName = db.PluginName,
            CallCount = db.CallCount,
            SuccessCount = db.SuccessCount,
            FailureCount = db.FailureCount,
            AverageDuration = TimeSpan.FromMilliseconds(db.AverageDurationMs),
            LastCallTime = db.LastCallDateTime
        }).ToList();
    }

    /// <summary>
    /// 加载指定插件的所有工具统计
    /// </summary>
    public static List<Plugin.ToolStatistics> LoadPluginToolStatisticsByPlugin(string pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return new List<Plugin.ToolStatistics>();
        }

        List<PluginToolStatisticsDB> dbStats = _database.Table<PluginToolStatisticsDB>()
            .Where(s => s.PluginName == pluginName)
            .ToList();

        return dbStats.Select(db => new Plugin.ToolStatistics
        {
            ToolName = db.ToolName,
            PluginName = db.PluginName,
            CallCount = db.CallCount,
            SuccessCount = db.SuccessCount,
            FailureCount = db.FailureCount,
            AverageDuration = TimeSpan.FromMilliseconds(db.AverageDurationMs),
            LastCallTime = db.LastCallDateTime
        }).ToList();
    }

    /// <summary>
    /// 清除指定插件的所有工具统计
    /// </summary>
    public static void ClearPluginToolStatistics(string pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return;
        }

        _database.Execute("DELETE FROM PluginToolStatistics WHERE PluginName = ?", pluginName);
    }

    #endregion

    #region 插件信息管理

    /// <summary>
    /// 获取插件信息
    /// </summary>
    public static PluginInfoDB? GetPluginInfo(string pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return null;
        }

        return _database.Table<PluginInfoDB>()
            .FirstOrDefault(p => p.PluginName == pluginName);
    }

    /// <summary>
    /// 设置插件启用状态
    /// </summary>
    public static void SetPluginEnabled(string pluginName, bool isEnabled)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return;
        }

        PluginInfoDB? pluginInfo = GetPluginInfo(pluginName);
        if (pluginInfo != null)
        {
            pluginInfo.IsEnabled = isEnabled;
            pluginInfo.UpdatedAt = TimeStampHelper.GetCurrentTimestamp();
            _database.Update(pluginInfo);
        }
    }

    /// <summary>
    /// 添加新插件信息记录
    /// </summary>
    public static void AddPluginInfo(PluginInfoDB info)
    {
        if (info == null || string.IsNullOrEmpty(info.PluginName))
        {
            return;
        }

        // 检查插件是否已存在
        PluginInfoDB? existing = GetPluginInfo(info.PluginName);
        if (existing != null)
        {
            // 如果已存在，更新信息
            UpdatePluginInfo(info);
        }
        else
        {
            // 如果不存在，插入新记录
            long now = TimeStampHelper.GetCurrentTimestamp();
            info.InstalledAt = now;
            info.UpdatedAt = now;
            _database.Insert(info);
        }
    }

    /// <summary>
    /// 更新插件信息
    /// </summary>
    public static void UpdatePluginInfo(PluginInfoDB info)
    {
        if (info == null || string.IsNullOrEmpty(info.PluginName))
        {
            return;
        }

        info.UpdatedAt = TimeStampHelper.GetCurrentTimestamp();
        _database.Update(info);
    }

    /// <summary>
    /// 删除插件信息记录
    /// </summary>
    public static void DeletePluginInfo(string pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return;
        }

        _database.Delete<PluginInfoDB>(pluginName);
    }

    /// <summary>
    /// 获取所有插件信息
    /// </summary>
    public static List<PluginInfoDB> GetAllPluginInfos()
    {
        return _database.Table<PluginInfoDB>().ToList();
    }

    /// <summary>
    /// 标记插件为待删除或取消待删除
    /// </summary>
    /// <param name="pluginName">插件唯一名称</param>
    /// <param name="pending">true 标记为待删除，false 取消待删除</param>
    public static void MarkPluginForDeletion(string pluginName, bool pending)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return;
        }

        PluginInfoDB? pluginInfo = GetPluginInfo(pluginName);
        if (pluginInfo != null)
        {
            pluginInfo.PendingDelete = pending;
            pluginInfo.UpdatedAt = TimeStampHelper.GetCurrentTimestamp();
            _database.Update(pluginInfo);
        }
    }

    /// <summary>
    /// 获取所有待删除的插件
    /// </summary>
    public static List<PluginInfoDB> GetPendingDeletePlugins()
    {
        return _database.Table<PluginInfoDB>()
            .Where(p => p.PendingDelete == true)
            .ToList();
    }

    /// <summary>
    /// 标记插件为待更新
    /// </summary>
    /// <param name="pluginName">插件唯一名称</param>
    /// <param name="newVersion">新版本号</param>
    public static void MarkPluginForUpdate(string pluginName, string newVersion)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return;
        }

        PluginInfoDB? pluginInfo = GetPluginInfo(pluginName);
        if (pluginInfo != null)
        {
            pluginInfo.PendingUpdate = true;
            pluginInfo.PendingVersion = newVersion;
            pluginInfo.UpdatedAt = TimeStampHelper.GetCurrentTimestamp();
            _database.Update(pluginInfo);
        }
    }

    /// <summary>
    /// 取消插件更新标记
    /// </summary>
    /// <param name="pluginName">插件唯一名称</param>
    public static void CancelPluginUpdate(string pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return;
        }

        PluginInfoDB? pluginInfo = GetPluginInfo(pluginName);
        if (pluginInfo != null)
        {
            pluginInfo.PendingUpdate = false;
            pluginInfo.PendingVersion = null;
            pluginInfo.UpdatedAt = TimeStampHelper.GetCurrentTimestamp();
            _database.Update(pluginInfo);
        }
    }

    /// <summary>
    /// 获取所有待更新的插件
    /// </summary>
    public static List<PluginInfoDB> GetPendingUpdatePlugins()
    {
        return _database.Table<PluginInfoDB>()
            .Where(p => p.PendingUpdate == true)
            .ToList();
    }

    #endregion

    #region 导航页签配置

    /// <summary>
    /// 获取或创建导航页签配置
    /// </summary>
    public static NavigationTabDB GetNavigationTab(string uniqueId)
    {
        if (NavigationTabDB.Cache.ContainsKey(uniqueId))
            return NavigationTabDB.Cache[uniqueId];

        var tab = _database.Table<NavigationTabDB>().FirstOrDefault(t => t.Id == uniqueId);
        if (tab == null)
        {
            tab = new NavigationTabDB
            {
                Id = uniqueId,
                OrderIndex = 0,
                IsVisible = true
            };
            _database.Insert(tab);
        }
        NavigationTabDB.Cache.Add(uniqueId, tab);
        return tab;
    }

    /// <summary>
    /// 获取所有导航页签配置
    /// </summary>
    public static List<NavigationTabDB> GetAllNavigationTabs()
    {
        return _database.Table<NavigationTabDB>().ToList();
    }

    /// <summary>
    /// 更新导航页签配置
    /// </summary>
    public static void UpdateNavigationTab(NavigationTabDB tab)
    {
        _database.Update(tab);
    }

    #endregion

    // 关闭数据库连接
    public static void Close()
    {
        _database.Close();
    }
}
