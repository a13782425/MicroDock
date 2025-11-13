using MicroDock.Plugin;
using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;

namespace Test;

/// <summary>
/// 测试插件，演示插件基础功能的使用
/// </summary>
public class TestPlugin : BaseMicroDockPlugin
{
    // 测试Tab实例
    private TestTab? _testTab;
    
    /// <summary>
    /// 所有标签页
    /// </summary>
    public override IMicroTab[] Tabs
    {
        get
        {
            if (_testTab == null)
            {
                _testTab = new TestTab();
            }
            return new IMicroTab[] { _testTab };
        }
    }
    
    // 设置控件引用（用于在事件处理中访问）
    private ToggleSwitch? _enableFeatureToggle;
    private Slider? _volumeSlider;
    private TextBlock? _volumeValueText;
    private TextBox? _customMessageTextBox;
    
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
        
        // 1. 布尔设置 - 启用功能开关
        var enableFeaturePanel = new StackPanel { Spacing = 4 };
        var enableFeatureLabel = new TextBlock
        {
            Text = "启用演示功能",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
        };
        _enableFeatureToggle = new ToggleSwitch
        {
            IsChecked = LoadBoolSetting("enable_feature", true)
        };
        _enableFeatureToggle.IsCheckedChanged += (s, e) =>
        {
            SaveBoolSetting("enable_feature", _enableFeatureToggle.IsChecked ?? false);
            LogInfo($"设置已更新: 启用功能 = {_enableFeatureToggle.IsChecked}");
        };
        enableFeaturePanel.Children.Add(enableFeatureLabel);
        enableFeaturePanel.Children.Add(_enableFeatureToggle);
        mainPanel.Children.Add(enableFeaturePanel);
        
