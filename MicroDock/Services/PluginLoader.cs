using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using MicroDock.Plugin;

namespace MicroDock.Services
{
    public static class PluginLoader
    {
        /// <summary>
        /// 从指定目录加载插件
        /// </summary>
        /// <param name="pluginDirectory">插件目录路径</param>
        /// <returns>加载的插件控件列表</returns>
        public static List<Control> LoadPlugins(string pluginDirectory)
        {
            List<Control> plugins = new List<Control>();

            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
                return plugins;
            }

            // 获取所有DLL文件
            string[] dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);

            foreach (string dllFile in dllFiles)
            {
                try
                {
                    // 加载程序集
                    Assembly assembly = Assembly.LoadFrom(dllFile);
                    
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
                            Control? pluginInstance = Activator.CreateInstance(type) as Control;
                            if (pluginInstance != null)
                            {
                                plugins.Add(pluginInstance);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录加载失败的插件
                    Console.WriteLine($"Failed to load plugin from {dllFile}: {ex.Message}");
                }
            }

            return plugins;
        }

        /// <summary>
        /// 获取插件名称
        /// </summary>
        public static string GetPluginName(Control plugin)
        {
            if (plugin is IMicroTab microTab)
            {
                return microTab.TabName;
            }
            return plugin.GetType().Name;
        }
    }
}
