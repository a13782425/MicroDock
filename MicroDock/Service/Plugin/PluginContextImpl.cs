using MicroDock.Database;
using MicroDock.Model;
using MicroDock.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MicroDock.Service;

/// <summary>
/// 插件上下文实现
/// </summary>
internal class PluginContextImpl : IPluginContext
{
    private readonly string _pluginName;
    private readonly string _pluginDirectory;
    private readonly string _assetsDirectory;
    private readonly string _configDirectory;
    private readonly string _dataDirectory;
    private readonly string _dllDirectory;
    private readonly string _tempDataDirectory;

    public PluginContextImpl(string pluginName, string pluginFolder)
    {
        _pluginName = pluginName;
        // 使用传入的插件文件夹路径作为数据目录
        _pluginDirectory = pluginFolder;

        // 确保插件目录存在
        if (!Directory.Exists(_pluginDirectory))
        {
            Directory.CreateDirectory(_pluginDirectory);
        }

        _assetsDirectory = Path.Combine(_pluginDirectory, "assets");
        _configDirectory = Path.Combine(_pluginDirectory, "config");
        _dataDirectory = Path.Combine(PLUGIN_DATA_FOLDER, pluginName);
        _dllDirectory = Path.Combine(_pluginDirectory, "dll");
        _tempDataDirectory = Path.Combine(PLUGIN_TEMP_DATA_FOLDER, pluginName);
        if (!Directory.Exists(_assetsDirectory))
            Directory.CreateDirectory(_assetsDirectory);
        if (!Directory.Exists(_configDirectory))
            Directory.CreateDirectory(_configDirectory);
        if (!Directory.Exists(_dataDirectory))
            Directory.CreateDirectory(_dataDirectory);
        if (!Directory.Exists(_dllDirectory))
            Directory.CreateDirectory(_dllDirectory);
        if (!Directory.Exists(_tempDataDirectory))
            Directory.CreateDirectory(_tempDataDirectory);
    }

    #region 日志 API

    public void LogDebug(string message, string? tag = null)
    {
        LogService.LogDebug(message, tag ?? _pluginName);
    }

    public void LogInfo(string message, string? tag = null)
    {
        LogService.LogInformation(message, tag ?? _pluginName);
    }

