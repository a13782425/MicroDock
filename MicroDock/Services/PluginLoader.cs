using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using MicroDock.Plugin;
using Serilog;

namespace MicroDock.Services
{
    /// <summary>
    /// 插件加载器，支持隔离加载和生命周期管理
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

            // 扫描插件目录下的直接子文件夹，每个子文件夹作为一个插件目录
            string[] pluginFolders = Directory.GetDirectories(pluginDirectory);
            Log.Information("发现 {Count} 个插件文件夹", pluginFolders.Length);

            foreach (string pluginFolder in pluginFolders)
            {
                PluginInfo? pluginInfo = LoadPlugin(pluginFolder);
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
        /// 加载单个插件
        /// </summary>
        /// <param name="pluginFolder">插件文件夹路径</param>
        private PluginInfo? LoadPlugin(string pluginFolder)
        {
            PluginLoadContext? loadContext = null;
            string? dllFile = null;

            try
            {
                Log.Debug("开始加载插件文件夹: {PluginFolder}", pluginFolder);

                // 在插件文件夹中查找 DLL 文件
                string folderName = Path.GetFileName(pluginFolder);
                string[] dllFiles = Directory.GetFiles(pluginFolder, "*.dll", SearchOption.TopDirectoryOnly);
                
                if (dllFiles.Length == 0)
                {
                    Log.Warning("插件文件夹中未找到 DLL 文件: {PluginFolder}", pluginFolder);
                    return null;
                }

                // 优先查找与文件夹名相同的 DLL，否则使用第一个 DLL
                dllFile = dllFiles.FirstOrDefault(f => 
                    Path.GetFileNameWithoutExtension(f).Equals(folderName, StringComparison.OrdinalIgnoreCase));
                
                if (dllFile == null)
                {
                    dllFile = dllFiles[0];
                    Log.Debug("未找到与文件夹名相同的 DLL，使用第一个 DLL: {DllFile}", dllFile);
                }
                else
                {
                    Log.Debug("找到匹配的 DLL: {DllFile}", dllFile);
                }

                // 创建隔离的加载上下文（使用插件文件夹路径）
                loadContext = new PluginLoadContext(pluginFolder);
                Assembly assembly = loadContext.LoadFromAssemblyPath(dllFile);

                // 查找实现IMicroDockPlugin接口的类型
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (typeof(IMicroDockPlugin).IsAssignableFrom(type) &&
                        !type.IsAbstract && 
                        !type.IsInterface)
                    {
                        try
                        {
                            // 1. 使用无参构造函数创建插件实例（用于读取元数据）
                            IMicroDockPlugin? dockPlugin = Activator.CreateInstance(type) as IMicroDockPlugin;
                            
                            if (dockPlugin == null)
                            {
                                Log.Warning("无法创建插件实例: {Type}", type.Name);
                                continue;
                            }
                            
                            // 2. 从实例读取元数据
                            string uniqueName = dockPlugin.UniqueName;
                            string displayName = dockPlugin.DisplayName;
                            string[] dependencies = dockPlugin.Dependencies ?? Array.Empty<string>();
                            Version version = dockPlugin.PluginVersion;
                            
                            if (string.IsNullOrEmpty(uniqueName))
                            {
                                Log.Warning("插件类型 {Type} 的 UniqueName 为空", type.Name);
                                continue;
                            }
                            
                            // 3. 创建插件上下文（传递插件文件夹路径）
                            PluginContextImpl context = new PluginContextImpl(
                                uniqueName, 
                                dependencies,
                                pluginFolder);
                            
                            // 4. 调用 Initialize 方法注入上下文（Initialize 内部会调用 OnInit）
                            dockPlugin.Initialize(context);
                            
                            // 5. 通过 Tabs 属性获取所有标签页
                            IMicroTab[]? tabs = dockPlugin.Tabs;
                            if (tabs == null)
                            {
                                Log.Warning("插件 {Name} 的 Tabs 属性返回 null", uniqueName);
                                tabs = Array.Empty<IMicroTab>();
                            }
                            
                            // 6. 验证每个标签页是否为 Control 类型
                            List<Control> tabControls = new List<Control>();
                            foreach (IMicroTab tab in tabs)
                            {
                                if (tab is Control control)
                                {
                                    tabControls.Add(control);
                                }
                                else
                                {
                                    Log.Warning("插件 {Name} 的标签页 {TabName} 不是 Control 类型", uniqueName, tab.TabName);
                                }
                            }
                            
                            Log.Information(
                                "成功加载插件: {Name} v{Version}, 依赖: [{Dependencies}], 标签页数: {TabCount}",
                                uniqueName,
                                version,
                                string.Join(", ", dependencies),
                                tabControls.Count);
                            
                            PluginInfo pluginInfo = new PluginInfo
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = displayName,
                                UniqueName = uniqueName,
                                AssemblyPath = dllFile,
                                LoadContext = loadContext,
                                Assembly = assembly,
                                PluginInstance = dockPlugin,
                                ControlInstance = tabControls.FirstOrDefault(), // 保留第一个标签页作为 ControlInstance（向后兼容）
                                IsInitialized = true,
                                IsEnabled = false
                            };

                            return pluginInfo;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "加载插件失败: {Type}", type.Name);
                            throw;
                        }
                    }
                }

                // 未找到有效插件类型，卸载上下文
                loadContext.Unload();
                Log.Debug("插件文件夹中未找到有效的IMicroDockPlugin实现: {PluginFolder}", pluginFolder);
            }
            catch (Exception ex)
            {
                // 记录加载失败的插件
                Log.Error(ex, "加载插件失败: {PluginFolder}", pluginFolder);
                
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
