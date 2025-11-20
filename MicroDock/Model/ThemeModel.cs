using Avalonia.Media;
using System.Collections.Generic;

namespace MicroDock.Model;

/// <summary>
/// 主题数据模型
/// </summary>
public class ThemeModel
{
    /// <summary>
    /// 主题唯一名称（XML文件名，不含扩展名）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 主题分类（默认、Fluent、Tailwind等），用于分组显示
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 标记是否为分组标题（用于在扁平化列表中区分）
    /// </summary>
    public bool IsGroupHeader => false;

    /// <summary>
    /// 主题描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 主题变体：Dark, Light, System
    /// </summary>
    public Avalonia.Styling.ThemeVariant Variant { get; set; } = Avalonia.Styling.ThemeVariant.Default;

    /// <summary>
    /// 基础强调色（主色调）
    /// </summary>
    public Color AccentColor { get; set; } = Colors.SlateBlue;

    /// <summary>
    /// 自定义颜色资源字典（Key为颜色资源名称，Value为颜色值）
    /// </summary>
    public Dictionary<string, Color> ColorResources { get; set; } = new();

    /// <summary>
    /// XML文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}

