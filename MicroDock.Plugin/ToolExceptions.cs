using System;

namespace MicroDock.Plugin;

/// <summary>
/// 工具未找到异常
/// </summary>
public class ToolNotFoundException : Exception
{
    public string ToolName { get; }
    public string? PluginName { get; }

    public ToolNotFoundException(string toolName, string? pluginName = null)
        : base(BuildMessage(toolName, pluginName))
    {
        ToolName = toolName;
        PluginName = pluginName;
    }

    private static string BuildMessage(string toolName, string? pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return $"工具 '{toolName}' 未找到";
        }
        return $"插件 '{pluginName}' 中的工具 '{toolName}' 未找到";
    }
}

/// <summary>
/// 工具执行异常
/// </summary>
public class ToolExecutionException : Exception
{
    public string ToolName { get; }
    public string? PluginName { get; }

    public ToolExecutionException(string toolName, string? pluginName, Exception innerException)
        : base(BuildMessage(toolName, pluginName), innerException)
    {
        ToolName = toolName;
        PluginName = pluginName;
    }

    private static string BuildMessage(string toolName, string? pluginName)
    {
        if (string.IsNullOrEmpty(pluginName))
        {
            return $"工具 '{toolName}' 执行失败";
        }
        return $"插件 '{pluginName}' 中的工具 '{toolName}' 执行失败";
    }
}

/// <summary>
/// 工具参数异常
/// </summary>
public class ToolParameterException : Exception
{
    public string ToolName { get; }
    public string ParameterName { get; }

    public ToolParameterException(string toolName, string parameterName, string message)
        : base($"工具 '{toolName}' 的参数 '{parameterName}' 错误: {message}")
    {
        ToolName = toolName;
        ParameterName = parameterName;
    }
}

