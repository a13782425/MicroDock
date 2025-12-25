using Avalonia.Controls;
using MicroDock.Database;
using MicroDock.Plugin;
using MicroDock.Service;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MicroDock.Model;

/// <summary>
/// 插件信息，包含插件实例、上下文和元数据
/// </summary>
public class PluginInfo : IDisposable
{
    /// <summary>
    /// 插件名称
    /// </summary>
    public string DisplayName => Manifest?.EffectiveDisplayName ?? string.Empty;

    /// <summary>
    /// 插件唯一名字
    /// 格式：com.xxxx.xxx
    /// </summary>
    public string UniqueName => Manifest?.Name ?? string.Empty;

    /// <summary>
    /// 插件程序集路径
    /// </summary>
    public string AssemblyFile { get; set; } = string.Empty;

    /// <summary>
    /// 插件依赖所在路径
    /// </summary>
    public string AssemblyDependencyPath { get; set; } = string.Empty;

    /// <summary>
    /// 插件地址
    /// </summary>
    public string PluginPath { get; set; } = string.Empty;

    private PluginInfoDB? _db = null;
    /// <summary>
    /// 当前插件对应的服务器
    /// </summary>
    public PluginInfoDB? InfoDB
    {
        get
        {
            if (Manifest == null)
                return null;
            if (_db != null)
                return _db;
            _db = DBContext.GetPluginInfo(UniqueName);
            if (_db == null)
            {
                _db = new PluginInfoDB
                {
                    PluginName = Manifest.Name,
                    DisplayName = Manifest.EffectiveDisplayName,
                    Version = Manifest.Version,
                    Description = Manifest.Description ?? string.Empty,
                    Author = Manifest.Author ?? string.Empty,
                    IsEnabled = true,
                };
                DBContext.AddPluginInfo(_db);
            }
            return _db;
        }
    }

    /// <summary>
    /// 插件加载上下文
    /// </summary>
    public PluginLoadContext? LoadContext { get; set; }

    /// <summary>
    /// 插件程序集
    /// </summary>
    public Assembly? Assembly { get; set; }

    /// <summary>
    /// 插件主实例（实现IMicroDockPlugin接口）
    /// </summary>
    public IMicroDockPlugin? PluginInstance { get; set; }

    /// <summary>
    /// 插件清单（从 plugin.json 读取）
    /// </summary>
    public PluginManifest? Manifest { get; set; }

    /// <summary>
    /// 是否标记为待删除（默认 false）
    /// </summary>
    public bool PendingDelete { get; set; } = false;

    /// <summary>
    /// 是否有待安装的更新（默认 false）
    /// </summary>
    public bool PendingUpdate { get; set; } = false;

    /// <summary>
    /// 待安装的新版本号（当 PendingUpdate = true 时）
    /// </summary>
    public string? PendingVersion { get; set; }

    /// <summary>
    /// 插件是否已初始化
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// 插件是否已启用
    /// </summary>
    public bool IsEnabled
    {
        get { return InfoDB?.IsEnabled ?? true; }
        set
        {
            if (InfoDB != null)
            {
                InfoDB.IsEnabled = value;
                DBContext.UpdatePluginInfo(InfoDB);
            }
        }
    }

    /// <summary>
    /// 当前插件的所有工具
    /// </summary>
    public Dictionary<string, PluginToolDefinition> ToolDict { get; } = new Dictionary<string, PluginToolDefinition>();

    private bool _disposed = false;

    /// <summary>
    /// 获取tab的唯一Id
    /// </summary>
    /// <param name="tab"></param>
    /// <returns></returns>
    public string GetTabUniqueId(IMicroTab tab)
    {
        return $"{UniqueName.ToLower()}:{tab.GetType().Name.ToLower()}";
    }

    /// <summary>
    /// 释放插件资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            // 调用插件的OnDisable和OnDestroy
            if (PluginInstance != null)
            {
                if (IsEnabled)
                {
                    PluginInstance.OnDisable();
                }
                PluginInstance.OnDestroy();
            }

            // 卸载上下文（如果支持可收集）
            if (LoadContext != null)
            {
                LoadContext.Unload();
                LoadContext = null;
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "释放插件失败: {PluginName}", DisplayName);
        }

        _disposed = true;
    }
}

