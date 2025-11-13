using System;
using System.Text.RegularExpressions;

namespace MicroDock.Utils;

/// <summary>
/// 版本号解析和匹配工具类
/// 支持语义化版本 (Semver) 和 npm 风格的版本范围
/// </summary>
public static class VersionHelper
{
    /// <summary>
    /// 解析版本字符串为 Version 对象
    /// </summary>
    /// <param name="versionString">版本字符串，例如 "1.2.3"</param>
    /// <returns>Version 对象，如果解析失败返回 null</returns>
    public static Version? ParseVersion(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            return null;

        // 移除可能的前缀（v）和后缀（-prerelease, +build）
        var cleaned = versionString.Trim().TrimStart('v', 'V');
        
        // 移除 prerelease 和 build 标识
        var match = Regex.Match(cleaned, @"^(\d+\.\d+\.\d+)", RegexOptions.None, TimeSpan.FromSeconds(1));
        if (!match.Success)
            return null;

        cleaned = match.Groups[1].Value;

        if (Version.TryParse(cleaned, out var version))
            return version;

        return null;
    }

    /// <summary>
    /// 检查版本是否匹配指定的版本范围
    /// </summary>
    /// <param name="version">要检查的版本</param>
    /// <param name="range">版本范围（支持 ^, ~, >=, *, 精确版本等）</param>
    /// <returns>如果版本匹配范围返回 true</returns>
    public static bool MatchesRange(Version version, string range)
    {
        if (version == null || string.IsNullOrWhiteSpace(range))
            return false;

        range = range.Trim();

        // * 匹配任意版本
        if (range == "*")
            return true;

        // 精确版本匹配
        if (!range.StartsWith("^") && !range.StartsWith("~") && !range.StartsWith(">=") && 
            !range.StartsWith(">") && !range.StartsWith("<=") && !range.StartsWith("<"))
        {
            var exactVersion = ParseVersion(range);
            return exactVersion != null && CompareVersions(version, exactVersion) == 0;
        }

        // ^ 兼容版本范围（允许次版本和补丁版本升级，主版本不变）
        // 例如：^1.2.3 匹配 >=1.2.3 <2.0.0
        if (range.StartsWith("^"))
        {
            var minVersion = ParseVersion(range.Substring(1));
            if (minVersion == null)
                return false;

            // 如果主版本号不同，不匹配
            if (version.Major != minVersion.Major)
                return false;

            // 主版本相同，检查是否 >= 最小版本
            return CompareVersions(version, minVersion) >= 0;
        }

        // ~ 补丁版本范围（允许补丁版本升级，主版本和次版本不变）
        // 例如：~1.2.3 匹配 >=1.2.3 <1.3.0
        if (range.StartsWith("~"))
        {
            var minVersion = ParseVersion(range.Substring(1));
            if (minVersion == null)
                return false;

            // 主版本和次版本必须相同
            if (version.Major != minVersion.Major || version.Minor != minVersion.Minor)
                return false;

            // 补丁版本必须 >= 最小版本
            return version.Build >= minVersion.Build;
        }

        // >= 最低版本
        if (range.StartsWith(">="))
        {
            var minVersion = ParseVersion(range.Substring(2));
            if (minVersion == null)
                return false;

            return CompareVersions(version, minVersion) >= 0;
        }

        // > 大于指定版本
        if (range.StartsWith(">"))
        {
            var minVersion = ParseVersion(range.Substring(1));
            if (minVersion == null)
                return false;

            return CompareVersions(version, minVersion) > 0;
        }

        // <= 最高版本
        if (range.StartsWith("<="))
        {
            var maxVersion = ParseVersion(range.Substring(2));
            if (maxVersion == null)
                return false;

            return CompareVersions(version, maxVersion) <= 0;
        }

        // < 小于指定版本
        if (range.StartsWith("<"))
        {
            var maxVersion = ParseVersion(range.Substring(1));
            if (maxVersion == null)
                return false;

            return CompareVersions(version, maxVersion) < 0;
        }

        return false;
    }

    /// <summary>
    /// 比较两个版本的大小
    /// </summary>
    /// <param name="v1">版本1</param>
    /// <param name="v2">版本2</param>
    /// <returns>
    /// 如果 v1 > v2 返回正数
    /// 如果 v1 == v2 返回 0
    /// 如果 v1 &lt; v2 返回负数
    /// </returns>
    public static int CompareVersions(Version v1, Version v2)
    {
        if (v1 == null && v2 == null)
            return 0;
        if (v1 == null)
            return -1;
        if (v2 == null)
            return 1;

        // 比较主版本号
        if (v1.Major != v2.Major)
            return v1.Major.CompareTo(v2.Major);

        // 比较次版本号
        if (v1.Minor != v2.Minor)
            return v1.Minor.CompareTo(v2.Minor);

        // 比较补丁版本号
        return v1.Build.CompareTo(v2.Build);
    }

    /// <summary>
    /// 验证版本范围字符串的格式
    /// </summary>
    /// <param name="range">版本范围字符串</param>
    /// <returns>如果格式有效返回 true</returns>
    public static bool IsValidRange(string range)
    {
        if (string.IsNullOrWhiteSpace(range))
            return false;

        range = range.Trim();

        // * 总是有效
        if (range == "*")
            return true;

        // 检查是否有有效的前缀
        if (range.StartsWith("^") || range.StartsWith("~") || 
            range.StartsWith(">=") || range.StartsWith(">") ||
            range.StartsWith("<=") || range.StartsWith("<"))
        {
            var versionPart = range.TrimStart('^', '~', '>', '<', '=');
            return ParseVersion(versionPart) != null;
        }

        // 没有前缀，尝试作为精确版本解析
        return ParseVersion(range) != null;
    }
}

