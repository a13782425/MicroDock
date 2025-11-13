using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MicroDock.Plugin;

/// <summary>
/// 工具定义（内部使用）
/// </summary>
public class ToolDefinition
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 返回值描述
    /// </summary>
    public string ReturnDescription { get; set; } = string.Empty;

    /// <summary>
    /// 提供该工具的插件名称
    /// </summary>
    public string ProviderPlugin { get; set; } = string.Empty;

    /// <summary>
    /// 工具方法信息
    /// </summary>
    public MethodInfo Method { get; set; } = null!;

    /// <summary>
    /// 工具方法的目标类型（用于创建实例）
    /// </summary>
    public Type? TargetType { get; set; }

    /// <summary>
    /// 工具方法的目标对象实例
    /// - 静态方法时为 null
    /// - 插件实例方法时为插件实例
    /// - 其他类实例方法时为延迟创建的实例（首次调用时创建）
    /// </summary>
    public object? TargetInstance { get; set; }

    /// <summary>
    /// 方法是否为静态方法
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// 是否需要延迟创建实例（非插件类的实例方法）
    /// </summary>
    public bool NeedsLazyInstance { get; set; }

    /// <summary>
    /// 实例创建锁（用于线程安全的延迟创建）
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public object InstanceLock { get; set; } = new object();

    /// <summary>
    /// 参数列表
    /// </summary>
    public List<ToolParameterInfo> Parameters { get; set; } = new();
}

/// <summary>
/// 工具参数信息
/// </summary>
public class ToolParameterInfo
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参数描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    public Type Type { get; set; } = null!;

    /// <summary>
    /// 友好的类型名称（用于显示）
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// 是否为必需参数
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 默认值（如果有）
    /// </summary>
    public object? DefaultValue { get; set; }
}

/// <summary>
/// 工具信息（对外提供的工具元数据）
/// </summary>
public class ToolInfo
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 返回值描述
    /// </summary>
    public string ReturnDescription { get; set; } = string.Empty;

    /// <summary>
    /// 提供该工具的插件名称
    /// </summary>
    public string ProviderPlugin { get; set; } = string.Empty;

    /// <summary>
    /// 参数列表
    /// </summary>
    public List<ToolParameterInfo> Parameters { get; set; } = new();

    /// <summary>
    /// 是否为静态方法
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// 是否为非插件类的实例方法（首次调用创建实例）
    /// </summary>
    public bool NeedsLazyInstance { get; set; }

    /// <summary>
    /// 工具所属的类名（用于显示）
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// 参数摘要（用于快速显示）
    /// </summary>
    public string ParametersSummary
    {
        get
        {
            if (Parameters == null || Parameters.Count == 0)
            {
                return "(无参数)";
            }

            return string.Join(", ", Parameters.Select(p => $"{p.Name}: {p.TypeName}"));
        }
    }

    /// <summary>
    /// 完整的工具签名（用于显示）
    /// </summary>
    public string Signature => $"{Name}({ParametersSummary})";
}

/// <summary>
/// 工具使用统计
/// </summary>
public class ToolStatistics
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// 提供该工具的插件名称
    /// </summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// 总调用次数
    /// </summary>
    public int CallCount { get; set; }

    /// <summary>
    /// 成功次数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败次数
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// 平均执行时间
    /// </summary>
    public TimeSpan AverageDuration { get; set; }

    /// <summary>
    /// 最后调用时间
    /// </summary>
    public DateTime LastCallTime { get; set; }

    /// <summary>
    /// 成功率（百分比）
    /// </summary>
    public double SuccessRate
    {
        get
        {
            if (CallCount == 0) return 0;
            return (double)SuccessCount / CallCount * 100;
        }
    }
}