    public void LogWarning(string message, string? tag = null)
    {
        LogService.LogWarning(message, tag ?? _pluginName);
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
        LogService.LogError(message, tag ?? _pluginName, exception);
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

    public string AssetsPath => _assetsDirectory;

    public string ConfigPath => _configDirectory;

    public string DataPath => _dataDirectory;

    public string TempDataPath => _tempDataDirectory;

    public string DllPath => _dllDirectory;

    #endregion

    #region 插件查询 API

    /// <summary>
    /// 判断指定名称的插件是否已加载
    /// </summary>
    /// <param name="pluginName">插件名称</param>
    /// <returns>如果插件已加载则返回 true，否则返回 false</returns>
    public bool IsPluginLoaded(string pluginName)
    {
        if (string.IsNullOrWhiteSpace(pluginName))
            return false;
        return ServiceLocator.Get<PluginService>()?.GetPluginInfo(pluginName) != null;
    }

    /// <summary>
    /// 判断指定的多个插件是否全部已加载
    /// </summary>
    /// <param name="pluginNames">插件名称列表</param>
    /// <returns>如果所有插件都已加载则返回 true，否则返回 false</returns>
    public bool IsAllPluginsLoaded(params string[] pluginNames)
    {
        bool isResult = true;
        foreach (var item in pluginNames)
        {
            if (!IsPluginLoaded(item))
            {
                isResult = false;
                break;
            }
        }
        return isResult;
    }
    /// <summary>
    /// 判断指定的多个插件是否有任意一个已加载
    /// </summary>
    /// <param name="pluginNames">插件名称列表</param>
    /// <returns>如果任意一个插件已加载则返回 true，否则返回 false</returns>
    public bool IsAnyPluginLoaded(params string[] pluginNames)
    {
        foreach (var item in pluginNames)
        {
            if (IsPluginLoaded(item))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取所有已加载的插件名称列表(含自身)
    /// </summary>
    /// <returns>已加载插件的名称列表</returns>
    public List<string> GetLoadedPluginNames()
    {
        try
        {
            PluginService? pluginService = ServiceLocator.Get<PluginService>();
            if (pluginService == null)
                return new List<string>();

            return pluginService.LoadedPluginDict.Keys.ToList();
        }
        catch (Exception ex)
        {
            LogError("获取已加载插件名称列表失败", DEFAULT_LOG_TAG, ex);
            return new List<string>();
        }
    }

    #endregion

    #region 工具调用 API

    public async Task<string> CallToolAsync(
        string toolName,
        Dictionary<string, string> parameters,
        string? pluginName = null)
    {
        try
        {
            LogDebug($"调用工具: {toolName}" + (pluginName != null ? $" (插件: {pluginName})" : ""));
            return await ServiceLocator.Get<PluginToolService>().CallToolAsync(toolName, parameters, pluginName);
        }
        catch (Exception ex)
        {
            LogError($"工具调用失败: {toolName}", ex);
            throw;
        }
    }

    public List<string> GetAvailableTools()
    {
        List<string> result = new List<string>();
        try
        {
            PluginService? pluginService = ServiceLocator.Get<PluginService>();
            if (pluginService == null)
                return result;
            foreach (var pluginInfo in pluginService.LoadedPlugins)
            {
                result.AddRange(pluginInfo.ToolDict.Keys);
            }
            return result;
        }
        catch (Exception ex)
        {
            LogError("获取可用工具列表失败", ex);
            result.Clear();
            return result;
        }
    }

    public List<string> GetPluginTools(string pluginName)
    {
        List<string> result = new List<string>();
        try
        {
            PluginInfo? pluginInfo = ServiceLocator.Get<PluginService>()?.GetPluginInfo(pluginName);
            if (pluginInfo == null)
                return result;

            return pluginInfo.ToolDict.Keys.ToList();
        }
        catch (Exception ex)
        {
            LogError($"获取插件工具列表失败: {pluginName}", ex);
            result.Clear();
            return result;
        }
    }

    /// <summary>
    /// 判断指定名称的工具是否存在
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <param name="pluginName">可选的插件名称，如果指定则只在该插件中查找</param>
    /// <returns>如果工具存在则返回 true，否则返回 false</returns>
    public bool IsToolAvailable(string toolName, string? pluginName = null)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return false;
        PluginInfo? pluginInfo = ServiceLocator.Get<PluginService>()?.GetPluginInfo(pluginName);
        if (pluginInfo != null)
            return pluginInfo.ToolDict.ContainsKey(toolName);
        return ServiceLocator.Get<PluginService>()?.GlobalToolDict.ContainsKey(toolName) ?? false;
    }
    /// <summary>
    /// 判断多个工具是否全部存在
    /// </summary>
    /// <param name="toolNames">工具名称列表</param>
    /// <returns>如果所有工具都存在则返回 true，否则返回 false</returns>
    public bool IsAllToolsAvailable(params string[] toolNames)
    {
        bool isResult = true;
        foreach (var item in toolNames)
        {
            if (!IsToolAvailable(item))
            {
                isResult = false;
                break;
            }
        }
        return isResult;
    }
    /// <summary>
    /// 判断多个工具是否有任意一个存在
    /// </summary>
    /// <param name="toolNames">工具名称列表</param>
    /// <returns>如果任意一个工具存在则返回 true，否则返回 false</returns>
    public bool IsAnyToolAvailable(params string[] toolNames)
    {
        foreach (var item in toolNames)
        {
            if (IsToolAvailable(item))
            {
                return true;
            }
        }
        return false;
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

    public void ShowInAppNotification(string title, string message, PluginNotificationType type = PluginNotificationType.Information)
    {
        try
        {
            if (MicroWindowNotificationManager != null)
            {
                // 将插件的NotificationType转换为Avalonia的NotificationType
                AppNotificationType avaloniaType = type switch
                {
                    PluginNotificationType.Information => AppNotificationType.Information,
                    PluginNotificationType.Success => AppNotificationType.Success,
                    PluginNotificationType.Warning => AppNotificationType.Warning,
                    PluginNotificationType.Error => AppNotificationType.Error,
                    _ => AppNotificationType.Information
                };

                // 需要在UI线程上显示通知
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    MicroWindowNotificationManager.Show(new Avalonia.Controls.Notifications.Notification(
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
                MicroNotificationManager.ShowNotification(notification, DateTimeOffset.Now + TimeSpan.FromSeconds(5));
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

