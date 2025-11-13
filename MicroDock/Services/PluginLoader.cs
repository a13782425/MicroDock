using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using MicroDock.Models;
using MicroDock.Plugin;
using Serilog;

namespace MicroDock.Services
{
    /// <summary>
    /// 插件加载器，支持隔离加载和生命周期管理
    /// 注意：所有插件必须提供 plugin.json 配置文件
    /// </summary>
    public class PluginLoader : IDisposable
    {
        private readonly List<PluginInfo> _loadedPlugins = new List<PluginInfo>();
        private bool _disposed = false;

        /// <summary>
        /// 获取所有已加载的插件信息
        /// </summary>
        public IReadOnlyList<PluginInfo> LoadedPlugins => _loadedPlugins.AsReadOnly();

        /// <summary>
        /// 从指定目录加载插件
        /// </summary>
        /// <param name="pluginDirectory">插件目录路径</param>
        /// <returns>加载的插件信息列表</returns>
        public List<PluginInfo> LoadPlugins(string pluginDirectory)
        {
            List<PluginInfo> loadedPlugins = new List<PluginInfo>();

            if (!Directory.Exists(pluginDirectory))
            {
                Log.Information("插件目录不存在，创建目录: {Directory}", pluginDirectory);
                Directory.CreateDirectory(pluginDirectory);
                return loadedPlugins;
            }

            // 第一阶段：扫描并加载所有 plugin.json
            string[] pluginFolders = Directory.GetDirectories(pluginDirectory);
            Log.Information("发现 {Count} 个插件文件夹", pluginFolders.Length);

            var manifestsWithPaths = new List<(string folderPath, PluginManifest manifest)>();
            
            foreach (string pluginFolder in pluginFolders)
            {
                string manifestPath = Path.Combine(pluginFolder, "plugin.json");
                
                if (!File.Exists(manifestPath))
                {
                    Log.Warning("插件文件夹 {Folder} 缺少 plugin.json，跳过加载", pluginFolder);
                    continue;
                }

                try
                {
                    var manifest = LoadManifest(manifestPath);
                    if (manifest != null)
                    {
                        manifestsWithPaths.Add((pluginFolder, manifest));
                        Log.Debug("成功解析 plugin.json: {Name} v{Version}", manifest.Name, manifest.Version);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "解析 plugin.json 失败: {Path}", manifestPath);
                }
            }

            if (manifestsWithPaths.Count == 0)
            {
                Log.Information("未找到有效的插件");
                return loadedPlugins;
            }

            // 第二阶段：解析依赖关系并确定加载顺序
            var manifests = manifestsWithPaths.Select(x => x.manifest).ToList();
            var resolveResult = DependencyResolver.Resolve(manifests);

            if (!resolveResult.Success)
            {
                Log.Error("插件依赖解析失败: {Error}", resolveResult.ErrorMessage);
                return loadedPlugins;
            }

            Log.Information("依赖解析成功，将按顺序加载 {Count} 个插件", resolveResult.OrderedManifests!.Count);

            // 第三阶段：按依赖顺序加载插件
            foreach (var manifest in resolveResult.OrderedManifests!)
            {
                // 找到对应的插件文件夹
                var manifestWithPath = manifestsWithPaths.First(x => x.manifest.Name == manifest.Name);
                
                PluginInfo? pluginInfo = LoadPlugin(manifestWithPath.folderPath, manifest);
                if (pluginInfo != null)
                {
                    loadedPlugins.Add(pluginInfo);
                    _loadedPlugins.Add(pluginInfo);
                }
            }

            Log.Information("成功加载 {Count} 个插件", loadedPlugins.Count);
            return loadedPlugins;
        }

        /// <summary>
        /// 加载并验证 plugin.json 清单文件
        /// </summary>
        private PluginManifest? LoadManifest(string manifestPath)
        {
            try
            {
                string jsonContent = File.ReadAllText(manifestPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var manifest = JsonSerializer.Deserialize<PluginManifest>(jsonContent, options);
                if (manifest == null)
                {
                    Log.Error("plugin.json 反序列化失败: {Path}", manifestPath);
                    return null;
                }

                // 验证清单
                string? validationError = manifest.Validate();
                if (validationError != null)
                {
                    Log.Error("plugin.json 验证失败 ({Path}): {Error}", manifestPath, validationError);
                    return null;
                }

                return manifest;
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "plugin.json 格式错误: {Path}", manifestPath);
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "读取 plugin.json 失败: {Path}", manifestPath);
                return null;
            }
        }

