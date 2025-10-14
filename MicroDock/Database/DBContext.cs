using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    
    // 获取所有应用
    public static List<ApplicationDB> GetApplications()
    {
        return _database.Table<ApplicationDB>().ToList();
    }
    
    // 添加应用
    public static void AddApplication(ApplicationDB application)
    {
        _database.Insert(application);
    }
    
    // 删除应用
    public static void DeleteApplication(int id)
    {
        _database.Delete<ApplicationDB>(id);
    }

    // 关闭数据库连接
    public static void Close()
    {
        _database.Close();
    }
}
