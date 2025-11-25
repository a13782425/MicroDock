using MicroDock.Database;
using MicroDock.Plugin;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MicroDock.Service;

/// <summary>
/// 插件上下文实现
/// </summary>
internal class PluginContextImpl : IPluginContext
{
    private readonly string _pluginName;
    private readonly string[] _dependencies;
    private readonly string _pluginDirectory;
    private readonly string _configDirectory;
    private readonly string _dataDirectory;

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
        _configDirectory = Path.Combine(_pluginDirectory, "config");
        _dataDirectory = Path.Combine(_pluginDirectory, "data");
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
        }
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    #region 日志 API

    public void LogDebug(string message, string? tag = null)
    {
        LogService.Debug(message, tag ?? _pluginName);
    }

    public void LogInfo(string message, string? tag = null)
    {
        LogService.Information(message, tag ?? _pluginName);
    }

    public void LogWarning(string message, string? tag = null)
    {
        LogService.Warning(message, tag ?? _pluginName);
    }
    public void LogError(string message)
    {
        LogError(message, _pluginName, null);
    }
    public void LogError(string message, Exception? exception = null)
    {
        LogError(message, _pluginName, exception);
    }
    public void LogError(string message, string? tag = null, Exception? exception = null)
    {
        LogService.Error(message, tag ?? _pluginName, exception);
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

    public string ConfigPath => _configDirectory;

    public string DataPath => _dataDirectory;

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
            return await ServiceLocator.Get<ToolRegistry>().CallToolAsync(toolName, parameters, pluginName);
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
            return ServiceLocator.Get<ToolRegistry>().GetAllTools();
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
            return ServiceLocator.Get<ToolRegistry>().GetPluginTools(pluginName);
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
            return ServiceLocator.Get<ToolRegistry>().GetToolInfo(toolName, pluginName);
        }
        catch (Exception ex)
        {
            LogError($"获取工具信息失败: {toolName}", ex);
            return null;
        }
    }

    #endregion

    #region 托盘 API

    public void AddTrayMenuItem(string id, string text, Action onClick)
    {
        try
        {
            // 添加插件名前缀以避免冲突
            string fullId = $"{_pluginName}_{id}";
            ServiceLocator.Get<TrayService>().AddMenuItem(fullId, text, onClick);
            LogDebug($"添加托盘菜单项: {text}");
        }
        catch (Exception ex)
        {
            LogError($"添加托盘菜单项失败: {text}", ex);
        }
    }

    public void RemoveTrayMenuItem(string id)
    {
        try
        {
            // 添加插件名前缀以避免冲突
            string fullId = $"{_pluginName}_{id}";
            ServiceLocator.Get<TrayService>().RemoveMenuItem(fullId);
            LogDebug($"移除托盘菜单项: {id}");
        }
        catch (Exception ex)
        {
            LogError($"移除托盘菜单项失败: {id}", ex);
        }
    }

    public void AddTrayMenuSeparator(string id)
    {
        try
        {
            // 添加插件名前缀以避免冲突
            string fullId = $"{_pluginName}_{id}";
            ServiceLocator.Get<TrayService>().AddSeparator(fullId);
            LogDebug($"添加托盘菜单分隔符: {id}");
        }
        catch (Exception ex)
        {
            LogError($"添加托盘菜单分隔符失败: {id}", ex);
        }
    }

    #endregion

    #region 通知 API

    public void ShowInAppNotification(string title, string message, Plugin.NotificationType type = Plugin.NotificationType.Information)
    {
        try
        {
            if (Program.WindowNotificationManager != null)
            {
                // 将插件的NotificationType转换为Avalonia的NotificationType
                Avalonia.Controls.Notifications.NotificationType avaloniaType = type switch
                {
                    Plugin.NotificationType.Information => Avalonia.Controls.Notifications.NotificationType.Information,
                    Plugin.NotificationType.Success => Avalonia.Controls.Notifications.NotificationType.Success,
                    Plugin.NotificationType.Warning => Avalonia.Controls.Notifications.NotificationType.Warning,
                    Plugin.NotificationType.Error => Avalonia.Controls.Notifications.NotificationType.Error,
                    _ => Avalonia.Controls.Notifications.NotificationType.Information
                };

                // 需要在UI线程上显示通知
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Program.WindowNotificationManager.Show(new Avalonia.Controls.Notifications.Notification(
                        title,
                        message,
                        avaloniaType,
                        TimeSpan.FromSeconds(3)
                    ));
                });

                LogDebug($"显示应用内通知: {title}");
            }
            else
            {
                LogWarning("WindowNotificationManager 未初始化，无法显示应用内通知");
            }
        }
        catch (Exception ex)
        {
            LogError($"显示应用内通知失败: {title}", ex);
        }
    }

    public void ShowSystemNotification(string title, string message, Dictionary<string, string>? buttons = null)
    {
        try
        {
            var notification = new DesktopNotifications.Notification
            {
                Title = title,
                Body = message
            };

            // 添加按钮
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    notification.Buttons.Add((button.Key, button.Value));
                }
            }

            // 需要在UI线程上显示通知
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Program.NotificationManager.ShowNotification(notification, DateTimeOffset.Now + TimeSpan.FromSeconds(5));
            });

            LogDebug($"显示系统托盘通知: {title}");
        }
        catch (Exception ex)
        {
            LogError($"显示系统托盘通知失败: {title}", ex);
        }
    }

    #endregion

    #region Loading API

    public void ShowLoading(string? message = null)
    {
        try
        {
            ServiceLocator.Get<EventService>().Publish(new ShowLoadingMessage(message));
            LogDebug($"显示Loading: {message ?? "(无消息)"}");
        }
        catch (Exception ex)
        {
            LogError("显示Loading失败", ex);
        }
    }

    public void HideLoading()
    {
        try
        {
            ServiceLocator.Get<EventService>().Publish(new HideLoadingMessage());
            LogDebug("隐藏Loading");
        }
        catch (Exception ex)
        {
            LogError("隐藏Loading失败", ex);
        }
    }

    #endregion
}

