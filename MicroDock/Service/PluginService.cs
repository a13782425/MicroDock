using Avalonia.Controls;
using MicroDock.Database;
using MicroDock.Model;
using MicroDock.Plugin;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace MicroDock.Service
{
    /// <summary>
    /// 插件加载器，支持隔离加载和生命周期管理
    /// 注意：所有插件必须提供 plugin.json 配置文件
    /// </summary>
    [AutoRegister]
    public class PluginService : IMicroService
    {
        private readonly List<PluginInfo> _loadedPlugins = new List<PluginInfo>();
        /// <summary>
        /// 获取所有已加载的插件信息
        /// </summary>
        public IReadOnlyList<PluginInfo> LoadedPlugins => _loadedPlugins.AsReadOnly();

        async Task IMicroService.OnRegistered()
        {
            _loadedPlugins.Clear();
            await PreLoadPlugins();
        }

        Task IMicroService.OnAfterAppBuilder()
        {
            foreach (var pluginInfo in _loadedPlugins)
            {
                var manifest = pluginInfo.Manifest;
                if (string.IsNullOrEmpty(manifest.AppBuilderMethod))
                {
                    continue;
                }
                try
                {
                    // 解析完整方法名：命名空间.类型.方法名
                    string fullMethodName = manifest.AppBuilderMethod;
                    int lastDotIndex = fullMethodName.LastIndexOf('.');
                    if (lastDotIndex <= 0)
                    {
                        LogWarning($"插件 {manifest.Name} 的 AppBuilderMethod '{fullMethodName}' 格式无效（应为 命名空间.类型.方法名），跳过", DEFAULT_LOG_TAG);
                        continue;
                    }
                    string fullTypeName = fullMethodName.Substring(0, lastDotIndex);
                    string methodName = fullMethodName.Substring(lastDotIndex + 1);
                    // 从插件程序集中获取类型
                    Type? targetType = pluginInfo.Assembly.GetType(fullTypeName);
                    if (targetType == null)
                    {
                        LogWarning($"插件 {manifest.Name} 的类型 '{fullTypeName}' 未找到，跳过", DEFAULT_LOG_TAG);

                        continue;
                    }
                    // 获取方法（支持静态）
                    MethodInfo? method = targetType.GetMethod(methodName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (method == null)
                    {
                        LogWarning($"插件 {manifest.Name} 的方法 '{methodName}' 在类型 '{fullTypeName}' 中未找到，跳过", DEFAULT_LOG_TAG);
                        continue;
                    }
                    // 验证参数：需要一个 AppBuilder 参数
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Avalonia.AppBuilder))
                    {
                        LogWarning($"插件 {manifest.Name} 的 AppBuilderMethod '{fullMethodName}' 参数数量不匹配（需要1个参数Avalonia.AppBuilder），跳过", DEFAULT_LOG_TAG);
                        continue;
                    }
                    method.Invoke(null, [MicroAppBuilder]);
                    LogInformation($"插件 {manifest.Name} 的 AppBuilderMethod '{fullMethodName}' 已调用", DEFAULT_LOG_TAG);
                }
                catch (Exception ex)
                {
                    LogError($"调用插件 {manifest.Name} 的 AppBuilderMethod '{manifest.AppBuilderMethod}' 失败", DEFAULT_LOG_TAG, ex);
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 导入插件（从 ZIP 文件）
        /// </summary>
        /// <param name="zipFilePath">ZIP 文件路径</param>
        /// <returns>导入结果（成功/失败，消息）</returns>
        public async Task<(bool success, string message, string? pluginName)> ImportPluginAsync(string zipFilePath)
        {
            string? tempDirectory = null;
            string? pluginName = null;
            try
            {
                // 1. 验证 ZIP 文件存在
                if (!File.Exists(zipFilePath))
                {
                    return (false, "ZIP 文件不存在", pluginName);
                }
                // 2. 创建临时目录并解压
                tempDirectory = Path.Combine(AppConfig.TEMP_IMPORT_FOLDER, $"MicroDockPlugin_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDirectory);
                LogInformation($"正在解压插件到临时目录: {tempDirectory}", DEFAULT_LOG_TAG);
                await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, tempDirectory));
                // 3. 验证根目录是否存在 plugin.json
                string manifestPath = Path.Combine(tempDirectory, "plugin.json");
                if (!File.Exists(manifestPath))
                {
                    return (false, "ZIP 根目录缺少 plugin.json 文件", null);
                }
                // 4. 解析 plugin.json 获取插件名和版本
                PluginManifest? manifest = await LoadManifest(manifestPath);
                if (manifest == null)
                {
                    return (false, "plugin.json 解析失败", null);
                }
                pluginName = manifest.Name;
                string newVersion = manifest.Version;
                LogInformation($"正在导入插件: {pluginName} v{newVersion}", DEFAULT_LOG_TAG);

                // 5. 检查数据库中是否已存在该插件
                PluginInfoDB? existingPluginInfo = DBContext.GetPluginInfo(pluginName);
                if (existingPluginInfo != null)
                {   // 插件已存在，检查版本
                    // 插件已存在，检查版本
                    string currentVersion = existingPluginInfo.Version;
                    if (currentVersion == newVersion)
                    {
                        // 版本相同，提示已安装
                        LogInformation($"插件 {pluginName} 版本 {currentVersion} 已安装", DEFAULT_LOG_TAG);
                        return (false, $"该插件已安装（版本 v{currentVersion}）", pluginName);
                    }
                    else
                    {
                        // 版本不同，标记为待更新
                        LogInformation($"插件 {pluginName} 版本不同: {currentVersion} -> {newVersion}，标记为待更新", DEFAULT_LOG_TAG);
                        // 解压到 temp/plugin/[PluginName] 目录
                        string tempPluginDir = Path.Combine(TEMP_INSTALL_FOLDER, pluginName);

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
                        ServiceLocator.Get<PluginPendingService>()?.MarkForUpdate(pluginName, newVersion);

                        return (true, $"插件将在下次重启时更新：v{currentVersion} → v{newVersion}", pluginName);
                    }
                }
                else
                {
                    //
                    // 插件不存在，直接导入
                    Log.Information("插件 {PluginName} 不存在，直接导入", pluginName);

                    // 复制文件到 Plugins/{插件名}/ 目录
                    string targetPluginDir = Path.Combine(PLUGIN_FOLDER, pluginName);
                    Directory.CreateDirectory(targetPluginDir);
                    await Task.Run(() => CopyDirectory(tempDirectory, targetPluginDir));

                    Log.Information("插件文件已复制到: {TargetDir}", targetPluginDir);


                    // 验证插件加载

                    PluginInfo pluginInfo = await PreLoadPlugin(targetPluginDir);
                    if (pluginInfo == null)
                    {
                        return (false, "插件加载失败", pluginName);
                    }
                    _loadedPlugins.Add(pluginInfo);
                    if (!await LoadPluginAsync(pluginInfo, manifest))
                    {
                        // 加载失败，清理已复制的文件
                        try
                        {
                            _loadedPlugins.Remove(pluginInfo);
                            Directory.Delete(targetPluginDir, true);
                        }
                        catch { }
                        return (false, "插件加载验证失败", pluginName);
                    }

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
        /// 从指定目录异步加载所有插件
        /// </summary>
        /// <returns>加载的插件信息列表</returns>
        public async Task LoadPluginsAsync()
        {
            if (!Directory.Exists(PLUGIN_FOLDER))
            {
                LogService.LogInformation($"插件目录不存在，创建目录: {PLUGIN_FOLDER}");
                Directory.CreateDirectory(PLUGIN_FOLDER);
                return;
            }


            // 第二阶段：解析依赖关系并确定加载顺序
            var manifests = _loadedPlugins.Select(x => x.Manifest).ToList();
            var resolveResult = DependencyResolver.Resolve(manifests);

            if (!resolveResult.Success)
            {
                Log.Error("插件依赖解析失败: {Error}", resolveResult.ErrorMessage);
                return;
            }

            Log.Information("依赖解析成功，将按顺序加载 {Count} 个插件", resolveResult.OrderedManifests!.Count);

            // 第三阶段：按依赖顺序异步加载插件
            foreach (var manifest in resolveResult.OrderedManifests!)
            {
                // 找到对应的插件文件夹
                PluginInfo pluginInfo = _loadedPlugins.First(x => x.Manifest.Name == manifest.Name);

                if (!await LoadPluginAsync(pluginInfo, manifest))
                {
                    _loadedPlugins.Remove(pluginInfo);
                }
                await Task.Delay(100); // 小延迟，避免阻塞
            }

            LogInformation($"成功加载 {_loadedPlugins.Count} 个插件", DEFAULT_LOG_TAG);

            // 第四阶段：所有插件加载完成，触发 OnAllPluginsLoaded 回调
            foreach (var plugin in _loadedPlugins)
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
        }
        /// <summary>
        /// 异步加载单个插件
        /// </summary>
        /// <param name="pluginInfo">插件信息</param>
        /// <param name="manifest">插件清单</param>
        private async Task<bool> LoadPluginAsync(PluginInfo pluginInfo, PluginManifest manifest)
        {
            PluginLoadContext? loadContext = null;

            try
            {

                Type? pluginType = pluginInfo.Assembly.GetType(manifest.EntryClass);

                if (pluginType == null)
                {
                    Log.Error("在程序集中未找到入口类: {EntryClass}", manifest.EntryClass);
                    loadContext.Unload();
                    return false;
                }

                if (!typeof(IMicroDockPlugin).IsAssignableFrom(pluginType))
                {
                    Log.Error("入口类 {EntryClass} 没有实现 IMicroDockPlugin 接口", manifest.EntryClass);
                    loadContext.Unload();
                    return false;
                }

                if (pluginType.IsAbstract || pluginType.IsInterface)
                {
                    Log.Error("入口类 {EntryClass} 是抽象类或接口", manifest.EntryClass);
                    loadContext.Unload();
                    return false;
                }

                IMicroDockPlugin? dockPlugin = Activator.CreateInstance(pluginType) as IMicroDockPlugin;
                if (dockPlugin == null)
                {
                    Log.Error("无法创建插件实例: {Type}", pluginType.Name);
                    loadContext.Unload();
                    return false;
                }

                string[] dependencies = manifest.Dependencies?.Keys.ToArray() ?? Array.Empty<string>();
                PluginContextImpl context = new PluginContextImpl(manifest.Name, pluginInfo.PluginPath);
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
                    return false;
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

                pluginInfo.Id = Guid.NewGuid().ToString();
                pluginInfo.UniqueName = manifest.Name;
                pluginInfo.PluginInstance = dockPlugin;
                pluginInfo.ControlInstance = tabControls.FirstOrDefault();
                pluginInfo.IsInitialized = true;
                pluginInfo.IsEnabled = isEnabled;
                return true;
            }
            catch (Exception ex)
            {
                LogError($"加载插件失败: {manifest.Name}", DEFAULT_LOG_TAG, ex);
                loadContext?.Unload();
                return false;
            }
        }

        /// <summary>
        /// 预加载所有插件
        /// </summary>
        /// <returns></returns>
        private async Task PreLoadPlugins()
        {
            if (!Directory.Exists(PLUGIN_FOLDER))
            {
                LogInformation($"插件目录不存在，创建目录: {PLUGIN_FOLDER}", DEFAULT_LOG_TAG);
                Directory.CreateDirectory(PLUGIN_FOLDER);
                return;
            }
            // 第一阶段：扫描并加载所有 plugin.json
            string[] pluginFolders = Directory.GetDirectories(PLUGIN_FOLDER);
            if (pluginFolders.Length == 0)
                return;
            LogInformation($"发现 {pluginFolders.Length} 个插件文件夹", DEFAULT_LOG_TAG);

            foreach (string pluginFolder in pluginFolders)
            {
                string manifestPath = Path.Combine(pluginFolder, "plugin.json");

                if (!File.Exists(manifestPath))
                {
                    LogWarning($"插件文件夹 {pluginFolder} 缺少 plugin.json，跳过加载", DEFAULT_LOG_TAG);
                    continue;
                }

                try
                {
                    PluginInfo pluginInfo = await PreLoadPlugin(pluginFolder);
                    if (pluginInfo != null)
                    {
                        _loadedPlugins.Add(pluginInfo);
                        LogDebug($"预加载插件 {pluginInfo.Name} v{pluginInfo.Manifest.Version} 成功", DEFAULT_LOG_TAG);
                    }
                    else
                    {
                        LogWarning($"预加载插件 {pluginFolder} 失败", DEFAULT_LOG_TAG);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"解析 plugin.json 失败: {manifestPath}", DEFAULT_LOG_TAG, ex);
                }
            }

            if (_loadedPlugins.Count == 0)
            {
                LogInformation("未找到有效的插件", DEFAULT_LOG_TAG);
            }
        }

        /// <summary>
        /// 预加载单个插件
        /// </summary>
        /// <returns></returns>
        private async Task<PluginInfo> PreLoadPlugin(string pluginFolder)
        {
            string manifestPath = Path.Combine(pluginFolder, "plugin.json");
            if (!File.Exists(manifestPath))
            {
                return null;
            }
            var manifest = await LoadManifest(manifestPath);
            if (manifest != null)
            {
                PluginInfo pluginInfo = new PluginInfo();
                pluginInfo.Manifest = manifest;
                pluginInfo.PluginPath = pluginFolder;
                pluginInfo.AssemblyFile = Directory.GetFiles(pluginFolder, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? "";
                pluginInfo.AssemblyDependencyPath = Path.Combine(pluginFolder, "dll");
                if (!string.IsNullOrWhiteSpace(pluginInfo.AssemblyFile))
                {
                    pluginInfo.LoadContext = new PluginLoadContext(pluginInfo);
                    pluginInfo.Assembly = pluginInfo.LoadContext.LoadFromAssemblyPath(pluginInfo.AssemblyFile);
                    LogDebug($"解析成功 plugin.json: {manifest.Name} v{manifest.Version}", DEFAULT_LOG_TAG);
                    return pluginInfo;
                }
                else
                {
                    LogDebug($"解析失败 plugin.json: {manifest.Name} v{manifest.Version}", DEFAULT_LOG_TAG);
                }
            }
            return null;
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
        private async Task<PluginManifest?> LoadManifest(string manifestPath)
        {
            try
            {
                string jsonContent = await File.ReadAllTextAsync(manifestPath);
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
                    string pluginFolder = Path.Combine(PLUGIN_FOLDER, pluginName);

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

                    PluginManifest? manifest = await LoadManifest(manifestPath);
                    if (manifest == null)
                    {
                        Log.Error("加载插件 manifest 失败: {PluginName}", pluginName);
                        return false;
                    }
                    PluginInfo pluginInfo = new PluginInfo();
                    pluginInfo.Manifest = manifest;
                    pluginInfo.PluginPath = pluginFolder;
                    _loadedPlugins.Add(pluginInfo);

                    if (!await LoadPluginAsync(pluginInfo, manifest))
                    {
                        // 加载失败，清理已复制的文件
                        try
                        {
                            _loadedPlugins.Remove(pluginInfo);
                            Log.Error("加载插件失败: {PluginName}", pluginName);
                        }
                        catch { }
                        return false;
                    }
                    pluginInfo.IsEnabled = true;
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
                ServiceLocator.Get<PluginPendingService>()?.MarkForDelete(pluginName);

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
                ServiceLocator.Get<PluginPendingService>()?.CancelDelete(pluginName);

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
                ServiceLocator.Get<PluginPendingService>()?.CancelUpdate(pluginName);

                // 2. 删除临时插件目录中的临时文件
                string pluginTempDirectory = AppConfig.TEMP_INSTALL_FOLDER;
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

        Task IMicroService.OnApplicationStopping()
        {
            UnloadAllPlugins();
            return Task.CompletedTask;
        }
    }
}
