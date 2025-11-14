using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MicroDock.Plugin;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BuglySymbolResolver;

/// <summary>
/// 解析结果项 - 用于界面显示
/// </summary>
public class ParseResultItem : INotifyPropertyChanged
{
    private string _index = string.Empty;
    private bool _success;
    private string _statusIcon = string.Empty;
    private string _resultText = string.Empty;
    private IBrush? _statusColor;

    public string Index
    {
        get => _index;
        set { _index = value; OnPropertyChanged(); }
    }

    public bool Success
    {
        get => _success;
        set { _success = value; OnPropertyChanged(); }
    }

    public string StatusIcon
    {
        get => _statusIcon;
        set { _statusIcon = value; OnPropertyChanged(); }
    }

    public string ResultText
    {
        get => _resultText;
        set { _resultText = value; OnPropertyChanged(); }
    }

    public IBrush? StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public partial class BuglySymbolResolverTab : UserControl, IMicroTab
{
    public string TabName => "符号解析";

    public IconSymbolEnum IconSymbol => IconSymbolEnum.Code;
    
    private BuglySymbolResolverPlugin? _plugin;
    
    // 符号文件扫描结果缓存
    private SymbolFileScanner.ScanResult? _currentScanResult;
    
    // 解析结果集合（用于界面绑定）
    public ObservableCollection<ParseResultItem> ParseResults { get; }
    
    // 状态颜色画刷
    private readonly SolidColorBrush _successBrush;
    private readonly SolidColorBrush _errorBrush;
    
    public BuglySymbolResolverTab()
    {
        // 初始化集合和画刷
        ParseResults = new ObservableCollection<ParseResultItem>();
        _successBrush = new SolidColorBrush(Color.FromRgb(40, 167, 69));  // 绿色
        _errorBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69));    // 红色
        
        InitializeComponent();
        
