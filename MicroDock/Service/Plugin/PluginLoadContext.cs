using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using MicroDock.Model;
using Serilog;

namespace MicroDock.Service;

/// <summary>
/// 插件加载上下文，用于隔离插件程序集
/// 每个插件使用独立的AssemblyLoadContext，避免版本冲突
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly PluginInfo _pluginInfo;

    public PluginLoadContext(PluginInfo pluginInfo) : base(isCollectible: true)
    {
        _pluginInfo = pluginInfo;
        if (!Directory.Exists(pluginInfo.AssemblyDependencyPath))
            Directory.CreateDirectory(pluginInfo.AssemblyDependencyPath);
        _resolver = new AssemblyDependencyResolver(pluginInfo.PluginPath);
        LogDebug($"创建插件加载上下文: {pluginInfo.PluginPath}", DEFAULT_LOG_TAG);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 1. 优先尝试从主程序默认上下文查找已加载的程序集（需要匹配版本）
        Assembly? defaultContextAssembly = AssemblyLoadContext.Default.Assemblies
            .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(
                a.GetName(), assemblyName));

        if (defaultContextAssembly != null)
        {
            LogDebug($"从主程序上下文加载程序集: {assemblyName.Name} (版本: {assemblyName.Version})", DEFAULT_LOG_TAG);
            return null; // 返回 null 使用默认上下文，避免重复加载
        }

        // 2. 如果主程序中未找到或版本不匹配，尝试从插件目录解析
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            LogDebug($"从插件目录加载程序集: {assemblyName.Name} (版本: {assemblyName.Version}) -> {assemblyPath}", DEFAULT_LOG_TAG);
            return LoadFromAssemblyPath(assemblyPath);
        }
        assemblyPath = Path.Combine(_pluginInfo.AssemblyDependencyPath, assemblyName.Name + ".dll");
        if (File.Exists(assemblyPath))
        {
            Log.Debug("从插件目录加载程序集: {AssemblyName} (版本: {Version}) -> {Path}",
             assemblyName.Name, assemblyName.Version, assemblyPath);
            return LoadFromAssemblyPath(assemblyPath);
        }
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            LogDebug($"加载非托管DLL: {unmanagedDllName} -> {libraryPath}", DEFAULT_LOG_TAG);
            return LoadUnmanagedDllFromPath(libraryPath);
        }
        libraryPath = Path.Combine(_pluginInfo.AssemblyDependencyPath, unmanagedDllName);
        if (File.Exists(libraryPath))
        {
            LogDebug($"加载非托管DLL: {unmanagedDllName} -> {libraryPath}", DEFAULT_LOG_TAG);
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 获取插件路径
    /// </summary>
    public PluginInfo PluginInfo => _pluginInfo;
}

