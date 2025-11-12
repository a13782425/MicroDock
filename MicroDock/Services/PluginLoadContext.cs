using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Serilog;

namespace MicroDock.Services;

/// <summary>
/// 插件加载上下文，用于隔离插件程序集
/// 每个插件使用独立的AssemblyLoadContext，避免版本冲突
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginPath;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _pluginPath = pluginPath;
        _resolver = new AssemblyDependencyResolver(pluginPath);
        
        Log.Debug("创建插件加载上下文: {PluginPath}", pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 1. 优先尝试从主程序默认上下文查找已加载的程序集
        Assembly? defaultContextAssembly = AssemblyLoadContext.Default.Assemblies
            .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(
                a.GetName(), assemblyName));
        
        if (defaultContextAssembly != null)
        {
            Log.Debug("从主程序上下文加载程序集: {AssemblyName}", assemblyName.Name);
            return null; // 返回 null 使用默认上下文，避免重复加载
        }
        
        // 2. 如果主程序中未找到，尝试从插件目录解析
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            Log.Debug("从插件目录加载程序集: {AssemblyName} -> {Path}", assemblyName.Name, assemblyPath);
            return LoadFromAssemblyPath(assemblyPath);
        }
        
        // 3. 对于共享程序集，使用默认上下文
        if (IsSharedAssembly(assemblyName))
        {
            Log.Debug("使用默认上下文加载共享程序集: {AssemblyName}", assemblyName.Name);
            return null; // 返回null将使用默认上下文
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            Log.Debug("加载非托管DLL: {DllName} -> {Path}", unmanagedDllName, libraryPath);
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 判断是否为共享程序集（应该从默认上下文加载）
    /// </summary>
    private bool IsSharedAssembly(AssemblyName assemblyName)
    {
        string? name = assemblyName.Name;
        if (string.IsNullOrEmpty(name))
            return false;

        // 以下程序集应该与主应用共享
        return name.StartsWith("MicroDock.Plugin") ||
               name.StartsWith("Avalonia") ||
               name.StartsWith("System.") ||
               name.StartsWith("Microsoft.") ||
               name.StartsWith("netstandard") ||
               name == "mscorlib";
    }

    /// <summary>
    /// 获取插件路径
    /// </summary>
    public string PluginPath => _pluginPath;
}

