using System;

namespace MicroDock.Plugin;

/// <summary>
/// 标记方法为可调用的插件工具
/// 工具方法必须返回 Task&lt;string&gt; 类型
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class MicroToolAttribute : Attribute
{
    /// <summary>
    /// 工具名称（唯一标识符）
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 返回值描述
    /// </summary>
    public string ReturnDescription { get; set; } = string.Empty;

    /// <summary>
    /// 创建工具特性
    /// </summary>
    /// <param name="name">工具名称，建议使用 "category.action" 格式，如 "http.get"</param>
    public MicroToolAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("工具名称不能为空", nameof(name));
        }

        Name = name;
    }
}

/// <summary>
/// 标记工具方法的参数，提供参数文档
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public class ToolParameterAttribute : Attribute
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 参数描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否为必需参数（默认为 true，如果参数有默认值则自动设置为 false）
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// 创建参数特性
    /// </summary>
    /// <param name="name">参数名称</param>
    public ToolParameterAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("参数名称不能为空", nameof(name));
        }

        Name = name;
    }
}

