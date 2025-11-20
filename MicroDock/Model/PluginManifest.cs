using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MicroDock.Model;

/// <summary>
/// 插件清单文件 (plugin.json) 的数据模型
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// 插件唯一标识符，使用反向域名格式 (例如: com.company.pluginname)
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称（可选，如果未提供则使用 Name）
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 插件版本号（语义化版本）
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 插件描述
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 作者信息
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// 许可证
    /// </summary>
    [JsonPropertyName("license")]
    public string? License { get; set; }

    /// <summary>
    /// 主页 URL
    /// </summary>
    [JsonPropertyName("homepage")]
    public string? Homepage { get; set; }

    /// <summary>
    /// 主 DLL 文件名（例如: PluginName.dll）
    /// </summary>
    [JsonPropertyName("main")]
    public string Main { get; set; } = string.Empty;

    /// <summary>
    /// 入口类的完全限定名（例如: Namespace.PluginClassName）
    /// </summary>
    [JsonPropertyName("entryClass")]
    public string EntryClass { get; set; } = string.Empty;

    /// <summary>
    /// 插件依赖（键为插件名称，值为版本范围）
    /// </summary>
    [JsonPropertyName("dependencies")]
    public Dictionary<string, string>? Dependencies { get; set; }

    /// <summary>
    /// 运行环境要求（例如 MicroDock 的最低版本）
    /// </summary>
    [JsonPropertyName("engines")]
    public Dictionary<string, string>? Engines { get; set; }

    /// <summary>
    /// 获取有效的显示名称（如果 DisplayName 为空则返回 Name）
    /// </summary>
    [JsonIgnore]
    public string EffectiveDisplayName => string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;

    /// <summary>
    /// 验证清单的完整性和格式
    /// </summary>
    /// <returns>验证结果，如果成功返回 null，否则返回错误信息</returns>
    public string? Validate()
    {
        // 验证必需字段
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "name 字段不能为空";
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            return "version 字段不能为空";
        }

        if (string.IsNullOrWhiteSpace(Main))
        {
            return "main 字段不能为空（DLL 文件名）";
        }

        if (string.IsNullOrWhiteSpace(EntryClass))
        {
            return "entryClass 字段不能为空（入口类全名）";
        }

        // 验证 name 格式（反向域名：com.company.pluginname）
        if (!IsValidReverseDomainName(Name))
        {
            return $"name 字段格式无效: '{Name}'。必须使用反向域名格式，例如: com.company.pluginname";
        }

        // 验证版本号格式（基本的语义化版本检查）
        if (!IsValidSemanticVersion(Version))
        {
            return $"version 字段格式无效: '{Version}'。必须使用语义化版本格式，例如: 1.0.0";
        }

        // 验证 main 文件名
        if (!Main.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return $"main 字段必须是 .dll 文件: '{Main}'";
        }

        // 验证依赖的版本范围格式
        if (Dependencies != null)
        {
            foreach (var dep in Dependencies)
            {
                if (string.IsNullOrWhiteSpace(dep.Key))
                {
                    return "dependencies 中存在空的插件名称";
                }

                if (string.IsNullOrWhiteSpace(dep.Value))
                {
                    return $"插件 '{dep.Key}' 的版本范围不能为空";
                }

                if (!IsValidReverseDomainName(dep.Key))
                {
                    return $"依赖的插件名称格式无效: '{dep.Key}'";
                }
            }
        }

        return null; // 验证通过
    }

    /// <summary>
    /// 验证反向域名格式
    /// 格式: com.company.pluginname (至少两个部分，只能包含小写字母、数字、点和连字符，每部分必须以字母开头)
    /// </summary>
    private static bool IsValidReverseDomainName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // 正则表达式：每个部分必须以字母开头，可以包含字母、数字、连字符
        var regex = new Regex(@"^[a-z][a-z0-9-]*(\.[a-z][a-z0-9-]*)+$", RegexOptions.None, TimeSpan.FromSeconds(1));
        return regex.IsMatch(name);
    }

    /// <summary>
    /// 验证语义化版本格式 (简化版，支持 X.Y.Z 和 X.Y.Z-prerelease)
    /// </summary>
    private static bool IsValidSemanticVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        // 简化的语义化版本正则: major.minor.patch[-prerelease][+build]
        var regex = new Regex(@"^\d+\.\d+\.\d+(-[a-zA-Z0-9.-]+)?(\+[a-zA-Z0-9.-]+)?$", RegexOptions.None, TimeSpan.FromSeconds(1));
        return regex.IsMatch(version);
    }
}

