using MicroDock.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MicroDock.Service;

/// <summary>
/// 插件待处理操作管理器（使用 JSON 文件存储）
/// </summary>
[AutoRegister(-1)]
public class PluginPendingService : IMicroService
{
    private readonly string _pendingFilePath;
    private readonly object _lock = new();
    private PendingOperations _pendingOperations;
    public PluginPendingService()
    {
        _pendingFilePath = Path.Combine(AppConfig.TEMP_FOLDER, "pending_operations.json");
        _pendingOperations = Load();
    }

    Task IMicroService.OnRegistered()
    {
        ProcessPendingUpdates();

        ProcessPendingDeletes();

        ClearAll();
        return Task.CompletedTask;
    }
    /// <summary>
    /// 处理所有待更新的插件
    /// </summary>
    private void ProcessPendingUpdates()
    {
        try
        {
            if (_pendingOperations.Updates.Count == 0)
            {
                return;
            }

            LogInformation($"发现 {_pendingOperations.Updates.Count} 个待更新插件", DEFAULT_LOG_TAG);


            foreach (var pendingUpdate in _pendingOperations.Updates)
            {
                string pluginName = pendingUpdate.Key;
                PluginUpdateInfo pluginUpdateInfo = pendingUpdate.Value;
                string tempPluginDir = Path.Combine(TEMP_PLUGIN_FOLDER, pluginName);
                string targetPluginDir = Path.Combine(PLUGIN_FOLDER, pluginName);

                LogInformation($"处理待更新插件: {pluginName} v{pluginUpdateInfo.OldVersion} -> v{pluginUpdateInfo.Version}", DEFAULT_LOG_TAG);

                try
                {
                    // 1. 检查临时目录是否存在
                    if (!Directory.Exists(tempPluginDir))
                    {
                        LogWarning($"临时插件目录不存在，跳过更新: {tempPluginDir}", DEFAULT_LOG_TAG);
                        continue;
                    }

                    // 3. 智能更新插件目录（保留 Data 目录，删除 Config）
                    if (Directory.Exists(targetPluginDir))
                    {
                        string dataDir = Path.Combine(targetPluginDir, "Data");
                        string tempDataBackup = null;

                        // 备份 Data 目录（如果存在）
                        try
                        {
                            if (Directory.Exists(dataDir))
                            {
                                tempDataBackup = Path.Combine(TEMP_PLUGIN_FOLDER, $"{pluginName}_Data_Backup");

                                // 如果备份目录已存在，先删除
                                if (Directory.Exists(tempDataBackup))
                                {
                                    Directory.Delete(tempDataBackup, true);
                                }

                                Directory.Move(dataDir, tempDataBackup);
                                LogInformation($"已备份插件数据目录: {dataDir} -> {tempDataBackup}", DEFAULT_LOG_TAG);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"备份 Data 目录失败，将继续更新（数据可能丢失）: {dataDir}, ex: {ex.Message}", DEFAULT_LOG_TAG);
                            tempDataBackup = null; // 确保后续不会尝试恢复
                        }

                        // 删除旧的插件目录（包括 Config，重试机制）
                        int maxRetries = 5;
                        bool deleted = false;

                        for (int i = 0; i < maxRetries && !deleted; i++)
                        {
                            try
                            {
                                Directory.Delete(targetPluginDir, true);
                                deleted = true;
                                LogInformation($"成功删除旧插件目录（Config 已删除）: {targetPluginDir}", DEFAULT_LOG_TAG);
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                if (i < maxRetries - 1)
                                {
                                    LogWarning($"删除插件目录失败，重试 {i + 1}/{maxRetries}: {ex.Message}", DEFAULT_LOG_TAG);
                                    System.Threading.Thread.Sleep(1000);
                                }
                                else
                                {
                                    LogError("删除插件目录失败，已达最大重试次数", DEFAULT_LOG_TAG, ex);
                                    throw;
                                }
                            }
                        }

                        // 移动新插件到目标目录
                        Directory.Move(tempPluginDir, targetPluginDir);
                        LogInformation($"已安装新版本插件: {targetPluginDir}", DEFAULT_LOG_TAG);

                        // 恢复 Data 目录
                        if (tempDataBackup != null && Directory.Exists(tempDataBackup))
                        {
                            try
                            {
                                string restoredDataDir = Path.Combine(targetPluginDir, "Data");
                                Directory.Move(tempDataBackup, restoredDataDir);
                                LogInformation($"已恢复用户数据目录: {tempDataBackup} -> {restoredDataDir}", DEFAULT_LOG_TAG);
                            }
                            catch (Exception ex)
                            {
                                LogError($"恢复插件数据目录失败: {pluginName}，备份位置: {tempDataBackup}", DEFAULT_LOG_TAG, ex);
                                // 数据还在备份目录中，用户可以手动恢复
                            }
                        }
                    }
                    else
                    {
                        // 目标目录不存在，直接移动（首次安装不应该走这个分支）
                        Directory.Move(tempPluginDir, targetPluginDir);
                        LogInformation($"插件目录不存在，直接移动: {tempPluginDir} -> {targetPluginDir}", DEFAULT_LOG_TAG);
                    }

                    // 5. 更新数据库
                    var pluginInfo = DBContext.GetPluginInfo(pluginName);
                    if (pluginInfo != null)
                    {
                        pluginInfo.Version = pluginUpdateInfo.Version;
                        DBContext.UpdatePluginInfo(pluginInfo);
                    }

                    LogInformation($"插件 {pluginName} 更新成功", DEFAULT_LOG_TAG);
                }
                catch (Exception ex)
                {
                    LogError($"处理待更新插件 {pluginName} 失败", DEFAULT_LOG_TAG, ex);
                }
            }
        }
        catch (Exception ex)
        {
            LogError("处理待更新插件时发生错误", ex: ex);
        }
    }

