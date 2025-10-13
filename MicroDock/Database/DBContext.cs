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
        _database.CreateTable<SettingDB>();
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
    // 关闭数据库连接
    public static void Close()
    {
        _database.Close();
    }
}