        /// <summary>
        /// 加载单个插件
        /// </summary>
        /// <param name="pluginFolder">插件文件夹路径</param>
        /// <param name="manifest">插件清单</param>
        private PluginInfo? LoadPlugin(string pluginFolder, PluginManifest manifest)
        {
            PluginLoadContext? loadContext = null;

            try
            {
                Log.Debug("开始加载插件: {Name} (文件夹: {PluginFolder})", manifest.Name, pluginFolder);

                // 使用 manifest 中指定的 DLL 文件
                string dllFile = Path.Combine(pluginFolder, manifest.Main);
                
                if (!File.Exists(dllFile))
                {
                    Log.Error("插件 DLL 文件不存在: {DllFile}", dllFile);
                    return null;
                }

                // 创建隔离的加载上下文
                loadContext = new PluginLoadContext(pluginFolder);
                Assembly assembly = loadContext.LoadFromAssemblyPath(dllFile);

                // 使用 manifest 中指定的入口类
                Type? pluginType = assembly.GetType(manifest.EntryClass);
                
                if (pluginType == null)
                {
                    Log.Error("在程序集中未找到入口类: {EntryClass}", manifest.EntryClass);
                    loadContext.Unload();
                    return null;
                }

                // 验证类型是否实现 IMicroDockPlugin 接口
                if (!typeof(IMicroDockPlugin).IsAssignableFrom(pluginType))
                {
                    Log.Error("入口类 {EntryClass} 没有实现 IMicroDockPlugin 接口", manifest.EntryClass);
                    loadContext.Unload();
                    return null;
                }

                if (pluginType.IsAbstract || pluginType.IsInterface)
                {
                    Log.Error("入口类 {EntryClass} 是抽象类或接口", manifest.EntryClass);
                    loadContext.Unload();
                    return null;
                }

                try
                {
                    // 创建插件实例
                    IMicroDockPlugin? dockPlugin = Activator.CreateInstance(pluginType) as IMicroDockPlugin;
                    
                    if (dockPlugin == null)
                    {
                        Log.Error("无法创建插件实例: {Type}", pluginType.Name);
                        loadContext.Unload();
                        return null;
                    }
                    
                    // 创建插件上下文（从 manifest 获取依赖列表）
                    string[] dependencies = manifest.Dependencies?.Keys.ToArray() ?? Array.Empty<string>();
                    PluginContextImpl context = new PluginContextImpl(
                        manifest.Name, 
                        dependencies,
                        pluginFolder);
                    
                    // 调用 Initialize 方法注入上下文（Initialize 内部会调用 OnInit）
                    dockPlugin.Initialize(context);
                    
                    // 通过 Tabs 属性获取所有标签页
                    IMicroTab[]? tabs = dockPlugin.Tabs;
                    if (tabs == null)
                    {
                        Log.Warning("插件 {Name} 的 Tabs 属性返回 null", manifest.Name);
                        tabs = Array.Empty<IMicroTab>();
                    }
                    
                    // 验证每个标签页是否为 Control 类型
                    List<Control> tabControls = new List<Control>();
                    foreach (IMicroTab tab in tabs)
                    {
                        if (tab is Control control)
                        {
                            tabControls.Add(control);
                        }
                        else
                        {
                            Log.Warning("插件 {Name} 的标签页 {TabName} 不是 Control 类型", manifest.Name, tab.TabName);
                        }
                    }
                    
                    Log.Information(
                        "成功加载插件: {DisplayName} ({Name}) v{Version}, 依赖: [{Dependencies}], 标签页数: {TabCount}",
                        manifest.EffectiveDisplayName,
                        manifest.Name,
                        manifest.Version,
                        string.Join(", ", dependencies),
                        tabControls.Count);
                    
                    PluginInfo pluginInfo = new PluginInfo
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = manifest.EffectiveDisplayName,
                        UniqueName = manifest.Name,
                        AssemblyPath = dllFile,
                        LoadContext = loadContext,
                        Assembly = assembly,
                        PluginInstance = dockPlugin,
                        Manifest = manifest,
                        ControlInstance = tabControls.FirstOrDefault(), // 保留第一个标签页作为 ControlInstance（向后兼容）
                        IsInitialized = true,
                        IsEnabled = false
                    };

                    return pluginInfo;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "加载插件失败: {Name}", manifest.Name);
                    loadContext?.Unload();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "加载插件失败: {Name} (文件夹: {PluginFolder})", manifest.Name, pluginFolder);
                