    /// <summary>
    /// 删除所有标记为待删除的插件（启动时调用）
    /// </summary>
    /// <param name="pluginDirectory">插件目录</param>
    private void ProcessPendingDeletes()
    {
        if (_pendingOperations.Deletes.Count == 0)
        {
            return;
        }

        LogInformation($"发现 {_pendingOperations.Deletes.Count} 个待删除的插件", DEFAULT_LOG_TAG);

        foreach (var plugin in _pendingOperations.Deletes)
        {
            string pluginFolder = Path.Combine(PLUGIN_FOLDER, plugin);

            if (Directory.Exists(pluginFolder))
            {
                try
                {
                    Directory.Delete(pluginFolder, true);
                    LogInformation($"已删除插件目录: {pluginFolder}", DEFAULT_LOG_TAG);
                }
                catch (Exception ex)
                {
                    LogError($"删除插件目录失败: {pluginFolder}", DEFAULT_LOG_TAG, ex);
                    // 继续处理其他插件
                }
            }
            else
            {
                LogWarning($"插件目录不存在，跳过文件删除: {pluginFolder}", DEFAULT_LOG_TAG);
            }

            // 清理数据库记录
            try
            {
                DBContext.DeletePluginInfo(plugin);
                LogInformation($"已删除待删除插件: {plugin}", DEFAULT_LOG_TAG);
            }
            catch (Exception ex)
            {
                LogError($"清理插件数据库记录失败: {plugin}", DEFAULT_LOG_TAG, ex);
            }
        }
    }
    #region 待删除操作

    /// <summary>
    /// 标记插件待删除
    /// </summary>
    public void MarkForDelete(string pluginName)
    {
        if (!_pendingOperations.Deletes.Contains(pluginName))
        {
            _pendingOperations.Deletes.Add(pluginName);
            Save();
        }
    }

    /// <summary>
    /// 取消待删除标记
    /// </summary>
    public void CancelDelete(string pluginName)
    {
        if (_pendingOperations.Deletes.Remove(pluginName))
        {
            Save();
        }
    }

