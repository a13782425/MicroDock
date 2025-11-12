namespace MicroDock.Plugin;

/// <summary>
/// 插件上下文接口，提供日志、数据存储、图片管理等功能
/// </summary>
public interface IPluginContext
{
    #region 日志 API
    
    /// <summary>
    /// 输出调试日志
    /// </summary>
    void LogDebug(string message);
    
    /// <summary>
    /// 输出信息日志
    /// </summary>
    void LogInfo(string message);
    
    /// <summary>
    /// 输出警告日志
    /// </summary>
    void LogWarning(string message);
    
    /// <summary>
    /// 输出错误日志
    /// </summary>
    void LogError(string message, Exception? exception = null);
    
    #endregion
    
    #region 键值存储 API
    
    /// <summary>
    /// 设置键值对
    /// </summary>
    void SetValue(string key, string value);
    
    /// <summary>
    /// 获取键值
    /// </summary>
    string? GetValue(string key);
    
    /// <summary>
    /// 删除键值
    /// </summary>
    void DeleteValue(string key);
    
    /// <summary>
    /// 获取所有键
    /// </summary>
    List<string> GetAllKeys();
    
    #endregion
    
    #region 设置 API
    
    /// <summary>
    /// 获取设置
    /// </summary>
    string? GetSettings(string key);
    
    /// <summary>
    /// 设置设置
    /// </summary>
    void SetSettings(string key, string value, string? description = null);
    
    /// <summary>
    /// 删除设置
    /// </summary>
    void DeleteSettings(string key);
    
    /// <summary>
    /// 获取所有设置键
    /// </summary>
    List<string> GetAllSettingsKeys();
    
    #endregion
    
    #region 依赖访问 API（只读）
    
    /// <summary>
    /// 从依赖插件获取键值（只读）
    /// </summary>
    string? GetValueFromDependency(string dependencyPluginName, string key);
    
    /// <summary>
    /// 获取依赖插件的所有键（只读）
    /// </summary>
    List<string> GetKeysFromDependency(string dependencyPluginName);
    
    /// <summary>
    /// 获取依赖插件的设置（只读）
    /// </summary>
    string? GetSettingsFromDependency(string dependencyPluginName, string key);
    
    /// <summary>
    /// 获取依赖插件的所有设置键（只读）
    /// </summary>
    List<string> GetAllSettingsKeysFromDependency(string dependencyPluginName);
    
    #endregion
    
    #region 图片管理 API
    
    /// <summary>
    /// 保存图片
    /// </summary>
    void SaveImage(string key, byte[] imageData);
    
    /// <summary>
    /// 加载图片
    /// </summary>
    byte[]? LoadImage(string key);
    
    /// <summary>
    /// 删除图片
    /// </summary>
    void DeleteImage(string key);
    
    #endregion
    
    #region 路径 API
    
    /// <summary>
    /// 获取插件配置目录
    /// </summary>
    string ConfigPath { get; }
    
    /// <summary>
    /// 获取插件数据目录
    /// </summary>
    string DataPath { get; }
    
    #endregion
}