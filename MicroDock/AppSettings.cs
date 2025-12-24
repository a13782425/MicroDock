using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MicroDock;

/// <summary>
/// 应用设置
/// </summary>
internal class AppSettings
{
    // 配置文件路径
    private static string SettingsFilePath => Path.Combine(AppConfig.ROOT_PATH, "settings.json");

    private static AppSettings _instance = null;

    /// <summary>
    /// 应用配置单例
    /// </summary>
    public static AppSettings Instance
    {
        get
        {
            if (_instance != null)
                return _instance;
            if (!File.Exists(SettingsFilePath))
            {
                _instance = s_initSettings();  // 返回默认值
                Save();
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    AppSettings? temp = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    });
                    if (temp != null)
                        _instance = temp;
                    else
                    {
                        _instance = s_initSettings();
                        Save();
                    }
                }
                catch
                {
                    _instance = s_initSettings();
                    Save();
                }
            }
            return _instance;
        }
    }

    public static void Save()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true  // 格式化输出，便于阅读
        };
        string json = JsonSerializer.Serialize(_instance, options);
        File.WriteAllText(SettingsFilePath, json);
    }

    /// <summary>
    /// 初始化默认值
    /// </summary>
    private static AppSettings s_initSettings()
    {
        var temp = new AppSettings();
        temp.StoragePath = Path.GetFullPath(Path.Combine(AppConfig.ROOT_PATH, "storage") + "/");
        if (!Directory.Exists(temp.StoragePath))
            Directory.CreateDirectory(temp.StoragePath);
        return temp;
    }

    private AppSettings() { }

    /// <summary>
    /// 旧的存储路径
    /// 用来copy
    /// </summary>
    [JsonPropertyName("old_storage_path")]
    public string OldStoragePath { get; set; } = string.Empty;

    [JsonPropertyName("storage_path")]
    public string StoragePath { get; set; } = string.Empty;
}
