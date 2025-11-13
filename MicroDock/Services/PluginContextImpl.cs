using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MicroDock.Database;
using MicroDock.Plugin;
using Serilog;

namespace MicroDock.Services;

/// <summary>
/// 插件上下文实现
/// </summary>
internal class PluginContextImpl : IPluginContext
{
    private readonly string _pluginName;
    private readonly string[] _dependencies;
    private readonly string _pluginDirectory;

    public PluginContextImpl(string pluginName, string[] dependencies, string pluginFolder)
    {
        _pluginName = pluginName;
        _dependencies = dependencies ?? Array.Empty<string>();
        // 使用传入的插件文件夹路径作为数据目录
        _pluginDirectory = pluginFolder;
        
        // 确保插件目录存在
        if (!Directory.Exists(_pluginDirectory))
        {
            Directory.CreateDirectory(_pluginDirectory);
        }
    }

    #region 日志 API

    public void LogDebug(string message)
    {
        Log.Debug("[Plugin:{PluginName}] {Message}", _pluginName, message);
    }

    public void LogInfo(string message)
    {
        Log.Information("[Plugin:{PluginName}] {Message}", _pluginName, message);
    }

    public void LogWarning(string message)
    {
        Log.Warning("[Plugin:{PluginName}] {Message}", _pluginName, message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            Log.Error(exception, "[Plugin:{PluginName}] {Message}", _pluginName, message);
        }
        else
        {
            Log.Error("[Plugin:{PluginName}] {Message}", _pluginName, message);
        }
    }

    #endregion

    #region 键值存储 API

    public void SetValue(string key, string value)
    {
        try
        {
            DBContext.SetPluginValue(_pluginName, key, value);
            LogDebug($"设置键值对: {key}");
        }
        catch (Exception ex)
        {
            LogError($"设置键值失败: {key}", ex);
            throw;
        }
    }

    public string? GetValue(string key)
    {
        try
        {
            return DBContext.GetPluginValue(_pluginName, key);
        }
        catch (Exception ex)
        {
            LogError($"获取键值失败: {key}", ex);
            return null;
        }
    }

    public void DeleteValue(string key)
    {
        try
        {
            DBContext.DeletePluginValue(_pluginName, key);
            LogDebug($"删除键值: {key}");
        }
        catch (Exception ex)
        {
            LogError($"删除键值失败: {key}", ex);
            throw;
        }
    }

    public List<string> GetAllKeys()
    {
        try
        {
            return DBContext.GetPluginKeys(_pluginName);
        }
        catch (Exception ex)
        {
            LogError("获取所有键失败", ex);
            return new List<string>();
        }
    }

    #endregion

    #region 设置 API

    public string? GetSettings(string key)
    {
        try
        {
            return DBContext.GetPluginSettings(_pluginName, key);
        }
        catch (Exception ex)
        {
            LogError($"获取设置失败: {key}", ex);
            return null;
        }
    }

    public void SetSettings(string key, string value, string? description = null)
    {
        try
        {
            DBContext.SetPluginSettings(_pluginName, key, value, description);
            LogDebug($"设置设置: {key}");
        }
        catch (Exception ex)
        {
            LogError($"设置设置失败: {key}", ex);
            throw;
        }
    }

    public void DeleteSettings(string key)
    {
        try
        {
            DBContext.DeletePluginSettings(_pluginName, key);
            LogDebug($"删除设置: {key}");
        }
        catch (Exception ex)
        {
            LogError($"删除设置失败: {key}", ex);
            throw;
        }
    }

    public List<string> GetAllSettingsKeys()
    {
        try
        {
            return DBContext.GetAllPluginSettingsKeys(_pluginName);
        }
        catch (Exception ex)
        {
            LogError("获取所有设置键失败", ex);
            return new List<string>();
        }
    }

    #endregion

    #region 依赖访问 API（只读）

    public string? GetValueFromDependency(string dependencyPluginName, string key)
    {
        // 验证是否为声明的依赖
        if (!_dependencies.Contains(dependencyPluginName))
        {
            LogWarning($"尝试访问未声明的依赖插件: {dependencyPluginName}");
            throw new InvalidOperationException($"插件 '{_pluginName}' 未声明依赖 '{dependencyPluginName}'");
        }

        try
        {
            return DBContext.GetPluginValue(dependencyPluginName, key);
        }
        catch (Exception ex)
        {
            LogError($"从依赖插件 '{dependencyPluginName}' 获取键值失败: {key}", ex);
            return null;
        }
    }

