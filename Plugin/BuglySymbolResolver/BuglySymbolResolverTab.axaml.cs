using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MicroDock.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BuglySymbolResolver;

public partial class BuglySymbolResolverTab : UserControl, IMicroTab
{
    public string TabName => "符号解析";

    public IconSymbolEnum IconSymbol => IconSymbolEnum.Code;
    
    private BuglySymbolResolverPlugin? _plugin;
    
    public BuglySymbolResolverTab()
    {
        InitializeComponent();
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
        var dialog = new OpenFileDialog
        {
            Title = "选择符号文件",
            AllowMultiple = false,
            Filters = new List<FileDialogFilter>
            {
                new() { Name = "Symbol files", Extensions = { "so", "dylib", "dll" } },
                new() { Name = "All files", Extensions = { "*" } }
            }
        };
        
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            string[]? result = await dialog.ShowAsync(desktop.MainWindow);
            if (result != null && result.Length > 0)
            {
                SymbolPathTextBox!.Text = result[0];
            }
        }
    }
    
    private async void ParseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (InputTextBox == null || OutputTextBox == null || StatusTextBlock == null)
            return;
        
        string inputText = InputTextBox.Text ?? "";
        if (string.IsNullOrWhiteSpace(inputText))
        {
            StatusTextBlock.Text = "错误: 请输入崩溃/ANR信息";
            return;
        }
        
        string symbolPath = SymbolPathTextBox?.Text ?? "";
        if (string.IsNullOrWhiteSpace(symbolPath) || !File.Exists(symbolPath))
        {
            StatusTextBlock.Text = "错误: 请选择有效的符号文件路径";
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
        OutputTextBox.Text = "";
        
        try
        {
            // 提取堆栈信息
            var stacks = StackParser.ExtractStacks(inputText);
            
            if (stacks.Count == 0)
            {
                StatusTextBlock.Text = "警告: 未找到堆栈信息";
                OutputTextBox.Text = "未在输入文本中找到符合格式的堆栈信息。\n格式示例: #03 pc 00000000004fe08c /path/to/libunity.so";
                return;
            }
            
            StatusTextBlock.Text = $"找到 {stacks.Count} 个堆栈，正在解析...";
            
            // 批量解析
            var results = await StackParser.ResolveStacksAsync(
                stacks,
                architectureType,
                resolver32BitPath,
                resolver64BitPath,
                symbolPath);
            
            // 格式化输出
            var outputBuilder = new System.Text.StringBuilder();
            int successCount = 0;
            int failCount = 0;
            
            foreach (var result in results)
            {
                if (result.Success)
                {
                    successCount++;
                    outputBuilder.AppendLine(result.SymbolizedLine);
                }
                else
                {
                    failCount++;
                    outputBuilder.AppendLine(result.SymbolizedLine ?? result.OriginalLine);
                }
            }
            
            OutputTextBox.Text = outputBuilder.ToString();
            StatusTextBlock.Text = $"解析完成: 成功 {successCount} 个，失败 {failCount} 个";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"错误: {ex.Message}";
            OutputTextBox.Text = $"解析过程中发生错误:\n{ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}";
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
        if (OutputTextBox != null)
        {
            OutputTextBox.Text = "";
        }
        if (StatusTextBlock != null)
        {
            StatusTextBlock.Text = "就绪";
        }
    }
}