                // 清理失败的上下文
                try
                {
                    loadContext?.Unload();
                }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// 获取插件名称
        /// </summary>
        private string GetPluginName(Control plugin)
        {
            if (plugin is IMicroTab microTab)
            {
                return microTab.TabName;
            }
            return plugin.GetType().Name;
        }

        /// <summary>
        /// 加载插件动作（静态方法，用于独立加载动作）
        /// </summary>
        public static List<MicroAction> LoadActions(string pluginDirectory)
        {
            List<MicroAction> actions = new List<MicroAction>();

            if (!Directory.Exists(pluginDirectory))
            {
                return actions;
            }

            // 扫描插件目录下的直接子文件夹
            string[] pluginFolders = Directory.GetDirectories(pluginDirectory);
            Log.Debug("扫描插件动作，发现 {Count} 个插件文件夹", pluginFolders.Length);

            foreach (string pluginFolder in pluginFolders)
            {
                try
                {
                    // 在插件文件夹中查找 DLL 文件
                    string[] dllFiles = Directory.GetFiles(pluginFolder, "*.dll", SearchOption.TopDirectoryOnly);
                    
                    foreach (string dllFile in dllFiles)
                    {
                        try
                        {
                            // 注意：这里为了向后兼容，暂时使用默认加载方式
                            // 未来可以改为使用隔离上下文
                            Assembly assembly = Assembly.LoadFrom(dllFile);
                            Type[] types = assembly.GetTypes();
                            foreach (Type type in types)
                            {
                                if (typeof(IMicroActionsProvider).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                                {
                                    object? instance = Activator.CreateInstance(type);
                                    IMicroActionsProvider? provider = instance as IMicroActionsProvider;
                                    if (provider != null)
                                    {
                                        foreach (MicroAction action in provider.GetActions())
                                        {
                                            actions.Add(action);
                                            Log.Debug("加载插件动作: {ActionName}", action.Name);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // 忽略单个 DLL 加载失败，但记录错误
                            Log.Warning(ex, "加载插件动作失败: {PluginPath}", dllFile);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 忽略单个插件文件夹加载失败，但记录错误
                    Log.Warning(ex, "扫描插件文件夹失败: {PluginFolder}", pluginFolder);
                }
            }

            Log.Information("成功加载 {Count} 个插件动作", actions.Count);
            return actions;
        }

        /// <summary>
        /// 卸载指定插件
        /// </summary>
        public bool UnloadPlugin(string pluginId)
        {
            PluginInfo? plugin = _loadedPlugins.FirstOrDefault(p => p.Id == pluginId);
            if (plugin == null)
            {
                Log.Warning("尝试卸载不存在的插件: {PluginId}", pluginId);
                return false;
            }

            try
            {
                Log.Information("卸载插件: {PluginName}", plugin.Name);
                plugin.Dispose();
                _loadedPlugins.Remove(plugin);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "卸载插件失败: {PluginName}", plugin.Name);
                return false;
            }
        }

        /// <summary>
        /// 卸载所有插件
        /// </summary>
        public void UnloadAllPlugins()
        {
            Log.Information("卸载所有插件，共 {Count} 个", _loadedPlugins.Count);

            foreach (PluginInfo plugin in _loadedPlugins.ToList())
            {
                try
                {
                    plugin.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "卸载插件时发生错误: {PluginName}", plugin.Name);
                }
            }

            _loadedPlugins.Clear();
        }

        /// <summary>
        /// 释放所有资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            UnloadAllPlugins();
            _disposed = true;
        }
    }
}