    public List<string> GetKeysFromDependency(string dependencyPluginName)
    {
        // 验证是否为声明的依赖
        if (!_dependencies.Contains(dependencyPluginName))
        {
            LogWarning($"尝试访问未声明的依赖插件: {dependencyPluginName}");
            throw new InvalidOperationException($"插件 '{_pluginName}' 未声明依赖 '{dependencyPluginName}'");
        }

        try
        {
            return DBContext.GetPluginKeys(dependencyPluginName);
        }
        catch (Exception ex)
        {
            LogError($"从依赖插件 '{dependencyPluginName}' 获取键列表失败", ex);
            return new List<string>();
        }
    }

    public string? GetSettingsFromDependency(string dependencyPluginName, string key)
    {
        // 验证是否为声明的依赖
        if (!_dependencies.Contains(dependencyPluginName))
        {
            LogWarning($"尝试访问未声明的依赖插件: {dependencyPluginName}");
            throw new InvalidOperationException($"插件 '{_pluginName}' 未声明依赖 '{dependencyPluginName}'");
        }

        try
        {
            return DBContext.GetPluginSettings(dependencyPluginName, key);
        }
        catch (Exception ex)
        {
            LogError($"从依赖插件 '{dependencyPluginName}' 获取设置失败: {key}", ex);
            return null;
        }
    }

    public List<string> GetAllSettingsKeysFromDependency(string dependencyPluginName)
    {
        // 验证是否为声明的依赖
        if (!_dependencies.Contains(dependencyPluginName))
        {
            LogWarning($"尝试访问未声明的依赖插件: {dependencyPluginName}");
            throw new InvalidOperationException($"插件 '{_pluginName}' 未声明依赖 '{dependencyPluginName}'");
        }

        try
        {
            return DBContext.GetAllPluginSettingsKeys(dependencyPluginName);
        }
        catch (Exception ex)
        {
            LogError($"从依赖插件 '{dependencyPluginName}' 获取设置键列表失败", ex);
            return new List<string>();
        }
    }

    #endregion

    #region 图片管理 API

    public void SaveImage(string key, byte[] imageData)
    {
        try
        {
            DBContext.SavePluginImage(_pluginName, key, imageData);
            LogDebug($"保存图片: {key}, 大小: {imageData.Length} 字节");
        }
        catch (Exception ex)
        {
            LogError($"保存图片失败: {key}", ex);
            throw;
        }
    }

    public byte[]? LoadImage(string key)
    {
        try
        {
            return DBContext.LoadPluginImage(_pluginName, key);
        }
        catch (Exception ex)
        {
            LogError($"加载图片失败: {key}", ex);
            return null;
        }
    }

    public void DeleteImage(string key)
    {
        try
        {
            DBContext.DeletePluginImage(_pluginName, key);
            LogDebug($"删除图片: {key}");
        }
        catch (Exception ex)
        {
            LogError($"删除图片失败: {key}", ex);
            throw;
        }
    }

    #endregion

    #region 路径 API

    public string ConfigPath => _pluginDirectory;

    public string DataPath => _pluginDirectory;

    #endregion

    #region 工具调用 API

    public async System.Threading.Tasks.Task<string> CallToolAsync(
        string toolName,
        Dictionary<string, string> parameters,
        string? pluginName = null)
    {
        try
        {
            LogDebug($"调用工具: {toolName}" + (pluginName != null ? $" (插件: {pluginName})" : ""));
            return await ToolRegistry.Instance.CallToolAsync(toolName, parameters, pluginName);
        }
        catch (Exception ex)
        {
            LogError($"工具调用失败: {toolName}", ex);
            throw;
        }
    }

    public List<Plugin.ToolInfo> GetAvailableTools()
    {
        try
        {
            return ToolRegistry.Instance.GetAllTools();
        }
        catch (Exception ex)
        {
            LogError("获取可用工具列表失败", ex);
            return new List<Plugin.ToolInfo>();
        }
    }

    public List<Plugin.ToolInfo> GetPluginTools(string pluginName)
    {
        try
        {
            return ToolRegistry.Instance.GetPluginTools(pluginName);
        }
        catch (Exception ex)
        {
            LogError($"获取插件工具列表失败: {pluginName}", ex);
            return new List<Plugin.ToolInfo>();
        }
    }

    public Plugin.ToolInfo? GetToolInfo(string toolName, string? pluginName = null)
    {
        try
        {
            return ToolRegistry.Instance.GetToolInfo(toolName, pluginName);
        }
        catch (Exception ex)
        {
            LogError($"获取工具信息失败: {toolName}", ex);
            return null;
        }
    }

    #endregion
}

