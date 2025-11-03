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
        /// <returns>加载的插件控件列表</returns>
        public List<Control> LoadPlugins(string pluginDirectory)
        {
            List<Control> pluginControls = new List<Control>();

            if (!Directory.Exists(pluginDirectory))
            {
                Log.Information("插件目录不存在，创建目录: {Directory}", pluginDirectory);
                Directory.CreateDirectory(pluginDirectory);
                return pluginControls;
            }

            // 获取所有DLL文件
            string[] dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
            Log.Information("发现 {Count} 个插件文件", dllFiles.Length);

            foreach (string dllFile in dllFiles)
            {
                PluginInfo? pluginInfo = LoadPlugin(dllFile);
                if (pluginInfo?.ControlInstance != null)
                {
                    pluginControls.Add(pluginInfo.ControlInstance);
                    _loadedPlugins.Add(pluginInfo);
                }
            }

            Log.Information("成功加载 {Count} 个插件", pluginControls.Count);
            return pluginControls;
        }

        /// <summary>
        /// 加载单个插件
        /// </summary>
        private PluginInfo? LoadPlugin(string dllFile)
        {
            PluginLoadContext? loadContext = null;

            try
            {
                Log.Debug("开始加载插件: {PluginPath}", dllFile);

                // 创建隔离的加载上下文
                loadContext = new PluginLoadContext(dllFile);
                Assembly assembly = loadContext.LoadFromAssemblyPath(dllFile);

                // 查找实现IMicroTab接口的类型
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (typeof(IMicroTab).IsAssignableFrom(type) && 
                        typeof(Control).IsAssignableFrom(type) && 
                        !type.IsAbstract && 
                        !type.IsInterface)
                    {
                        // 创建插件实例
                        Control? controlInstance = Activator.CreateInstance(type) as Control;
                        if (controlInstance != null)
                        {
                            string pluginName = GetPluginName(controlInstance);
                            
                            PluginInfo pluginInfo = new PluginInfo
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = pluginName,
                                AssemblyPath = dllFile,
                                LoadContext = loadContext,
                                Assembly = assembly,
                                ControlInstance = controlInstance,
                                IsInitialized = false,
                                IsEnabled = false
                            };

                            Log.Information("成功加载插件: {PluginName} ({Path})", pluginName, dllFile);
                            return pluginInfo;
                        }
                    }
                }

                // 未找到有效插件类型，卸载上下文
                loadContext.Unload();
                Log.Debug("插件文件中未找到有效的IMicroTab实现: {PluginPath}", dllFile);
            }
            catch (Exception ex)
            {
                // 记录加载失败的插件
                Log.Error(ex, "加载插件失败: {PluginPath}", dllFile);
                
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

            string[] dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
            Log.Debug("扫描插件动作，发现 {Count} 个文件", dllFiles.Length);

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
                    // 忽略单个插件加载失败，但记录错误
                    Log.Warning(ex, "加载插件动作失败: {PluginPath}", dllFile);
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