        // 2. 数值设置 - 音量滑块
        var volumePanel = new StackPanel { Spacing = 4 };
        var volumeLabel = new TextBlock
        {
            Text = "音量大小",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
        };
        var volumeContainer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        _volumeSlider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = LoadIntSetting("volume", 50),
            Width = 200,
            VerticalAlignment = VerticalAlignment.Center
        };
        _volumeValueText = new TextBlock
        {
            Text = $"{(int)_volumeSlider.Value}",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 30
        };
        _volumeSlider.ValueChanged += (s, e) =>
        {
            var value = (int)e.NewValue;
            _volumeValueText!.Text = value.ToString();
            SaveIntSetting("volume", value);
            LogInfo($"设置已更新: 音量 = {value}");
        };
        volumeContainer.Children.Add(_volumeSlider);
        volumeContainer.Children.Add(_volumeValueText);
        volumePanel.Children.Add(volumeLabel);
        volumePanel.Children.Add(volumeContainer);
        mainPanel.Children.Add(volumePanel);
        
        // 3. 文本设置 - 自定义消息
        var messagePanel = new StackPanel { Spacing = 4 };
        var messageLabel = new TextBlock
        {
            Text = "自定义消息",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
        };
        _customMessageTextBox = new TextBox
        {
            Text = LoadStringSetting("custom_message", "Hello, MicroDock!"),
            Watermark = "请输入自定义消息...",
            MaxLength = 100
        };
        _customMessageTextBox.LostFocus += (s, e) =>
        {
            SaveStringSetting("custom_message", _customMessageTextBox.Text ?? "");
            LogInfo($"设置已更新: 自定义消息 = {_customMessageTextBox.Text}");
        };
        messagePanel.Children.Add(messageLabel);
        messagePanel.Children.Add(_customMessageTextBox);
        mainPanel.Children.Add(messagePanel);
        
        // 4. 操作按钮
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0)
        };
        
        var resetButton = new Button
        {
            Content = "重置为默认值",
            Padding = new Thickness(12, 6)
        };
        resetButton.Click += (s, e) =>
        {
            ResetToDefaults();
            LogInfo("设置已重置为默认值");
        };
        
        var testButton = new Button
        {
            Content = "测试功能",
            Padding = new Thickness(12, 6)
        };
        testButton.Click += (s, e) =>
        {
            TestFeature();
        };
        
        buttonPanel.Children.Add(resetButton);
        buttonPanel.Children.Add(testButton);
        mainPanel.Children.Add(buttonPanel);
        
        return mainPanel;
    }
    
    /// <summary>
    /// 加载布尔类型设置
    /// </summary>
    private bool LoadBoolSetting(string key, bool defaultValue)
    {
        var value = GetSettings(key);
        if (string.IsNullOrEmpty(value))
        {
            SetSettings(key, defaultValue.ToString(), $"布尔设置: {key}");
            return defaultValue;
        }
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
    
    /// <summary>
    /// 保存布尔类型设置
    /// </summary>
    private void SaveBoolSetting(string key, bool value)
    {
        SetSettings(key, value.ToString(), $"布尔设置: {key}");
    }
    
    /// <summary>
    /// 加载整数类型设置
    /// </summary>
    private int LoadIntSetting(string key, int defaultValue)
    {
        var value = GetSettings(key);
        if (string.IsNullOrEmpty(value))
        {
            SetSettings(key, defaultValue.ToString(), $"整数设置: {key}");
            return defaultValue;
        }
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
    
    /// <summary>
    /// 保存整数类型设置
    /// </summary>
    private void SaveIntSetting(string key, int value)
    {
        SetSettings(key, value.ToString(), $"整数设置: {key}");
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
    /// 重置所有设置为默认值
    /// </summary>
    private void ResetToDefaults()
    {
        if (_enableFeatureToggle != null)
        {
            _enableFeatureToggle.IsChecked = true;
            SaveBoolSetting("enable_feature", true);
        }
        
        if (_volumeSlider != null && _volumeValueText != null)
        {
            _volumeSlider.Value = 50;
            _volumeValueText.Text = "50";
            SaveIntSetting("volume", 50);
        }
        
        if (_customMessageTextBox != null)
        {
            _customMessageTextBox.Text = "Hello, MicroDock!";
            SaveStringSetting("custom_message", "Hello, MicroDock!");
        }
    }
    
    /// <summary>
    /// 测试功能按钮的处理
    /// </summary>
    private void TestFeature()
    {
        var enabled = LoadBoolSetting("enable_feature", true);
        var volume = LoadIntSetting("volume", 50);
        var message = LoadStringSetting("custom_message", "Hello, MicroDock!");
        
        LogInfo("=== 测试功能 ===");
        LogInfo($"启用功能: {enabled}");
        LogInfo($"音量大小: {volume}");
        LogInfo($"自定义消息: {message}");
        LogInfo("================");
    }
    
    /// <summary>
    /// 插件初始化 - 演示日志、键值存储、图片管理等功能
    /// </summary>
    public override void OnInit()
    {
        base.OnInit();
        
        // 演示日志功能
        LogInfo("测试插件初始化开始（通过 Initialize 方法调用）");
        LogDebug("这是一条调试日志");
        LogWarning("这是一条警告日志");
        
        try
        {
            // 演示键值存储功能
            LogInfo("演示键值存储功能");
            
            // 设置键值对
            SetValue("test_key", "test_value");
            SetValue("version", "1.0.0");
            SetValue("init_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            // 读取键值
            string? value = GetValue("test_key");
            LogInfo($"读取键值: test_key = {value}");
            
            // 获取所有键
            var allKeys = GetAllKeys();
            LogInfo($"当前有 {allKeys.Count} 个键: {string.Join(", ", allKeys)}");
            
            // 演示图片管理功能
            LogInfo("演示图片管理功能");
            
            // 创建一个简单的测试图片数据（1x1 像素的红色 BMP 图片）
            byte[] testImageData = CreateTestImage();
            SaveImage("test_icon", testImageData);
            LogInfo($"保存测试图片，大小: {testImageData.Length} 字节");
            
            // 加载图片
            byte[]? loadedImage = LoadImage("test_icon");
            if (loadedImage != null)
            {
                LogInfo($"成功加载图片，大小: {loadedImage.Length} 字节");
            }
            
            // 演示路径获取功能
            string configPath = GetConfigPath();
            string dataPath = GetPluginDataPath();
            LogInfo($"配置目录: {configPath}");
            LogInfo($"数据目录: {dataPath}");
            
            // 演示设置 API 功能
            LogInfo("演示设置 API 功能");
            
            // 初始化默认设置值（如果不存在）
            var enableFeature = LoadBoolSetting("enable_feature", true);
            var volume = LoadIntSetting("volume", 50);
            var customMessage = LoadStringSetting("custom_message", "Hello, MicroDock!");
            
            LogInfo($"当前设置 - 启用功能: {enableFeature}, 音量: {volume}, 消息: {customMessage}");
            
            // 获取所有设置键
            var allSettingsKeys = GetAllSettingsKeys();
            LogInfo($"当前有 {allSettingsKeys.Count} 个设置项: {string.Join(", ", allSettingsKeys)}");
            
            LogInfo("测试插件初始化完成");
        }
        catch (Exception ex)
        {
            LogError("测试插件初始化时发生错误", ex);
        }
    }
    
    /// <summary>
    /// 创建一个简单的测试图片（1x1 像素的红色 BMP）
    /// </summary>
    private byte[] CreateTestImage()
    {
        // BMP 文件头 + 1x1 红色像素
        return new byte[]
        {
            0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 
            0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 
            0x00, 0x00
        };
    }
    
    public override void OnEnable()
    {
        base.OnEnable();
        LogInfo("测试插件已启用");
    }
    
    public override void OnDisable()
    {
        base.OnDisable();
        LogInfo("测试插件已禁用");
    }
    
    public override void OnDestroy()
    {
        base.OnDestroy();
        LogInfo("测试插件正在销毁");
    }

    public override void OnAllPluginsLoaded()
    {
        base.OnAllPluginsLoaded();
        
        LogInfo("所有插件已加载完成，可以安全地调用其他插件的工具了");
        
        // 演示工具查询
        var availableTools = GetAvailableTools();
        LogInfo($"当前可用工具数量: {availableTools.Count}");
        foreach (var tool in availableTools)
        {
            LogInfo($"  - {tool.ProviderPlugin}.{tool.Name}: {tool.Description}");
        }
    }

    #region 工具示例

    /// <summary>
    /// 示例工具：字符串反转
    /// </summary>
    [MicroTool("test.reverse",
        Description = "反转字符串",
        ReturnDescription = "反转后的字符串")]
    public async Task<string> ReverseString(
        [ToolParameter("input", Description = "要反转的字符串")] string input)
    {
        await Task.CompletedTask;
        var reversed = new string(input.Reverse().ToArray());
        LogInfo($"字符串反转: '{input}' -> '{reversed}'");
        return reversed;
    }

    /// <summary>
    /// 示例工具：计算两个数的和
    /// </summary>
    [MicroTool("test.add",
        Description = "计算两个整数的和",
        ReturnDescription = "计算结果（JSON 字符串）")]
    public async Task<string> Add(
        [ToolParameter("a", Description = "第一个整数")] int a,
        [ToolParameter("b", Description = "第二个整数")] int b)
    {
        await Task.CompletedTask;
        var result = a + b;
        LogInfo($"计算: {a} + {b} = {result}");
        return result.ToString();
    }

    /// <summary>
    /// 示例工具：格式化问候语
    /// </summary>
    [MicroTool("test.greet",
        Description = "生成格式化的问候语",
        ReturnDescription = "格式化的问候消息")]
    public async Task<string> Greet(
        [ToolParameter("name", Description = "要问候的名字")] string name,
        [ToolParameter("times", Description = "重复次数", Required = false)] int times = 1)
    {
        await Task.CompletedTask;
        var greetings = new List<string>();
        for (int i = 0; i < times; i++)
        {
            greetings.Add($"Hello, {name}!");
        }
        var result = string.Join("\n", greetings);
        LogInfo($"生成问候语: {result.Replace("\n", " ")}");
        return result;
    }

    /// <summary>
    /// 示例工具：处理复杂对象
    /// </summary>
    [MicroTool("test.processData",
        Description = "处理复杂数据对象",
        ReturnDescription = "处理后的数据摘要（JSON）")]
    public async Task<string> ProcessData(
        [ToolParameter("items", Description = "字符串列表")] List<string> items,
        [ToolParameter("prefix", Description = "前缀", Required = false)] string prefix = "Item")
    {
        await Task.CompletedTask;
        var processed = items.Select((item, index) => $"{prefix}_{index + 1}: {item}").ToList();
        var result = System.Text.Json.JsonSerializer.Serialize(processed);
        LogInfo($"处理了 {items.Count} 个项目");
        return result;
    }

    /// <summary>
    /// 静态方法工具示例：格式化日期时间
    /// </summary>
    [MicroTool("test.static_format_date",
        Description = "静态方法：格式化当前日期时间",
        ReturnDescription = "格式化后的日期时间字符串")]
    public static async Task<string> FormatCurrentDate(
        [ToolParameter("format", Description = "日期格式 (如 yyyy-MM-dd)", Required = false)] string format = "yyyy-MM-dd HH:mm:ss")
    {
        await Task.CompletedTask;
        return DateTime.Now.ToString(format);
    }

    #endregion
}

/// <summary>
/// 辅助类工具示例（演示实例复用和状态管理）
/// </summary>
internal class TestHelper
{
    private int _callCount = 0;
    private readonly List<string> _history = new();

    /// <summary>
    /// 工具：获取调用次数（演示实例复用）
    /// </summary>
    [MicroTool("test.helper_count",
        Description = "辅助类方法：返回此工具实例的调用次数（演示实例复用）",
        ReturnDescription = "调用次数")]
    public async Task<string> GetCallCount()
    {
        await Task.CompletedTask;
        _callCount++;
        return $"此实例已被调用 {_callCount} 次";
    }

    /// <summary>
    /// 工具：字符串反转（辅助类版本）
    /// </summary>
    [MicroTool("test.helper_reverse",
        Description = "辅助类方法：反转字符串",
        ReturnDescription = "反转后的字符串")]
    public async Task<string> ReverseString(
        [ToolParameter("text", Description = "要反转的字符串")] string text)
    {
        await Task.CompletedTask;
        var reversed = new string(text.Reverse().ToArray());
        _history.Add($"反转: {text} -> {reversed}");
        return reversed;
    }

    /// <summary>
    /// 工具：获取操作历史（演示状态持久化）
    /// </summary>
    [MicroTool("test.helper_history",
        Description = "辅助类方法：获取所有操作历史",
        ReturnDescription = "历史记录（JSON 数组）")]
    public async Task<string> GetHistory()
    {
        await Task.CompletedTask;
        return System.Text.Json.JsonSerializer.Serialize(_history);
    }
}

/// <summary>
/// 非公共辅助类（演示扫描非公共类）
/// </summary>
internal class PrivateHelper
{
    private DateTime _createdAt = DateTime.Now;

    /// <summary>
    /// 工具：获取实例创建时间
    /// </summary>
    [MicroTool("test.private_created_time",
        Description = "非公共类方法：获取工具实例的创建时间",
        ReturnDescription = "实例创建时间")]
    public async Task<string> GetCreatedTime()
    {
        await Task.CompletedTask;
        var elapsed = DateTime.Now - _createdAt;
        return $"实例创建于 {_createdAt:HH:mm:ss}，已存活 {elapsed.TotalSeconds:F1} 秒";
    }
}