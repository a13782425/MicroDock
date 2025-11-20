using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using MicroDock.Model;
using Serilog;

namespace MicroDock.Service;

/// <summary>
/// 主题服务，负责主题的加载和应用
/// </summary>
public class ThemeService : IDisposable
{
    private readonly ThemeLoader _themeLoader;
    private ThemeModel? _currentTheme;
    private ThemeVariant? _currentCustomVariant; // 当前使用的自定义ThemeVariant
    private System.Timers.Timer? _cleanupTimer; // 延迟清理定时器
    private readonly Dictionary<ThemeVariant, ResourceDictionary> _pendingCleanupDictionaries = new(); // 待清理的资源字典

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
            ThemeVariant targetVariant;
            if (theme.Name.StartsWith("Default-", StringComparison.OrdinalIgnoreCase))
            {
                // 系统默认主题，直接使用
                targetVariant = theme.Variant;
                _currentCustomVariant = null;
            }
            else
            {
                // 自定义主题，创建新的 ThemeVariant 实例
                targetVariant = new ThemeVariant(theme.Name, theme.Variant);
                
                // 将旧的自定义 ThemeVariant 对应的资源字典加入待清理列表
                if (_currentCustomVariant != null && 
                    Application.Current.Resources.ThemeDictionaries.TryGetValue(_currentCustomVariant, out IThemeVariantProvider? oldProvider) &&
                    oldProvider is ResourceDictionary oldDict)
                {
                    _pendingCleanupDictionaries[_currentCustomVariant] = oldDict;
                    ScheduleCleanup();
                }
                
                _currentCustomVariant = targetVariant;
            }

            Application.Current.RequestedThemeVariant = targetVariant;
            
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
    /// 应用颜色资源到独立的主题资源字典
    /// </summary>
    private void ApplyColorResources(Dictionary<string, Color> colorResources)
    {
        if (Application.Current?.Resources == null || _currentCustomVariant == null)
        {
            return;
        }

        try
        {
            // 创建独立的资源字典
            ResourceDictionary themeResources = new ResourceDictionary();
            
            foreach (var kvp in colorResources)
            {
                try
                {
                    Color color = kvp.Value;
                    string key = kvp.Key;

                    // 添加Color资源到主题专属字典
                    themeResources[key] = color;

                    // 添加对应的Brush资源
                    if (!key.EndsWith("Brush", StringComparison.OrdinalIgnoreCase))
                    {
                        string brushKey = key + "Brush";
                        themeResources[brushKey] = new SolidColorBrush(color);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "添加颜色资源失败: {Key}", kvp.Key);
                }
            }
            
            // 将资源字典添加到 ThemeDictionaries
            Application.Current.Resources.ThemeDictionaries[_currentCustomVariant] = themeResources;
            
            Log.Debug("已为主题 {ThemeName} 创建独立资源字典，包含 {Count} 个资源", 
                _currentCustomVariant.Key, themeResources.Count);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "应用颜色资源失败");
        }
    }

    /// <summary>
    /// 调度延迟清理任务
    /// </summary>
    private void ScheduleCleanup()
    {
        // 停止现有的定时器
        _cleanupTimer?.Stop();
        _cleanupTimer?.Dispose();
        
        // 创建新的定时器，30秒后执行清理
        _cleanupTimer = new System.Timers.Timer(30000);
        _cleanupTimer.AutoReset = false;
        _cleanupTimer.Elapsed += (sender, e) => CleanupOldThemeResources();
        _cleanupTimer.Start();
        
        Log.Debug("已调度主题资源延迟清理任务（30秒后执行）");
    }

    /// <summary>
    /// 清理旧主题的资源字典
    /// </summary>
    private void CleanupOldThemeResources()
    {
        if (Application.Current?.Resources == null)
        {
            return;
        }

        try
        {
            int cleanedCount = 0;
            foreach (KeyValuePair<ThemeVariant, ResourceDictionary> kvp in _pendingCleanupDictionaries)
            {
                ThemeVariant variant = kvp.Key;
                ResourceDictionary dict = kvp.Value;
                
                // 从 ThemeDictionaries 中移除
                if (Application.Current.Resources.ThemeDictionaries.ContainsKey(variant))
                {
                    Application.Current.Resources.ThemeDictionaries.Remove(variant);
                }
                
                // 清空资源字典本身
                dict.Clear();
                cleanedCount++;
            }
            
            _pendingCleanupDictionaries.Clear();
            
            if (cleanedCount > 0)
            {
                Log.Information("已清理 {Count} 个旧主题的资源字典", cleanedCount);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "清理旧主题资源失败");
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

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Stop();
        _cleanupTimer?.Dispose();
    }
}

