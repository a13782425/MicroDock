using MicroDock.Plugin;
using System;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace BuglySymbolResolver;

/// <summary>
/// 插件配置模型
/// </summary>
public class PluginSettings
{
    /// <summary>
    /// 32位解析器路径
    /// </summary>
    public string Resolver32Bit { get; set; } = string.Empty;

    /// <summary>
    /// 64位解析器路径
    /// </summary>
    public string Resolver64Bit { get; set; } = string.Empty;
}

/// <summary>
/// Bugly符号解析插件
/// 注意：插件元数据现在通过 plugin.json 文件定义
/// </summary>
public class BuglySymbolResolverPlugin : BaseMicroDockPlugin
{
    // Tab实例
    private BuglySymbolResolverTab? _tab;

    // 设置控件引用
    private TextBox? _resolver32BitTextBox;
    private TextBox? _resolver64BitTextBox;

    // 配置实例（缓存在内存中）
    private PluginSettings? _settings;

    // 配置文件名
    private const string SettingsFileName = "settings.json";

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
        if (_settings == null)
        {
            _settings = LoadSettingsFromFile();
        }

        return key switch
        {
            "resolver_32bit" => _settings.Resolver32Bit,
            "resolver_64bit" => _settings.Resolver64Bit,
            _ => null
        };
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

        // 使用新的 StorageProvider API
        if (Avalonia.Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null)
            return;
        
        Avalonia.Platform.Storage.IStorageProvider? storageProvider = desktop.MainWindow.StorageProvider;
        if (storageProvider == null)
            return;
        
        // 定义文件类型过滤器
        var filePickerFileTypes = new Avalonia.Platform.Storage.FilePickerFileType[]
        {
            new("Executable files")
            {
                Patterns = new[] { "*.exe", "*.bat", "*.cmd" }
            },
            Avalonia.Platform.Storage.FilePickerFileTypes.All
        };
        
        var filePickerOptions = new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = dialogTitle,
            AllowMultiple = false,
            FileTypeFilter = filePickerFileTypes
        };
        
        System.Collections.Generic.IReadOnlyList<Avalonia.Platform.Storage.IStorageFile> result = 
            await storageProvider.OpenFilePickerAsync(filePickerOptions);
        
        if (result.Count > 0)
        {
            string path = result[0].Path.LocalPath;
            textBox.Text = path;
            SaveStringSetting(settingKey, path);
            LogInfo($"设置已更新: {settingKey} = {path}");
        }
    }

    /// <summary>
    /// 从文件加载配置
    /// </summary>
    private PluginSettings LoadSettingsFromFile()
    {
        try
        {
            var dataPath = GetPluginDataPath();
            var settingsPath = Path.Combine(dataPath, SettingsFileName);

            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<PluginSettings>(json);
                if (settings != null)
                {
                    LogInfo($"配置已从文件加载: {settingsPath}");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"加载配置文件失败: {ex.Message}", ex);
        }

        // 返回默认配置
        return new PluginSettings();
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    private void SaveSettingsToFile(PluginSettings settings)
    {
        try
        {
            var dataPath = GetPluginDataPath();
            
            // 确保目录存在
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
                LogInfo($"创建数据目录: {dataPath}");
            }

            var settingsPath = Path.Combine(dataPath, SettingsFileName);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(settingsPath, json);
            LogInfo($"配置已保存到文件: {settingsPath}");
        }
        catch (Exception ex)
        {
            LogError($"保存配置文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 加载字符串类型设置
    /// </summary>
    private string LoadStringSetting(string key, string defaultValue)
    {
        if (_settings == null)
        {
            _settings = LoadSettingsFromFile();
        }

        return key switch
        {
            "resolver_32bit" => string.IsNullOrEmpty(_settings.Resolver32Bit) ? defaultValue : _settings.Resolver32Bit,
            "resolver_64bit" => string.IsNullOrEmpty(_settings.Resolver64Bit) ? defaultValue : _settings.Resolver64Bit,
            _ => defaultValue
        };
    }

    /// <summary>
    /// 保存字符串类型设置
    /// </summary>
    private void SaveStringSetting(string key, string value)
    {
        if (_settings == null)
        {
            _settings = LoadSettingsFromFile();
        }

        switch (key)
        {
            case "resolver_32bit":
                _settings.Resolver32Bit = value;
                break;
            case "resolver_64bit":
                _settings.Resolver64Bit = value;
                break;
        }

        SaveSettingsToFile(_settings);
    }

    /// <summary>
    /// 插件初始化
    /// </summary>
    public override void OnInit()
    {
        base.OnInit();
        
        try
        {
            // 确保数据目录存在
            var dataPath = GetPluginDataPath();
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
                LogInfo($"创建数据目录: {dataPath}");
            }

            // 加载或创建配置文件
            _settings = LoadSettingsFromFile();
            
            LogInfo("Bugly符号解析插件初始化完成");
        }
        catch (Exception ex)
        {
            LogError($"插件初始化失败: {ex.Message}", ex);
        }
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