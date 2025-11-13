using MicroDock.Plugin;
using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace BuglySymbolResolver;

/// <summary>
/// Bugly符号解析插件
/// </summary>
public class BuglySymbolResolverPlugin : BaseMicroDockPlugin
{
    /// <summary>
    /// 插件唯一名称
    /// </summary>
    public override string UniqueName => "com.micro.buglysymbolresolver";

    public override string DisplayName => "Bugly符号解析";

    /// <summary>
    /// 依赖的插件列表
    /// </summary>
    public override string[] Dependencies => Array.Empty<string>();
    
    /// <summary>
    /// 插件版本
    /// </summary>
    public override Version PluginVersion => new Version(1, 0, 0);
    
    // Tab实例
    private BuglySymbolResolverTab? _tab;
    
    // 设置控件引用
    private TextBox? _resolver32BitTextBox;
    private TextBox? _resolver64BitTextBox;
    
    /// <summary>
    /// 所有标签页
    /// </summary>
    public override IMicroTab[] Tabs
    {
        get
        {
            if (_tab == null)
            {
                _tab = new BuglySymbolResolverTab();
                _tab.SetPlugin(this);
            }
            return new IMicroTab[] { _tab };
        }
    }
    
    /// <summary>
    /// 获取设置（供Tab访问）
    /// </summary>
    public string? GetSettingsValue(string key)
    {
        return GetSettings(key);
    }
    
    /// <summary>
    /// 获取插件的设置UI控件
    /// </summary>
    public override object? GetSettingsControl()
    {
        // 创建主容器
        var mainPanel = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(8, 0, 0, 0)
        };
        
        // 32位解析器路径配置
        var resolver32Panel = new StackPanel { Spacing = 4 };
        var resolver32Label = new TextBlock
        {
            Text = "32位解析器路径",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
        };
        var resolver32Container = new Grid
        {
            ColumnSpacing = 8
        };
        resolver32Container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        resolver32Container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var resolver32Path = LoadStringSetting("resolver_32bit", "");
        _resolver32BitTextBox = new TextBox
        {
            Text = resolver32Path,
            Watermark = "请选择32位解析器路径...",
            VerticalAlignment = VerticalAlignment.Center,
            IsReadOnly = true,
            [Grid.ColumnProperty] = 0
        };
        ToolTip.SetTip(_resolver32BitTextBox, resolver32Path);
        var resolver32Button = new Button
        {
            Content = "浏览",
            Padding = new Thickness(12, 6),
            VerticalAlignment = VerticalAlignment.Center,
            [Grid.ColumnProperty] = 1
        };
        resolver32Button.Click += async (s, e) =>
        {
            await SelectResolverPath(_resolver32BitTextBox, "resolver_32bit", "选择32位解析器");
            // 更新 ToolTip 显示完整路径
            if (_resolver32BitTextBox != null && !string.IsNullOrEmpty(_resolver32BitTextBox.Text))
            {
                ToolTip.SetTip(_resolver32BitTextBox, _resolver32BitTextBox.Text);
            }
        };
        resolver32Container.Children.Add(_resolver32BitTextBox);
        resolver32Container.Children.Add(resolver32Button);
        resolver32Panel.Children.Add(resolver32Label);
        resolver32Panel.Children.Add(resolver32Container);
        mainPanel.Children.Add(resolver32Panel);
        
        // 64位解析器路径配置
        var resolver64Panel = new StackPanel { Spacing = 4 };
        var resolver64Label = new TextBlock
        {
            Text = "64位解析器路径",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
        };
        var resolver64Container = new Grid
        {
            ColumnSpacing = 8
        };
        resolver64Container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        resolver64Container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var resolver64Path = LoadStringSetting("resolver_64bit", "");
        _resolver64BitTextBox = new TextBox
        {
            Text = resolver64Path,
            Watermark = "请选择64位解析器路径...",
            VerticalAlignment = VerticalAlignment.Center,
            IsReadOnly = true,
            [Grid.ColumnProperty] = 0
        };
        ToolTip.SetTip(_resolver64BitTextBox, resolver64Path);
        var resolver64Button = new Button
        {
            Content = "浏览",
            Padding = new Thickness(12, 6),
            VerticalAlignment = VerticalAlignment.Center,
            [Grid.ColumnProperty] = 1
        };
        resolver64Button.Click += async (s, e) =>
        {
            await SelectResolverPath(_resolver64BitTextBox, "resolver_64bit", "选择64位解析器");
            // 更新 ToolTip 显示完整路径
            if (_resolver64BitTextBox != null && !string.IsNullOrEmpty(_resolver64BitTextBox.Text))
            {
                ToolTip.SetTip(_resolver64BitTextBox, _resolver64BitTextBox.Text);
            }
        };
        resolver64Container.Children.Add(_resolver64BitTextBox);
        resolver64Container.Children.Add(resolver64Button);
        resolver64Panel.Children.Add(resolver64Label);
        resolver64Panel.Children.Add(resolver64Container);
        mainPanel.Children.Add(resolver64Panel);
        
        return mainPanel;
    }
    
    /// <summary>
    /// 选择解析器路径
    /// </summary>
    private async System.Threading.Tasks.Task SelectResolverPath(TextBox? textBox, string settingKey, string dialogTitle)
    {
        if (textBox == null) return;
        
        var dialog = new OpenFileDialog
        {
            Title = dialogTitle,
            AllowMultiple = false,
            Filters = new System.Collections.Generic.List<FileDialogFilter>
            {
                new() { Name = "Executable files", Extensions = { "exe", "bat", "cmd" } },
                new() { Name = "All files", Extensions = { "*" } }
            }
        };
        
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            string[]? result = await dialog.ShowAsync(desktop.MainWindow);
            if (result != null && result.Length > 0)
            {
                string path = result[0];
                textBox.Text = path;
                SaveStringSetting(settingKey, path);
                LogInfo($"设置已更新: {settingKey} = {path}");
            }
        }
    }
    
    /// <summary>
    /// 加载字符串类型设置
    /// </summary>
    private string LoadStringSetting(string key, string defaultValue)
    {
        var value = GetSettings(key);
        if (string.IsNullOrEmpty(value))
        {
            SetSettings(key, defaultValue, $"字符串设置: {key}");
            return defaultValue;
        }
        return value;
    }
    
    /// <summary>
    /// 保存字符串类型设置
    /// </summary>
    private void SaveStringSetting(string key, string value)
    {
        SetSettings(key, value, $"字符串设置: {key}");
    }
    
    /// <summary>
    /// 插件初始化
    /// </summary>
    public override void OnInit()
    {
        base.OnInit();
        LogInfo("Bugly符号解析插件初始化完成");
    }
    
    public override void OnEnable()
    {
        base.OnEnable();
        LogInfo("Bugly符号解析插件已启用");
    }
    
    public override void OnDisable()
    {
        base.OnDisable();
        LogInfo("Bugly符号解析插件已禁用");
    }
    
    public override void OnDestroy()
    {
        base.OnDestroy();
        LogInfo("Bugly符号解析插件正在销毁");
    }
}

