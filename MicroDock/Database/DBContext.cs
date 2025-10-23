using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicroDock.Services;

namespace MicroDock.Database;

internal static class DBContext
{
    private static SQLiteConnection _database;
    static DBContext()
    {   // 获取应用数据目录
        string dbPath = Path.Combine(AppConfig.CONFIG_FOLDER, "gamehub");
        _database = new SQLiteConnection(dbPath);
        // 自动创建表，如果表结构有变化会自动更新
        //_database.CreateTable<ProjectDB>();
        
        // 使用 MigrateTable 来支持添加新列
        try
        {
            _database.CreateTable<SettingDB>();
            _database.CreateTable<ApplicationDB>();
            _database.CreateTable<IconDB>();
            
            // 尝试迁移表结构以支持新增字段
            TableMapping mapping = _database.GetMapping<SettingDB>();
            foreach (TableMapping.Column column in mapping.Columns)
            {
                // 检查并添加缺失的列
                try
                {
                    _database.Execute($"ALTER TABLE SettingDB ADD COLUMN {column.Name} {column.ColumnType}");
                }
                catch
                {
                    // 列已存在，忽略错误
                }
            }
        }
        catch
        {
            // 表创建失败时的处理
        }
        
        //_database.CreateTable<UnityVersionDB>();
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
            existingIcon.LastAccessedAt = DateTime.Now;
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
                CreatedAt = DateTime.Now,
                LastAccessedAt = DateTime.Now
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
            icon.LastAccessedAt = DateTime.Now;
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

    // 关闭数据库连接
    public static void Close()
    {
        _database.Close();
    }
}
