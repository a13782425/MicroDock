using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Avalonia.Media;
using Avalonia.Styling;
using MicroDock.Model;
using Serilog;

namespace MicroDock.Service;

/// <summary>
/// 主题加载器服务，负责扫描和解析XML主题文件
/// </summary>
public class ThemeLoader
{
    private readonly string _themesDirectory;
    private List<ThemeModel>? _cachedThemes;

    public ThemeLoader()
    {
        _themesDirectory = Path.Combine(AppConfig.ROOT_PATH, "assets", "themes");

        // 确保主题目录存在
        if (!Directory.Exists(_themesDirectory))
        {
            Directory.CreateDirectory(_themesDirectory);
        }
    }
    /// <summary>
    /// 扫描并加载所有主题
    /// </summary>
    public List<ThemeModel> LoadAllThemes()
    {
        if (_cachedThemes != null)
        {
            return _cachedThemes;
        }

        _cachedThemes = new List<ThemeModel>();

        try
        {
            // 首先创建默认主题
            CreateDefaultThemes();

            if (!Directory.Exists(_themesDirectory))
            {
                Log.Warning("主题目录不存在: {ThemesDirectory}", _themesDirectory);
                return _cachedThemes;
            }

            var xmlFiles = Directory.GetFiles(_themesDirectory, "*.xml", SearchOption.TopDirectoryOnly);

            foreach (var xmlFile in xmlFiles)
            {
                try
                {
                    var theme = LoadThemeFromFile(xmlFile);
                    if (theme != null)
                    {
                        _cachedThemes.Add(theme);
                        Log.Debug("加载主题成功: {ThemeName} ({DisplayName})", theme.Name, theme.DisplayName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "加载主题文件失败: {XmlFile}", xmlFile);
                }
            }

            Log.Information("共加载 {Count} 个主题", _cachedThemes.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "扫描主题目录失败");
        }

        return _cachedThemes;
    }

    /// <summary>
    /// 根据主题名称加载主题
    /// </summary>
    public ThemeModel? LoadTheme(string themeName)
    {
        var themes = LoadAllThemes();
        return themes.FirstOrDefault(t => t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 从XML文件加载主题
    /// </summary>
    private ThemeModel? LoadThemeFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var doc = XDocument.Load(filePath);
            var root = doc.Element(XName.Get("Theme", "http://schemas.microdock.app/themes/v1"));
            if (root == null)
            {
                Log.Warning("XML文件格式错误，缺少Theme根元素: {FilePath}", filePath);
                return null;
            }

            var theme = new ThemeModel
            {
                FilePath = filePath,
                Name = Path.GetFileNameWithoutExtension(filePath)
            };

            // 加载元数据
            var metadata = root.Element(XName.Get("Metadata", "http://schemas.microdock.app/themes/v1"));
            if (metadata != null)
            {
                theme.DisplayName = metadata.Element(XName.Get("DisplayName", "http://schemas.microdock.app/themes/v1"))?.Value ?? theme.Name;
                theme.Category = metadata.Element(XName.Get("Category", "http://schemas.microdock.app/themes/v1"))?.Value ?? string.Empty;
                theme.Description = metadata.Element(XName.Get("Description", "http://schemas.microdock.app/themes/v1"))?.Value ?? string.Empty;
                theme.Author = metadata.Element(XName.Get("Author", "http://schemas.microdock.app/themes/v1"))?.Value ?? string.Empty;
                theme.Version = metadata.Element(XName.Get("Version", "http://schemas.microdock.app/themes/v1"))?.Value ?? "1.0.0";
            }

            // 加载外观设置
            var appearance = root.Element(XName.Get("Appearance", "http://schemas.microdock.app/themes/v1"));
            if (appearance != null)
            {
                // 加载主题变体
                var variantElement = appearance.Element(XName.Get("Variant", "http://schemas.microdock.app/themes/v1"));
                if (variantElement != null)
                {
                    theme.Variant = variantElement.Value switch
                    {
                        "Dark" => new ThemeVariant(theme.DisplayName, ThemeVariant.Dark),
                        "Light" => new ThemeVariant(theme.DisplayName, ThemeVariant.Light),
                        "System" => new ThemeVariant(theme.DisplayName, ThemeVariant.Default),
                        _ => ThemeVariant.Default
                    };
                }

                // 加载强调色
                var accentColorElement = appearance.Element(XName.Get("AccentColor", "http://schemas.microdock.app/themes/v1"));
                if (accentColorElement != null)
                {
                    if (TryParseColor(accentColorElement.Value, out var accentColor))
                    {
                        theme.AccentColor = accentColor;
                    }
                }

                // 加载颜色资源
                var colorResources = appearance.Element(XName.Get("ColorResources", "http://schemas.microdock.app/themes/v1"));
                if (colorResources != null)
                {
                    LoadColorResources(colorResources, theme.ColorResources);
                }
            }

            // 验证必需字段
            if (string.IsNullOrEmpty(theme.Name))
            {
                Log.Warning("主题缺少Name字段: {FilePath}", filePath);
                return null;
            }

            return theme;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "解析主题XML文件失败: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// 加载颜色资源
    /// </summary>
    private void LoadColorResources(XElement colorResources, Dictionary<string, Color> targetDictionary)
    {
        var ns = "http://schemas.microdock.app/themes/v1";

        // 加载各个颜色分类
        var colorSections = new[]
        {
            "AccentColors", "BackgroundColors", "StrokeColors",
            "TextColors", "ButtonColors", "OtherColors"
        };

        foreach (var sectionName in colorSections)
        {
            var section = colorResources.Element(XName.Get(sectionName, ns));
            if (section != null)
            {
                var colors = section.Elements(XName.Get("Color", ns));
                foreach (var colorElement in colors)
                {
                    var key = colorElement.Attribute(XName.Get("Key"))?.Value;
                    var value = colorElement.Attribute(XName.Get("Value"))?.Value;

                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        if (TryParseColor(value, out var color))
                        {
                            targetDictionary[key] = color;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 解析颜色字符串（支持#RRGGBB和#AARRGGBB格式）
    /// </summary>
    private bool TryParseColor(string colorString, out Color color)
    {
        color = Colors.Transparent;

        if (string.IsNullOrWhiteSpace(colorString))
        {
            return false;
        }

        try
        {
            // 移除#号（如果有）
            colorString = colorString.TrimStart('#');

            if (colorString.Length == 6)
            {
                // RRGGBB格式
                var r = Convert.ToByte(colorString.Substring(0, 2), 16);
                var g = Convert.ToByte(colorString.Substring(2, 2), 16);
                var b = Convert.ToByte(colorString.Substring(4, 2), 16);
                color = Color.FromRgb(r, g, b);
                return true;
            }
            else if (colorString.Length == 8)
            {
                // AARRGGBB格式
                var a = Convert.ToByte(colorString.Substring(0, 2), 16);
                var r = Convert.ToByte(colorString.Substring(2, 2), 16);
                var g = Convert.ToByte(colorString.Substring(4, 2), 16);
                var b = Convert.ToByte(colorString.Substring(6, 2), 16);
                color = Color.FromArgb(a, r, g, b);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 创建默认主题（浅色和深色）
    /// </summary>
    private void CreateDefaultThemes()
    {
        // 创建默认浅色主题
        var defaultLight = new ThemeModel
        {
            Name = "Default-Light",
            DisplayName = "浅色",
            Category = "默认",
            Description = "FluentAvaloniaUI 默认浅色主题",
            Variant = ThemeVariant.Light,
            AccentColor = new Color(255, 37, 99, 235), // 不设置自定义颜色，使用系统默认
            ColorResources = new Dictionary<string, Color>() // 空字典，使用系统默认
        };
        _cachedThemes.Add(defaultLight);

        // 创建默认深色主题
        var defaultDark = new ThemeModel
        {
            Name = "Default-Dark",
            DisplayName = "深色",
            Category = "默认",
            Description = "FluentAvaloniaUI 默认深色主题",
            Variant = ThemeVariant.Dark,
            AccentColor = new Color(255, 59, 130, 246), // 不设置自定义颜色，使用系统默认
            ColorResources = new Dictionary<string, Color>() // 空字典，使用系统默认
        };
        _cachedThemes.Add(defaultDark);

        Log.Debug("创建默认主题: Default-Light, Default-Dark");
    }

    /// <summary>
    /// 清除缓存，强制重新加载主题
    /// </summary>
    public void ClearCache()
    {
        _cachedThemes = null;
    }
}