        // 设置 DataContext 以支持数据绑定
        DataContext = this;
    }
    
    /// <summary>
    /// 设置插件引用（用于访问设置）
    /// </summary>
    public void SetPlugin(BuglySymbolResolverPlugin plugin)
    {
        _plugin = plugin;
    }
    
    private async void SymbolPathButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // 使用新的 StorageProvider API
        Avalonia.Controls.TopLevel? topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;
        
        Avalonia.Platform.Storage.IStorageProvider? storageProvider = topLevel.StorageProvider;
        if (storageProvider == null)
            return;
        
        var folderPickerOptions = new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "选择符号文件夹",
            AllowMultiple = false
        };
        
        System.Collections.Generic.IReadOnlyList<Avalonia.Platform.Storage.IStorageFolder> result = 
            await storageProvider.OpenFolderPickerAsync(folderPickerOptions);
        
        if (result.Count > 0)
        {
            string folderPath = result[0].Path.LocalPath;
            SymbolPathTextBox!.Text = folderPath;
            
            // 立即扫描符号文件夹
            await ScanSymbolFolderAsync(folderPath);
        }
    }
    
    /// <summary>
    /// 扫描符号文件夹并更新UI
    /// </summary>
    private async Task ScanSymbolFolderAsync(string folderPath)
    {
        if (StatusTextBlock != null)
        {
            StatusTextBlock.Text = "正在扫描符号文件夹...";
        }
        
        try
        {
            // 异步扫描符号文件夹
            _currentScanResult = await SymbolFileScanner.ScanSymbolFolderAsync(folderPath);
            
            // 更新符号文件信息展示
            if (SymbolInfoBorder != null)
            {
                SymbolInfoBorder.IsVisible = true;
            }
            
            if (Symbol32CountText != null)
            {
                Symbol32CountText.Text = _currentScanResult.Count32Bit.ToString();
            }
            
            if (Symbol64CountText != null)
            {
                Symbol64CountText.Text = _currentScanResult.Count64Bit.ToString();
            }
            
            if (SymbolTotalCountText != null)
            {
                SymbolTotalCountText.Text = _currentScanResult.TotalCount.ToString();
            }
            
            if (StatusTextBlock != null)
            {
                if (_currentScanResult.TotalCount > 0)
                {
                    StatusTextBlock.Text = $"扫描完成: 找到 {_currentScanResult.TotalCount} 个符号文件";
                }
                else
                {
                    StatusTextBlock.Text = "警告: 未找到符号文件";
                }
            }
        }
        catch (Exception ex)
        {
            if (StatusTextBlock != null)
            {
                StatusTextBlock.Text = $"扫描失败: {ex.Message}";
            }
            
            if (SymbolInfoBorder != null)
            {
                SymbolInfoBorder.IsVisible = false;
            }
            
            _currentScanResult = null;
        }
    }
    
    private async void ParseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (InputTextBox == null || StatusTextBlock == null)
            return;
        
        string inputText = InputTextBox.Text ?? "";
        if (string.IsNullOrWhiteSpace(inputText))
        {
            StatusTextBlock.Text = "错误: 请输入崩溃/ANR信息";
            return;
        }
        
        // 检查是否已扫描符号文件夹
        if (_currentScanResult == null || _currentScanResult.TotalCount == 0)
        {
            StatusTextBlock.Text = "错误: 请先选择符号文件夹并确保扫描到符号文件";
            return;
        }
        
        // 获取架构选择
        StackParser.ArchitectureType architectureType = StackParser.ArchitectureType.Auto;
        if (ArchitectureComboBox != null)
        {
            int selectedIndex = ArchitectureComboBox.SelectedIndex;
            architectureType = selectedIndex switch
            {
                0 => StackParser.ArchitectureType.Auto,
                1 => StackParser.ArchitectureType.Bit32,
                2 => StackParser.ArchitectureType.Bit64,
                _ => StackParser.ArchitectureType.Auto
            };
        }
        
        // 获取解析器路径
        string? resolver32BitPath = null;
        string? resolver64BitPath = null;
        
        if (_plugin != null)
        {
            resolver32BitPath = _plugin.GetSettingsValue("resolver_32bit");
            resolver64BitPath = _plugin.GetSettingsValue("resolver_64bit");
        }
        
        if (string.IsNullOrEmpty(resolver32BitPath) && string.IsNullOrEmpty(resolver64BitPath))
        {
            StatusTextBlock.Text = "错误: 请先在设置中配置解析器路径";
            return;
        }
        
        // 禁用解析按钮
        if (ParseButton != null)
        {
            ParseButton.IsEnabled = false;
        }
        
        StatusTextBlock.Text = "正在解析...";
        ParseResults.Clear();  // 清空之前的结果
        
        try
        {
            // 提取堆栈信息
            var stacks = StackParser.ExtractStacks(inputText);
            
            if (stacks.Count == 0)
            {
                StatusTextBlock.Text = "警告: 未找到堆栈信息";
                
                // 添加一条提示信息
                ParseResults.Add(new ParseResultItem
                {
                    Index = "提示",
                    Success = false,
                    StatusIcon = "ℹ️",
                    ResultText = "未在输入文本中找到符合格式的堆栈信息。\n格式示例: #03 pc 00000000004fe08c /path/to/libunity.so",
                    StatusColor = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                });
                return;
            }
            
            StatusTextBlock.Text = $"找到 {stacks.Count} 个堆栈，正在解析...";
            
            // 批量解析（使用符号文件夹扫描结果）
            var results = await StackParser.ResolveStacksAsync(
                stacks,
                architectureType,
                resolver32BitPath,
                resolver64BitPath,
                _currentScanResult);
            
            // 将结果添加到集合
            int successCount = 0;
            int failCount = 0;
            
            for (int i = 0; i < results.Count && i < stacks.Count; i++)
            {
                var result = results[i];
                var stack = stacks[i];
                
                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
                
                ParseResults.Add(new ParseResultItem
                {
                    Index = $"#{stack.Index}",
                    Success = result.Success,
                    StatusIcon = result.Success ? "✓" : "✗",
                    ResultText = result.SymbolizedLine ?? result.OriginalLine,
                    StatusColor = result.Success ? _successBrush : _errorBrush
                });
            }
            
            StatusTextBlock.Text = $"解析完成: 成功 {successCount} 个，失败 {failCount} 个";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"错误: {ex.Message}";
            
            // 添加错误信息
            ParseResults.Add(new ParseResultItem
            {
                Index = "错误",
                Success = false,
                StatusIcon = "✗",
                ResultText = $"解析过程中发生错误:\n{ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}",
                StatusColor = _errorBrush
            });
        }
        finally
        {
            // 重新启用解析按钮
            if (ParseButton != null)
            {
                ParseButton.IsEnabled = true;
            }
        }
    }
    
    private void ClearButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (InputTextBox != null)
        {
            InputTextBox.Text = "";
        }
        
        // 清空解析结果集合
        ParseResults.Clear();
        
        if (StatusTextBlock != null)
        {
            StatusTextBlock.Text = "就绪";
        }
        
        // 注意：不清空符号文件夹路径和扫描结果，因为用户可能需要多次解析不同的崩溃信息
        // 如果需要重新选择符号文件夹，用户可以点击浏览按钮
    }
}

