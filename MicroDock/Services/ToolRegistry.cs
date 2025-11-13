using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MicroDock.Plugin;
using Serilog;

namespace MicroDock.Services;

/// <summary>
/// 工具注册表
/// 负责工具的注册、查询、调用和统计
/// </summary>
public class ToolRegistry
{
    // 工具存储：插件名 -> 工具名 -> 工具定义
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ToolDefinition>> _toolsByPlugin;

    // 全局工具索引：工具名 -> 第一个注册的工具定义
    private readonly ConcurrentDictionary<string, ToolDefinition> _globalTools;

    // 统计数据：完整键（插件名.工具名）-> 统计信息
    private readonly ConcurrentDictionary<string, ToolStatistics> _statistics;

    /// <summary>
    /// 公共构造函数，用于 ServiceLocator 注册
    /// </summary>
    public ToolRegistry()
    {
        _toolsByPlugin = new ConcurrentDictionary<string, ConcurrentDictionary<string, ToolDefinition>>();
        _globalTools = new ConcurrentDictionary<string, ToolDefinition>();
        _statistics = new ConcurrentDictionary<string, ToolStatistics>();
        
        // 启动时从数据库加载历史统计
        LoadStatisticsFromDatabase();
    }
    
    /// <summary>
    /// 从数据库加载历史统计
    /// </summary>
    private void LoadStatisticsFromDatabase()
    {
        try
        {
            var dbStats = Database.DBContext.LoadAllPluginToolStatistics();
            foreach (var stat in dbStats)
            {
                var key = $"{stat.PluginName}.{stat.ToolName}";
                _statistics.TryAdd(key, stat);
            }
            
            if (dbStats.Count > 0)
            {
                Log.Information("从数据库加载了 {Count} 条工具统计", dbStats.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载工具统计失败");
        }
    }

    /// <summary>
    /// 注册工具
    /// </summary>
    public void RegisterTool(string pluginName, ToolDefinition tool)
    {
        if (string.IsNullOrWhiteSpace(pluginName))
        {
            throw new ArgumentException("插件名称不能为空", nameof(pluginName));
        }

        if (tool == null)
        {
            throw new ArgumentNullException(nameof(tool));
        }

        // 获取或创建插件的工具字典
        var pluginTools = _toolsByPlugin.GetOrAdd(pluginName, 
            _ => new ConcurrentDictionary<string, ToolDefinition>());

        // 注册到插件的工具列表
        if (!pluginTools.TryAdd(tool.Name, tool))
        {
            Log.Warning("插件 {Plugin} 已存在工具 {Tool}，将被覆盖", pluginName, tool.Name);
            pluginTools[tool.Name] = tool;
        }

        // 注册到全局工具索引（只保留第一个注册的）
        _globalTools.TryAdd(tool.Name, tool);

        Log.Information("注册工具: {Tool} (插件: {Plugin}, 参数: {ParamCount})", 
            tool.Name, pluginName, tool.Parameters.Count);
    }

    /// <summary>
    /// 调用工具
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <param name="parameters">参数字典</param>
    /// <param name="pluginName">可选的插件名称，如果指定则只在该插件中查找</param>
    public async Task<string> CallToolAsync(
        string toolName,
        Dictionary<string, string> parameters,
        string? pluginName = null)
    {
        var stopwatch = Stopwatch.StartNew();
        ToolDefinition? tool = null;
        string actualPluginName;

        try
        {
            // 1. 查找工具
            if (!string.IsNullOrEmpty(pluginName))
            {
                // 在指定插件中查找
                if (!_toolsByPlugin.TryGetValue(pluginName, out var pluginTools) ||
                    !pluginTools.TryGetValue(toolName, out tool))
                {
                    throw new ToolNotFoundException(toolName, pluginName);
                }
                actualPluginName = pluginName;
            }
            else
            {
                // 在全局工具中查找第一个匹配的
                if (!_globalTools.TryGetValue(toolName, out tool))
                {
                    throw new ToolNotFoundException(toolName);
                }
                actualPluginName = tool.ProviderPlugin;
            }

            Log.Information("调用工具: {Tool} (插件: {Plugin}), 参数: {@Parameters}", 
                toolName, actualPluginName, parameters);

            // 2. 执行工具
            var result = await InvokeToolAsync(tool, parameters);

            stopwatch.Stop();

            // 3. 更新统计（成功）
            UpdateStatistics(toolName, actualPluginName, true, stopwatch.Elapsed);

            Log.Information("工具调用成功: {Tool}, 耗时: {Duration}ms", 
                toolName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex) when (ex is not ToolNotFoundException)
        {
            stopwatch.Stop();

            actualPluginName = tool?.ProviderPlugin ?? pluginName ?? "unknown";

            // 更新统计（失败）
            UpdateStatistics(toolName, actualPluginName, false, stopwatch.Elapsed);

            Log.Error(ex, "工具调用失败: {Tool} (插件: {Plugin}), 耗时: {Duration}ms", 
                toolName, actualPluginName, stopwatch.ElapsedMilliseconds);

            if (ex is ToolExecutionException || ex is ToolParameterException)
            {
                throw;
            }

            throw new ToolExecutionException(toolName, actualPluginName, ex);
        }
    }

    /// <summary>
    /// 执行工具方法
    /// </summary>
    private async Task<string> InvokeToolAsync(ToolDefinition tool, Dictionary<string, string> parameters)
    {
        var methodParams = tool.Method.GetParameters();
        var args = new object?[methodParams.Length];

        // 转换参数
        for (int i = 0; i < methodParams.Length; i++)
        {
            var param = methodParams[i];
            var paramInfo = tool.Parameters[i];

            if (parameters.TryGetValue(paramInfo.Name, out var value))
            {
                try
                {
                    args[i] = ConvertParameter(value, param.ParameterType);
                }
                catch (Exception ex)
                {
                    throw new ToolParameterException(tool.Name, paramInfo.Name, 
                        $"类型转换失败: {ex.Message}");
                }
            }
            else if (param.HasDefaultValue)
            {
                args[i] = param.DefaultValue;
            }
            else if (paramInfo.Required)
            {
                throw new ToolParameterException(tool.Name, paramInfo.Name, "缺少必需参数");
            }
            else
            {
                args[i] = param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
            }
        }

        // 确定调用实例
        object? invokeInstance;
        
        if (tool.IsStatic)
        {
            // 静态方法：传 null
            invokeInstance = null;
        }
        else if (tool.TargetInstance != null)
        {
            // 已有实例（插件实例或已创建的缓存实例）
            invokeInstance = tool.TargetInstance;
        }
        else if (tool.NeedsLazyInstance && tool.TargetType != null)
        {
            // 需要延迟创建实例：线程安全地创建并缓存
            lock (tool.InstanceLock)
            {
                if (tool.TargetInstance == null)
                {
                    try
                    {
                        tool.TargetInstance = Activator.CreateInstance(
                            tool.TargetType, 
                            nonPublic: true);
                        Log.Information("为工具 {Tool} 创建实例: {Type}", 
                            tool.Name, tool.TargetType.Name);
                    }
                    catch (Exception ex)
                    {
                        throw new ToolExecutionException(tool.Name, tool.ProviderPlugin, ex);
                    }
                }
            }
            invokeInstance = tool.TargetInstance;
        }
        else
        {
            throw new InvalidOperationException($"工具 {tool.Name} 配置错误：无法确定调用目标");
        }

        // 调用方法
        try
        {
            var result = tool.Method.Invoke(invokeInstance, args);

            if (result is Task<string> task)
            {
                return await task;
            }

            throw new InvalidOperationException($"工具方法必须返回 Task<string>，实际返回: {result?.GetType().Name ?? "null"}");
        }
        catch (TargetInvocationException ex)
        {
            // 展开内部异常
            throw ex.InnerException ?? ex;
        }
    }

    /// <summary>
    /// 转换参数类型（从字符串转换到目标类型）
    /// </summary>
    private object ConvertParameter(string stringValue, Type targetType)
    {
        // 1. 处理可空类型
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            if (string.IsNullOrEmpty(stringValue))
                return null!;
            targetType = underlyingType;
        }

        // 2. 字符串类型
        if (targetType == typeof(string))
        {
            return stringValue;
        }

        // 3. 基本类型
        if (targetType.IsPrimitive || targetType == typeof(decimal))
        {
            try
            {
                return Convert.ChangeType(stringValue, targetType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"无法将字符串 '{stringValue}' 转换为类型 {targetType.Name}: {ex.Message}");
            }
        }

        // 4. 枚举类型
        if (targetType.IsEnum)
        {
            try
            {
                return Enum.Parse(targetType, stringValue, true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"无法将字符串 '{stringValue}' 转换为枚举类型 {targetType.Name}: {ex.Message}");
            }
        }

        // 5. 复杂类型通过 JSON 反序列化
        try
        {
            return JsonSerializer.Deserialize(stringValue, targetType) 
                ?? throw new InvalidOperationException($"JSON 反序列化结果为 null");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"无法将 JSON 字符串转换为类型 {targetType.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics(string toolName, string pluginName, bool success, TimeSpan duration)
    {
        var key = $"{pluginName}.{toolName}";

        _statistics.AddOrUpdate(key,
            _ => new ToolStatistics
            {
                ToolName = toolName,
                PluginName = pluginName,
                CallCount = 1,
                SuccessCount = success ? 1 : 0,
                FailureCount = success ? 0 : 1,
                AverageDuration = duration,
                LastCallTime = DateTime.Now
            },
            (_, stats) =>
            {
                stats.CallCount++;
                if (success)
                    stats.SuccessCount++;
                else
                    stats.FailureCount++;

                // 更新平均耗时
                var totalMs = stats.AverageDuration.TotalMilliseconds * (stats.CallCount - 1);
                stats.AverageDuration = TimeSpan.FromMilliseconds((totalMs + duration.TotalMilliseconds) / stats.CallCount);

                stats.LastCallTime = DateTime.Now;

                return stats;
            });
        
        // 异步保存到数据库（避免阻塞调用）
        var currentStats = _statistics[key];
        Task.Run(() =>
        {
            try
            {
                Database.DBContext.SavePluginToolStatistics(pluginName, toolName, currentStats);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "保存工具统计失败: {PluginName}.{ToolName}", pluginName, toolName);
            }
        });
    }

    /// <summary>
    /// 获取所有工具
    /// </summary>
    public List<ToolInfo> GetAllTools()
    {
        var tools = new List<ToolInfo>();

        foreach (var pluginEntry in _toolsByPlugin)
        {
            foreach (var toolEntry in pluginEntry.Value)
            {
                tools.Add(ConvertToToolInfo(toolEntry.Value));
            }
        }

        return tools.OrderBy(t => t.ProviderPlugin).ThenBy(t => t.Name).ToList();
    }

    /// <summary>
    /// 获取指定插件的工具
    /// </summary>
    public List<ToolInfo> GetPluginTools(string pluginName)
    {
        if (!_toolsByPlugin.TryGetValue(pluginName, out var pluginTools))
        {
            return new List<ToolInfo>();
        }

        return pluginTools.Values
            .Select(ConvertToToolInfo)
            .OrderBy(t => t.Name)
            .ToList();
    }

    /// <summary>
    /// 获取工具详细信息
    /// </summary>
    public ToolInfo? GetToolInfo(string toolName, string? pluginName = null)
    {
        ToolDefinition? tool = null;

        if (!string.IsNullOrEmpty(pluginName))
        {
            if (_toolsByPlugin.TryGetValue(pluginName, out var pluginTools))
            {
                pluginTools.TryGetValue(toolName, out tool);
            }
        }
        else
        {
            _globalTools.TryGetValue(toolName, out tool);
        }

        return tool != null ? ConvertToToolInfo(tool) : null;
    }

    /// <summary>
    /// 获取工具统计
    /// </summary>
    public ToolStatistics? GetStatistics(string toolName, string? pluginName = null)
    {
        if (!string.IsNullOrEmpty(pluginName))
        {
            var key = $"{pluginName}.{toolName}";
            return _statistics.TryGetValue(key, out var stats) ? stats : null;
        }

        // 如果没有指定插件名，查找第一个匹配的统计
        return _statistics.Values.FirstOrDefault(s => s.ToolName == toolName);
    }

    /// <summary>
    /// 获取所有统计信息
    /// </summary>
    public List<ToolStatistics> GetAllStatistics()
    {
        return _statistics.Values.OrderByDescending(s => s.CallCount).ToList();
    }

    /// <summary>
    /// 转换为 ToolInfo
    /// </summary>
    private ToolInfo ConvertToToolInfo(ToolDefinition tool)
    {
        return new ToolInfo
        {
            Name = tool.Name,
            Description = tool.Description,
            ReturnDescription = tool.ReturnDescription,
            ProviderPlugin = tool.ProviderPlugin,
            Parameters = new List<ToolParameterInfo>(tool.Parameters),
            IsStatic = tool.IsStatic,
            NeedsLazyInstance = tool.NeedsLazyInstance,
            ClassName = tool.TargetType?.Name ?? "Unknown"
        };
    }

    /// <summary>
    /// 清除所有工具（用于测试）
    /// </summary>
    public void Clear()
    {
        _toolsByPlugin.Clear();
        _globalTools.Clear();
        _statistics.Clear();
    }
}

