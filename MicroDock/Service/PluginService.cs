using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using MicroDock.Database;
using MicroDock.Model;
using MicroDock.Plugin;
using Serilog;

namespace MicroDock.Service
{
    /// <summary>
    /// 插件加载器，支持隔离加载和生命周期管理
    /// 注意：所有插件必须提供 plugin.json 配置文件
    /// </summary>
    public class PluginService : IDisposable
    {
        private readonly List<PluginInfo> _loadedPlugins = new List<PluginInfo>();
        private bool _disposed = false;

        /// <summary>
        /// 公共构造函数，用于 ServiceLocator 注册
        /// </summary>
        public PluginService()
        {
        }

        /// <summary>
        /// 获取所有已加载的插件信息
        /// </summary>
        public IReadOnlyList<PluginInfo> LoadedPlugins => _loadedPlugins.AsReadOnly();

        /// <summary>
        /// 导入插件（从 ZIP 文件）
        /// </summary>
        /// <param name="zipFilePath">ZIP 文件路径</param>
        /// <param name="pluginDirectory">插件目录路径</param>
        /// <returns>导入结果（成功/失败，消息）</returns>
        public async Task<(bool success, string message, string? pluginName)> ImportPluginAsync(string zipFilePath, string pluginDirectory)
        {
            string? tempDirectory = null;
            string? pluginName = null;

            try
            {
                // 1. 验证 ZIP 文件存在
                if (!File.Exists(zipFilePath))
                {
                    return (false, "ZIP 文件不存在", null);
                }

                // 2. 创建临时目录并解压
                tempDirectory = Path.Combine(AppConfig.TEMP_IMPORT_FOLDER, $"MicroDockPlugin_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDirectory);

                Log.Information("正在解压插件到临时目录: {TempDir}", tempDirectory);
                await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, tempDirectory));

                // 3. 验证根目录是否存在 plugin.json
                string manifestPath = Path.Combine(tempDirectory, "plugin.json");
                if (!File.Exists(manifestPath))
                {
                    return (false, "ZIP 根目录缺少 plugin.json 文件", null);
                }

                // 4. 解析 plugin.json 获取插件名和版本
                PluginManifest? manifest = LoadManifest(manifestPath);
                if (manifest == null)
                {
                    return (false, "plugin.json 解析失败", null);
                }

                pluginName = manifest.Name;
                string newVersion = manifest.Version;
                Log.Information("正在导入插件: {PluginName} v{Version}", pluginName, newVersion);

                // 5. 检查数据库中是否已存在该插件
                PluginInfoDB? existingPluginInfo = DBContext.GetPluginInfo(pluginName);

                if (existingPluginInfo != null)
                {
                    // 插件已存在，检查版本
                    string currentVersion = existingPluginInfo.Version;

                    if (currentVersion == newVersion)
                    {
                        // 版本相同，提示已安装
                        Log.Information("插件 {PluginName} 版本 {Version} 已安装", pluginName, currentVersion);
                        return (false, $"该插件已安装（版本 v{currentVersion}）", pluginName);
                    }
                    else
                    {
                        // 版本不同，标记为待更新
                        Log.Information("插件 {PluginName} 版本不同: {CurrentVersion} -> {NewVersion}，标记为待更新",
                            pluginName, currentVersion, newVersion);

                        // 使用统一的插件临时目录
                        string pluginTempDirectory = AppConfig.TEMP_PLUGIN_FOLDER;

                        // 解压到 temp/plugin/[PluginName] 目录
                        string tempPluginDir = Path.Combine(pluginTempDirectory, pluginName);

                        // 如果临时目录已存在，先删除
                        if (Directory.Exists(tempPluginDir))
                        {
                            try
                            {
                                Directory.Delete(tempPluginDir, true);
                                await Task.Delay(100);
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, "删除旧的临时插件目录失败: {Dir}", tempPluginDir);
                            }
                        }

                        // 复制文件到临时目录
                        Directory.CreateDirectory(tempPluginDir);
                        await Task.Run(() => CopyDirectory(tempDirectory, tempPluginDir));
                        Log.Information("插件文件已复制到临时目录: {TempDir}", tempPluginDir);

                        // 在数据库中标记为待更新
                        DBContext.MarkPluginForUpdate(pluginName, newVersion);

                        return (true, $"插件将在下次重启时更新：v{currentVersion} → v{newVersion}", pluginName);
                    }
                }
                else
                {
                    // 插件不存在，直接导入
                    Log.Information("插件 {PluginName} 不存在，直接导入", pluginName);

                    // 确保插件目录存在
                    if (!Directory.Exists(pluginDirectory))
                    {
                        Directory.CreateDirectory(pluginDirectory);
                    }

                    // 复制文件到 Plugins/{插件名}/ 目录
                    string targetPluginDir = Path.Combine(pluginDirectory, pluginName);
                    Directory.CreateDirectory(targetPluginDir);
                    await Task.Run(() => CopyDirectory(tempDirectory, targetPluginDir));

                    Log.Information("插件文件已复制到: {TargetDir}", targetPluginDir);

                    // 验证插件加载
                    PluginInfo? pluginInfo = await LoadPluginAsync(targetPluginDir, manifest);
                    if (pluginInfo == null)
                    {
                        // 加载失败，清理已复制的文件
                        try
                        {
                            Directory.Delete(targetPluginDir, true);
                        }
                        catch { }
                        return (false, "插件加载验证失败", pluginName);
                    }

                    // 添加到内存列表
                    _loadedPlugins.Add(pluginInfo);

                    // 添加到数据库
                    PluginInfoDB dbInfo = new PluginInfoDB
                    {
                        PluginName = manifest.Name,
                        DisplayName = manifest.EffectiveDisplayName,
                        Version = manifest.Version,
                        Description = manifest.Description ?? string.Empty,
                        Author = manifest.Author ?? string.Empty,
                        IsEnabled = true,
                    };
                    DBContext.AddPluginInfo(dbInfo);

                    Log.Information("插件 {PluginName} 导入成功", pluginName);
                    return (true, $"插件已导入：{manifest.EffectiveDisplayName} v{manifest.Version}", pluginName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "导入插件失败: {ZipFile}", zipFilePath);
                return (false, $"导入失败: {ex.Message}", pluginName);
            }
            finally
            {
                // 10. 清理临时文件
                if (!string.IsNullOrEmpty(tempDirectory) && Directory.Exists(tempDirectory))
                {
                    try
                    {
                        Directory.Delete(tempDirectory, true);
                        Log.Debug("临时目录已清理: {TempDir}", tempDirectory);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "清理临时目录失败: {TempDir}", tempDirectory);
                    }
                }
            }
        }

        /// <summary>
        /// 递归复制目录
        /// </summary>
        private void CopyDirectory(string sourceDir, string targetDir)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"源目录不存在: {sourceDir}");
            }

            // 复制所有文件
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string targetFilePath = Path.Combine(targetDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // 递归复制子目录
            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (DirectoryInfo subDir in dirs)
            {
                string targetSubDir = Path.Combine(targetDir, subDir.Name);
                Directory.CreateDirectory(targetSubDir);
                CopyDirectory(subDir.FullName, targetSubDir);
            }
        }

        /// <summary>
        /// 处理所有待更新的插件
        /// </summary>
        /// <param name="pluginDirectory">插件目录路径</param>
        private void ProcessPendingUpdates(string pluginDirectory)
        {
            try
            {
                // 获取所有待更新的插件
                List<PluginInfoDB> pendingUpdatePlugins = DBContext.GetPendingUpdatePlugins();

                if (pendingUpdatePlugins.Count == 0)
                {
                    return;
                }

                Log.Information("发现 {Count} 个待更新插件", pendingUpdatePlugins.Count);

                string pluginTempDirectory = AppConfig.TEMP_PLUGIN_FOLDER;

                foreach (var pluginInfo in pendingUpdatePlugins)
                {
                    string pluginName = pluginInfo.PluginName;
                    string tempPluginDir = Path.Combine(pluginTempDirectory, pluginName);
                    string targetPluginDir = Path.Combine(pluginDirectory, pluginName);

                    Log.Information("处理待更新插件: {PluginName} v{OldVersion} -> v{NewVersion}",
                        pluginName, pluginInfo.Version, pluginInfo.PendingVersion);

                    try
                    {
                        // 1. 检查临时目录是否存在
                        if (!Directory.Exists(tempPluginDir))
                        {
                            Log.Warning("临时插件目录不存在，跳过更新: {Dir}", tempPluginDir);
                            // 清除待更新标记
                            DBContext.CancelPluginUpdate(pluginName);
                            continue;
                        }

                        // 2. 卸载旧版本插件（如果已加载）
                        PluginInfo? existingPlugin = _loadedPlugins.FirstOrDefault(p => p.UniqueName == pluginName);
                        if (existingPlugin != null)
                        {
                            Log.Information("卸载旧版本插件: {PluginName}", pluginName);
                            try
                            {
                                existingPlugin.PluginInstance?.OnDestroy();
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, "调用插件 OnDestroy 失败");
                            }

                            // 从加载列表中移除
                            _loadedPlugins.Remove(existingPlugin);

                            // 卸载程序集上下文
                            try
                            {
                                existingPlugin.LoadContext?.Unload();
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, "卸载插件上下文失败");
                            }

                            // 强制垃圾回收
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
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
                                    tempDataBackup = Path.Combine(pluginTempDirectory, $"{pluginName}_Data_Backup");

                                    // 如果备份目录已存在，先删除
                                    if (Directory.Exists(tempDataBackup))
                                    {
                                        Directory.Delete(tempDataBackup, true);
                                    }

                                    Directory.Move(dataDir, tempDataBackup);
                                    Log.Information("已备份插件数据目录: {DataDir} -> {BackupDir}", dataDir, tempDataBackup);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, "备份 Data 目录失败，将继续更新（数据可能丢失）: {DataDir}", dataDir);
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
                                    Log.Information("成功删除旧插件目录（Config 已删除）: {Dir}", targetPluginDir);
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    if (i < maxRetries - 1)
                                    {
                                        Log.Warning("删除插件目录失败，重试 {Retry}/{MaxRetries}: {Message}",
                                            i + 1, maxRetries, ex.Message);
                                        System.Threading.Thread.Sleep(1000);
                                    }
                                    else
                                    {
                                        Log.Error(ex, "删除插件目录失败，已达最大重试次数");
                                        throw;
                                    }
                                }
                            }

                            // 移动新插件到目标目录
                            Directory.Move(tempPluginDir, targetPluginDir);
                            Log.Information("已安装新版本插件: {Dir}", targetPluginDir);

                            // 恢复 Data 目录
                            if (tempDataBackup != null && Directory.Exists(tempDataBackup))
                            {
                                try
                                {
                                    string restoredDataDir = Path.Combine(targetPluginDir, "Data");
                                    Directory.Move(tempDataBackup, restoredDataDir);
                                    Log.Information("已恢复用户数据目录: {BackupDir} -> {DataDir}", tempDataBackup, restoredDataDir);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "恢复插件数据目录失败: {PluginName}，备份位置: {BackupDir}",
                                        pluginName, tempDataBackup);
                                    // 数据还在备份目录中，用户可以手动恢复
                                }
                            }
                        }
                        else
                        {
                            // 目标目录不存在，直接移动（首次安装不应该走这个分支）
                            Directory.Move(tempPluginDir, targetPluginDir);
                            Log.Information("插件目录不存在，直接移动: {From} -> {To}",
                                tempPluginDir, targetPluginDir);
                        }

                        // 5. 更新数据库
                        pluginInfo.Version = pluginInfo.PendingVersion ?? pluginInfo.Version;
                        pluginInfo.PendingUpdate = false;
                        pluginInfo.PendingVersion = null;
                        DBContext.UpdatePluginInfo(pluginInfo);

                        Log.Information("插件 {PluginName} 更新成功", pluginName);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "处理待更新插件 {PluginName} 失败", pluginName);
                        // 不清除待更新标记，下次启动时再试
                    }
                }

                // 清理临时插件目录中的残留文件
                if (Directory.Exists(pluginTempDirectory))
                {
                    try
                    {
                        var remainingDirs = Directory.GetDirectories(pluginTempDirectory);
                        foreach (var dir in remainingDirs)
                        {
                            try
                            {
                                Directory.Delete(dir, true);
                                Log.Information("清理残留临时目录: {Dir}", dir);
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, "清理残留临时目录失败: {Dir}", dir);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "清理临时插件目录失败");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "处理待更新插件时发生错误");
            }
        }

        /// <summary>
        /// 从指定目录异步加载所有插件
        /// </summary>
        /// <returns>加载的插件信息列表</returns>
        public async Task<List<PluginInfo>> LoadPluginsAsync()
        {
            string pluginDirectory = Path.Combine(AppConfig.ROOT_PATH, "plugins");
            List<PluginInfo> loadedPlugins = new List<PluginInfo>();

            if (!Directory.Exists(pluginDirectory))
            {
                LogService.LogInformation($"插件目录不存在，创建目录: {pluginDirectory}");
                Directory.CreateDirectory(pluginDirectory);
                return loadedPlugins;
            }

            // 首先处理所有待更新的插件
            ProcessPendingUpdates(pluginDirectory);

            // 然后删除所有标记为待删除的插件
            DeletePendingPlugins(pluginDirectory);

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

            // 第三阶段：按依赖顺序异步加载插件
            foreach (var manifest in resolveResult.OrderedManifests!)
            {
                // 找到对应的插件文件夹
                var manifestWithPath = manifestsWithPaths.First(x => x.manifest.Name == manifest.Name);

                PluginInfo? pluginInfo = await LoadPluginAsync(manifestWithPath.folderPath, manifest);
                if (pluginInfo != null)
                {
                    loadedPlugins.Add(pluginInfo);
                    _loadedPlugins.Add(pluginInfo);
                }
                await Task.Delay(100); // 小延迟，避免阻塞
            }

            Log.Information("成功加载 {Count} 个插件", loadedPlugins.Count);

            // 第四阶段：所有插件加载完成，触发 OnAllPluginsLoaded 回调
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.PluginInstance?.OnAllPluginsLoaded();
                    Log.Debug("插件 {Name} 的 OnAllPluginsLoaded 回调已触发", plugin.UniqueName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "插件 {Name} 的 OnAllPluginsLoaded 回调失败", plugin.UniqueName);
                }
            }

            return loadedPlugins;
        }
        /// <summary>
        /// 异步加载单个插件
        /// </summary>
        /// <param name="pluginFolder">插件文件夹路径</param>
        /// <param name="manifest">插件清单</param>
        private async Task<PluginInfo?> LoadPluginAsync(string pluginFolder, PluginManifest manifest)
        {
            PluginLoadContext? loadContext = null;

            try
            {
                Log.Debug("开始加载插件: {Name} (文件夹: {PluginFolder})", manifest.Name, pluginFolder);

                string dllFile = Path.Combine(pluginFolder, manifest.Main);
                if (!File.Exists(dllFile))
                {
                    Log.Error("插件 DLL 文件不存在: {DllFile}", dllFile);
                    return null;
                }

                loadContext = new PluginLoadContext(pluginFolder);
                Assembly assembly = loadContext.LoadFromAssemblyPath(dllFile);
                Type? pluginType = assembly.GetType(manifest.EntryClass);

                if (pluginType == null)
                {
                    Log.Error("在程序集中未找到入口类: {EntryClass}", manifest.EntryClass);
                    loadContext.Unload();
                    return null;
                }

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

                IMicroDockPlugin? dockPlugin = Activator.CreateInstance(pluginType) as IMicroDockPlugin;
                if (dockPlugin == null)
                {
                    Log.Error("无法创建插件实例: {Type}", pluginType.Name);
                    loadContext.Unload();
                    return null;
                }

                string[] dependencies = manifest.Dependencies?.Keys.ToArray() ?? Array.Empty<string>();
                PluginContextImpl context = new PluginContextImpl(manifest.Name, pluginFolder);
                dockPlugin.Initialize(context);
                // 异步初始化插件
                await dockPlugin.OnInitAsync();
                Log.Debug("插件 {Name} 异步初始化完成", manifest.Name);

                DiscoverAndRegisterTools(dockPlugin, manifest.Name);

                IMicroTab[]? tabs = dockPlugin.Tabs ?? Array.Empty<IMicroTab>();
                List<Control> tabControls = new List<Control>();
                foreach (IMicroTab tab in tabs)
                {
                    if (tab is Control control)
                        tabControls.Add(control);
                    else
                        LogWarning($"插件 {manifest.Name} 的标签页 {tab.TabName} 不是 Control 类型");
                }

                Log.Information("成功加载插件: {DisplayName} ({Name}) v{Version}, 依赖: [{Dependencies}], 标签页数: {TabCount}",
                    manifest.EffectiveDisplayName, manifest.Name, manifest.Version, string.Join(", ", dependencies), tabControls.Count);

                PluginInfoDB? dbInfo = DBContext.GetPluginInfo(manifest.Name);
                if (dbInfo?.PendingDelete == true)
                {
                    Log.Information("跳过待删除插件: {PluginName}", manifest.Name);
                    return null;
                }

                bool isEnabled = dbInfo?.IsEnabled ?? true;
                if (dbInfo == null)
                {
                    dbInfo = new PluginInfoDB
                    {
                        PluginName = manifest.Name,
                        DisplayName = manifest.EffectiveDisplayName,
                        Version = manifest.Version,
                        Description = manifest.Description ?? string.Empty,
                        Author = manifest.Author ?? string.Empty,
                        IsEnabled = true,
                    };
                    DBContext.AddPluginInfo(dbInfo);
                }
                else if (dbInfo.Version != manifest.Version)
                {
                    dbInfo.Version = manifest.Version;
                    dbInfo.DisplayName = manifest.EffectiveDisplayName;
                    dbInfo.Description = manifest.Description ?? string.Empty;
                    dbInfo.Author = manifest.Author ?? string.Empty;
                    DBContext.UpdatePluginInfo(dbInfo);
                }

                return new PluginInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = manifest.EffectiveDisplayName,
                    UniqueName = manifest.Name,
                    AssemblyPath = dllFile,
                    LoadContext = loadContext,
                    Assembly = assembly,
                    PluginInstance = dockPlugin,
                    Manifest = manifest,
                    ControlInstance = tabControls.FirstOrDefault(),
                    IsInitialized = true,
                    IsEnabled = isEnabled
                };
            }
            catch (Exception ex)
            {
                LogError($"加载插件失败: {manifest.Name}", DEFAULT_LOG_TAG, ex);
                loadContext?.Unload();
                return null;
            }
        }

        /// <summary>
        /// 获取友好的类型名称
        /// </summary>
        private string GetFriendlyTypeName(Type type)
        {
            // 处理可空类型
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                return GetFriendlyTypeName(underlyingType) + "?";
            }

            // 基本类型
            if (type == typeof(int)) return "int";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(double)) return "double";
            if (type == typeof(float)) return "float";
            if (type == typeof(long)) return "long";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(short)) return "short";
            if (type == typeof(char)) return "char";

            // 泛型类型
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();

                if (genericTypeDef == typeof(List<>))
                {
                    return $"List<{GetFriendlyTypeName(genericArgs[0])}>";
                }
                if (genericTypeDef == typeof(Dictionary<,>))
                {
                    return $"Dictionary<{GetFriendlyTypeName(genericArgs[0])}, {GetFriendlyTypeName(genericArgs[1])}>";
                }

                // 其他泛型类型
                var genericArgNames = string.Join(", ", genericArgs.Select(GetFriendlyTypeName));
                return $"{type.Name.Split('`')[0]}<{genericArgNames}>";
            }

            // 数组类型
            if (type.IsArray)
            {
                return GetFriendlyTypeName(type.GetElementType()!) + "[]";
            }

            // 复杂类型返回类名
            return type.Name;
        }

        /// <summary>
        /// 自动发现并注册插件工具
        /// </summary>
        private void DiscoverAndRegisterTools(IMicroDockPlugin plugin, string pluginName)
        {
            try
            {
                int toolCount = 0;

                // 获取插件程序集
                var assembly = plugin.GetType().Assembly;
                var pluginType = plugin.GetType();

                // 扫描程序集中的所有类型（公共和非公共）
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    // 跳过抽象类和接口
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    // 扫描该类型的所有公共和非公共方法（实例 + 静态）
                    var methods = type.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.DeclaredOnly);

                    foreach (var method in methods)
                    {
                        var toolAttr = method.GetCustomAttribute<Plugin.MicroToolAttribute>();
                        if (toolAttr == null) continue;

                        // 验证返回类型
                        if (method.ReturnType != typeof(System.Threading.Tasks.Task<string>))
                        {
                            Log.Warning("插件 {Plugin} 的工具方法 {Type}.{Method} 必须返回 Task<string>，已跳过",
                                pluginName, type.Name, method.Name);
                            continue;
                        }

                        // 确定实例策略
                        object? targetInstance = null;
                        bool needsLazyInstance = false;

                        if (method.IsStatic)
                        {
                            // 静态方法：不需要实例
                            Log.Debug("发现静态工具方法: {Type}.{Method}", type.Name, method.Name);
                        }
                        else if (type == pluginType)
                        {
                            // 插件类实例方法：使用插件实例
                            targetInstance = plugin;
                            Log.Debug("发现插件实例工具方法: {Type}.{Method}", type.Name, method.Name);
                        }
                        else
                        {
                            // 其他类实例方法：延迟创建
                            needsLazyInstance = true;
                            Log.Debug("发现其他类实例工具方法: {Type}.{Method} (将延迟创建实例)", type.Name, method.Name);
                        }

                        // 提取参数信息
                        var parameters = ExtractParameterInfo(method);

                        // 创建工具定义
                        var tool = new Plugin.ToolDefinition
                        {
                            Name = toolAttr.Name,
                            Description = toolAttr.Description,
                            ReturnDescription = toolAttr.ReturnDescription,
                            ProviderPlugin = pluginName,
                            Method = method,
                            TargetType = type,
                            TargetInstance = targetInstance,
                            IsStatic = method.IsStatic,
                            NeedsLazyInstance = needsLazyInstance,
                            Parameters = parameters
                        };

                        // 注册到工具注册表
                        ServiceLocator.Get<ToolRegistry>().RegisterTool(pluginName, tool);
                        toolCount++;

                        // 记录详细日志
                        string methodTypeDesc = method.IsStatic ? "静态" :
                                               needsLazyInstance ? "实例(延迟创建)" : "实例";
                        Log.Debug("注册工具: {Tool} ({Type}.{Method}, {MethodType})",
                            toolAttr.Name, type.Name, method.Name, methodTypeDesc);
                    }
                }

                if (toolCount > 0)
                {
                    Log.Information("插件 {Plugin} 注册了 {Count} 个工具", pluginName, toolCount);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "插件 {Plugin} 的工具发现失败", pluginName);
            }
        }

        /// <summary>
        /// 提取方法参数信息
        /// </summary>
        private List<Plugin.ToolParameterInfo> ExtractParameterInfo(MethodInfo method)
        {
            var parameters = new List<Plugin.ToolParameterInfo>();

            foreach (var param in method.GetParameters())
            {
                var paramAttr = param.GetCustomAttribute<Plugin.ToolParameterAttribute>();

                parameters.Add(new Plugin.ToolParameterInfo
                {
                    Name = paramAttr?.Name ?? param.Name!,
                    Description = paramAttr?.Description ?? string.Empty,
                    Type = param.ParameterType,
                    TypeName = GetFriendlyTypeName(param.ParameterType),
                    Required = paramAttr?.Required ?? !param.HasDefaultValue,
                    DefaultValue = param.HasDefaultValue ? param.DefaultValue : null
                });
            }

            return parameters;
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
        /// 启用插件(异步)
        /// </summary>
        /// <param name="pluginName">插件唯一名称</param>
        /// <returns>是否成功</returns>
        public async Task<bool> EnablePluginAsync(string pluginName)
        {
            try
            {
                // 1. 查找已加载的插件
                PluginInfo? plugin = _loadedPlugins.FirstOrDefault(p => p.UniqueName == pluginName);

                if (plugin != null)
                {
                    // 插件已加载
                    if (plugin.IsEnabled)
                    {
                        Log.Information("插件 {PluginName} 已经是启用状态", pluginName);
                        return true;
                    }

                    // 调用 OnEnable
                    plugin.PluginInstance?.OnEnable();
                    plugin.IsEnabled = true;

                    Log.Information("插件 {PluginName} 已启用", pluginName);
                }
                else
                {
                    // 插件未加载，需要重新加载
                    Log.Information("插件 {PluginName} 未加载，尝试重新加载", pluginName);

                    // 从数据库获取插件信息
                    PluginInfoDB? dbInfo = DBContext.GetPluginInfo(pluginName);
                    if (dbInfo == null)
                    {
                        Log.Warning("插件 {PluginName} 在数据库中不存在", pluginName);
                        return false;
                    }

                    // 尝试从插件目录加载
                    string pluginDirectory = Path.Combine(AppConfig.ROOT_PATH, "plugins");
                    string pluginFolder = Path.Combine(pluginDirectory, pluginName);

                    if (!Directory.Exists(pluginFolder))
                    {
                        Log.Error("插件目录不存在: {PluginFolder}", pluginFolder);
                        return false;
                    }

                    string manifestPath = Path.Combine(pluginFolder, "plugin.json");
                    if (!File.Exists(manifestPath))
                    {
                        Log.Error("插件 manifest 文件不存在: {ManifestPath}", manifestPath);
                        return false;
                    }

                    PluginManifest? manifest = LoadManifest(manifestPath);
                    if (manifest == null)
                    {
                        Log.Error("加载插件 manifest 失败: {PluginName}", pluginName);
                        return false;
                    }

                    PluginInfo? loadedPlugin = await LoadPluginAsync(pluginFolder, manifest);
                    if (loadedPlugin == null)
                    {
                        Log.Error("加载插件失败: {PluginName}", pluginName);
                        return false;
                    }

                    loadedPlugin.IsEnabled = true;
                    _loadedPlugins.Add(loadedPlugin);

                    Log.Information("插件 {PluginName} 重新加载并启用成功", pluginName);
                }

                // 4. 更新数据库状态
                DBContext.SetPluginEnabled(pluginName, true);

                // 5. 发布插件状态变更事件
                ServiceLocator.Get<EventService>().Publish(new PluginStateChangedMessage
                {
                    PluginName = pluginName,
                    IsEnabled = true
                });

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "启用插件失败: {PluginName}", pluginName);
                return false;
            }
        }

        /// <summary>
        /// 禁用插件
        /// </summary>
        /// <param name="pluginName">插件唯一名称</param>
        /// <returns>是否成功</returns>
        public bool DisablePlugin(string pluginName)
        {
            try
            {
                // 1. 查找已加载的插件
                PluginInfo? plugin = _loadedPlugins.FirstOrDefault(p => p.UniqueName == pluginName);

                if (plugin == null)
                {
                    Log.Warning("尝试禁用不存在的插件: {PluginName}", pluginName);
                    // 即使插件不在内存中，也更新数据库状态
                    DBContext.SetPluginEnabled(pluginName, false);
                    return true;
                }

                if (!plugin.IsEnabled)
                {
                    Log.Information("插件 {PluginName} 已经是禁用状态", pluginName);
                    return true;
                }

                // 2. 调用 OnDisable
                plugin.PluginInstance?.OnDisable();
                plugin.IsEnabled = false;

                // 3. 更新数据库状态
                DBContext.SetPluginEnabled(pluginName, false);

                // 4. 发布插件状态变更事件（从导航菜单移除标签页）
                ServiceLocator.Get<EventService>().Publish(new PluginStateChangedMessage
                {
                    PluginName = pluginName,
                    IsEnabled = false
                });

                Log.Information("插件 {PluginName} 已禁用", pluginName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "禁用插件失败: {PluginName}", pluginName);
                return false;
            }
        }

        /// <summary>
        /// 标记插件为待删除
        /// </summary>
        /// <param name="pluginName">插件唯一名称</param>
        /// <returns>删除结果（成功/失败，消息）</returns>
        public (bool success, string message) MarkPluginForDeletion(string pluginName)
        {
            try
            {
                // 1. 禁用插件
                DisablePlugin(pluginName);

                // 2. 标记为待删除
                DBContext.MarkPluginForDeletion(pluginName, true);

                // 3. 发布插件删除事件（移除导航项）
                ServiceLocator.Get<EventService>().Publish(
                    new PluginDeletedMessage { PluginName = pluginName });

                Log.Information("插件 {PluginName} 已标记为待删除", pluginName);
                return (true, "插件将在下次启动时删除");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "标记插件删除失败: {PluginName}", pluginName);
                return (false, $"标记失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消插件删除标记
        /// </summary>
        /// <param name="pluginName">插件唯一名称</param>
        /// <returns>是否成功</returns>
        public bool CancelPluginDeletion(string pluginName)
        {
            try
            {
                DBContext.MarkPluginForDeletion(pluginName, false);

                Log.Information("已取消删除插件: {PluginName}", pluginName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "取消删除失败: {PluginName}", pluginName);
                return false;
            }
        }

        /// <summary>
        /// 取消插件更新
        /// </summary>
        /// <param name="pluginName">插件唯一名称</param>
        /// <returns>操作结果（成功/失败，消息）</returns>
        public async Task<(bool success, string message)> CancelPluginUpdateAsync(string pluginName)
        {
            try
            {
                // 1. 清除数据库中的待更新标记
                DBContext.CancelPluginUpdate(pluginName);

                // 2. 删除临时插件目录中的临时文件
                string pluginTempDirectory = AppConfig.TEMP_PLUGIN_FOLDER;
                string tempPluginDir = Path.Combine(pluginTempDirectory, pluginName);

                if (Directory.Exists(tempPluginDir))
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            Directory.Delete(tempPluginDir, true);
                            Log.Information("已删除临时插件目录: {TempDir}", tempPluginDir);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "删除临时插件目录失败: {TempDir}", tempPluginDir);
                        }
                    });
                }

                Log.Information("已取消插件更新: {PluginName}", pluginName);
                return (true, "已取消更新");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "取消插件更新失败: {PluginName}", pluginName);
                return (false, $"取消更新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除所有标记为待删除的插件（启动时调用）
        /// </summary>
        /// <param name="pluginDirectory">插件目录</param>
        private void DeletePendingPlugins(string pluginDirectory)
        {
            var pendingPlugins = DBContext.GetPendingDeletePlugins();

            if (pendingPlugins.Count == 0)
            {
                return;
            }

            Log.Information("发现 {Count} 个待删除的插件", pendingPlugins.Count);

            foreach (var plugin in pendingPlugins)
            {
                string pluginFolder = Path.Combine(pluginDirectory, plugin.PluginName);

                if (Directory.Exists(pluginFolder))
                {
                    try
                    {
                        Directory.Delete(pluginFolder, true);
                        Log.Information("已删除插件目录: {PluginFolder}", pluginFolder);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "删除插件目录失败: {PluginFolder}", pluginFolder);
                        // 继续处理其他插件
                    }
                }
                else
                {
                    Log.Warning("插件目录不存在，跳过文件删除: {PluginFolder}", pluginFolder);
                }

                // 清理数据库记录
                try
                {
                    DBContext.DeletePluginInfo(plugin.PluginName);
                    Log.Information("已删除待删除插件: {PluginName}", plugin.PluginName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "清理插件数据库记录失败: {PluginName}", plugin.PluginName);
                }
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