    /// <summary>
    /// 检查是否待删除
    /// </summary>
    public bool IsPendingDelete(string pluginName)
    {
        return _pendingOperations.Deletes.Contains(pluginName);
    }

    /// <summary>
    /// 获取所有待删除插件
    /// </summary>
    public List<string> GetPendingDeletePlugins()
    {
        return _pendingOperations.Deletes.ToList();
    }

    #endregion

    #region 待更新操作

    /// <summary>
    /// 标记插件待更新
    /// </summary>
    public void MarkForUpdate(string pluginName, string version)
    {
        var pluginUpdateInfo = new PluginUpdateInfo
        {
            Version = version,
            Path = Path.Combine(TEMP_PLUGIN_FOLDER, pluginName)
        };
        pluginUpdateInfo.OldVersion = DBContext.GetPluginInfo(pluginName)?.Version ?? "";

        _pendingOperations.Updates[pluginName] = pluginUpdateInfo;
        Save();
    }

    /// <summary>
    /// 取消待更新标记
    /// </summary>
    public void CancelUpdate(string pluginName)
    {
        if (_pendingOperations.Updates.Remove(pluginName))
        {
            Save();

            // 同时删除临时文件
            var sourcePath = Path.Combine(TEMP_PLUGIN_FOLDER, pluginName);
            if (Directory.Exists(sourcePath))
            {
                try { Directory.Delete(sourcePath, true); } catch { }
            }
        }
    }

    /// <summary>
    /// 检查是否待更新
    /// </summary>
    public bool IsPendingUpdate(string pluginName)
    {
        return _pendingOperations.Updates.ContainsKey(pluginName);
    }

    /// <summary>
    /// 获取待更新版本号
    /// </summary>
    public string? GetPendingVersion(string pluginName)
    {
        return _pendingOperations.Updates.TryGetValue(pluginName, out var info) ? info.Version : null;
    }

    #endregion

    #region 清理操作

    /// <summary>
    /// 清除已完成的待删除标记
    /// </summary>
    public void ClearDeleteMark(string pluginName)
    {
        CancelDelete(pluginName);
    }

    /// <summary>
    /// 清除已完成的待更新标记
    /// </summary>
    public void ClearUpdateMark(string pluginName)
    {
        if (_pendingOperations.Updates.Remove(pluginName))
        {
            Save();
        }
    }

    /// <summary>
    /// 清空所有待处理操作
    /// </summary>
    public void ClearAll()
    {
        if (File.Exists(_pendingFilePath))
        {
            File.Delete(_pendingFilePath);
        }
    }

    #endregion

    #region 读写 JSON
    private PendingOperations Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_pendingFilePath))
                return new PendingOperations();

            try
            {
                var json = File.ReadAllText(_pendingFilePath);
                return JsonSerializer.Deserialize<PendingOperations>(json) ?? new();
            }
            catch
            {
                return new PendingOperations();
            }
        }
    }

    private void Save()
    {
        lock (_lock)
        {
            var dir = Path.GetDirectoryName(_pendingFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_pendingOperations, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_pendingFilePath, json);
        }
    }
    #endregion

    #region 数据模型

    /*
    * {
    *   "delete": ["PluginA"],
    *   "update": {
    *     "PluginB": {
    *       "version": "1.2.0",
    *       "old_version": "1.1.9",
    *       "source": "temp/plugin/PluginB"
    *     }
    *   }
    * }
     */

    private class PendingOperations
    {
        [JsonPropertyName("deletes")]
        public List<string> Deletes { get; set; } = new();
        [JsonPropertyName("updates")]
        public Dictionary<string, PluginUpdateInfo> Updates { get; set; } = new();
    }

    private class PluginUpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
        [JsonPropertyName("old_version")]
        public string OldVersion { get; set; } = string.Empty;
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }

    #endregion
}
