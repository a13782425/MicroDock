using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using MicroDock.Models;
using Serilog;

namespace MicroDock.Services;

/// <summary>
/// 主题服务，负责主题的加载和应用
/// </summary>
public class ThemeService
{
    private readonly ThemeLoader _themeLoader;
    private ThemeModel? _currentTheme;

    public ThemeService()
    {
        _themeLoader = new ThemeLoader();
    }

    /// <summary>
    /// 获取所有可用主题
    /// </summary>
    public List<ThemeModel> GetAvailableThemes()
    {
        return _themeLoader.LoadAllThemes();
    }

    /// <summary>
    /// 根据主题名称加载并应用主题
    /// </summary>
    public bool LoadAndApplyTheme(string themeName)
    {
        try
        {
            var theme = _themeLoader.LoadTheme(themeName);
            if (theme == null)
            {
                Log.Warning("主题不存在: {ThemeName}", themeName);
                return false;
            }

            ApplyTheme(theme);
            _currentTheme = theme;
            Log.Information("主题已应用: {ThemeName} ({DisplayName})", theme.Name, theme.DisplayName);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载并应用主题失败: {ThemeName}", themeName);
            return false;
        }
    }

    /// <summary>
    /// 应用主题到应用程序
    /// </summary>
    public void ApplyTheme(ThemeModel theme)
    {
        if (Application.Current == null)
        {
            Log.Warning("Application.Current 为 null，无法应用主题");
            return;
        }

        try
        {
            // 1. 设置主题变体
            Application.Current.RequestedThemeVariant = theme.Variant;

            // 2. 获取FluentAvaloniaTheme实例
            var fluentTheme = Application.Current.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();
            if (fluentTheme == null)
            {
                Log.Warning("未找到FluentAvaloniaTheme实例");
                return;
            }

            // 3. 设置强调色（默认主题不设置自定义颜色）
            if (!theme.Name.StartsWith("Default-", StringComparison.OrdinalIgnoreCase))
            {
                if (theme.AccentColor != Colors.Transparent)
                {
                    fluentTheme.CustomAccentColor = theme.AccentColor;
                }

                // 4. 应用自定义颜色资源（覆盖FluentAvaloniaTheme的默认值）
                ApplyColorResources(theme.ColorResources);
            }
            // 默认主题不设置自定义颜色，让 FluentAvaloniaTheme 使用系统默认

            _currentTheme = theme;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "应用主题失败");
        }
    }

    /// <summary>
    /// 应用颜色资源到Application.Resources
    /// </summary>
    private void ApplyColorResources(Dictionary<string, Color> colorResources)
    {
        if (Application.Current?.Resources == null)
        {
            return;
        }

        foreach (var kvp in colorResources)
        {
            try
            {
                var color = kvp.Value;
                var key = kvp.Key;

                // 添加Color资源
                Application.Current.Resources[key] = color;

                // 添加对应的Brush资源（如果Key不包含"Brush"后缀）
                if (!key.EndsWith("Brush", StringComparison.OrdinalIgnoreCase))
                {
                    var brushKey = key + "Brush";
                    Application.Current.Resources[brushKey] = new SolidColorBrush(color);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "应用颜色资源失败: {Key}", kvp.Key);
            }
        }
    }

    /// <summary>
    /// 获取当前应用的主题
    /// </summary>
    public ThemeModel? GetCurrentTheme()
    {
        return _currentTheme;
    }

    /// <summary>
    /// 重新加载主题列表（清除缓存）
    /// </summary>
    public void ReloadThemes()
    {
        _themeLoader.ClearCache();
    }
}

