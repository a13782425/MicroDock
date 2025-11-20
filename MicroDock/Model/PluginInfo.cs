using System;
using System.Reflection;
using Avalonia.Controls;
using MicroDock.Plugin;

namespace MicroDock.Service;

/// <summary>
/// 插件信息，包含插件实例、上下文和元数据
/// </summary>
public class PluginInfo : IDisposable
{
    /// <summary>
    /// 插件的唯一标识符
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 插件名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 插件唯一名字
    /// 格式：com.xxxx.xxx
    /// </summary>
    public string UniqueName { get; set; } = string.Empty;

    /// <summary>
    /// 插件程序集路径
    /// </summary>
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// 插件加载上下文
    /// </summary>
    public PluginLoadContext? LoadContext { get; set; }

    /// <summary>
    /// 插件程序集
    /// </summary>
    public Assembly? Assembly { get; set; }

    /// <summary>
    /// 插件控件实例（用于UI集成）
    /// </summary>
    public Control? ControlInstance { get; set; }

    /// <summary>
    /// 插件主实例（实现IMicroDockPlugin接口）
    /// </summary>
    public IMicroDockPlugin? PluginInstance { get; set; }

    /// <summary>
    /// 插件清单（从 plugin.json 读取）
    /// </summary>
    public Model.PluginManifest? Manifest { get; set; }

    /// <summary>
    /// 插件是否已初始化
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// 插件是否已启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 加载时间
    /// </summary>
    public DateTime LoadedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 加载错误信息（如果有）
    /// </summary>
    public string? ErrorMessage { get; set; }

    private bool _disposed = false;

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
            Serilog.Log.Error(ex, "释放插件失败: {PluginName}", Name);
        }

        _disposed = true;
    }
}

