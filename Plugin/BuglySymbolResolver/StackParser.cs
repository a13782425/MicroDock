using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuglySymbolResolver;

/// <summary>
/// 堆栈解析器
/// </summary>
public class StackParser
{
    /// <summary>
    /// 架构类型
    /// </summary>
    public enum ArchitectureType
    {
        Auto,
        Bit32,
        Bit64
    }
    
    /// <summary>
    /// 堆栈信息
    /// </summary>
    public class StackInfo
    {
        public string Index { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SoFile { get; set; } = string.Empty;
        public string FullLine { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 解析结果
    /// </summary>
    public class ParseResult
    {
        public string OriginalLine { get; set; } = string.Empty;
        public string? SymbolizedLine { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// 从输入文本中提取堆栈信息
    /// </summary>
    public static List<StackInfo> ExtractStacks(string inputText)
    {
        var stacks = new List<StackInfo>();
        
        // 匹配格式: #03 pc 00000000004fe08c /data/app/.../libunity.so
        var pattern = @"#(\d+)\s+pc\s+([0-9a-fA-F]+)\s+(.+\.so)";
        var matches = Regex.Matches(inputText, pattern);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 4)
            {
                stacks.Add(new StackInfo
                {
                    Index = match.Groups[1].Value,
                    Address = match.Groups[2].Value,
                    SoFile = match.Groups[3].Value.Trim(),
                    FullLine = match.Value
                });
            }
        }
        
        return stacks;
    }
    
    /// <summary>
    /// 检测架构类型（根据堆栈地址长度）
    /// </summary>
    public static ArchitectureType DetectArchitecture(string address)
    {
        // 32位地址是8位十六进制（4字节），64位是16位十六进制（8字节）
        // 根据原始地址长度判断
        if (address.Length > 8)
        {
            return ArchitectureType.Bit64;
        }
        else
        {
            return ArchitectureType.Bit32;
        }
    }
    
    /// <summary>
    /// 解析单个堆栈地址
    /// </summary>
    public static async Task<ParseResult> ResolveStackAsync(
        StackInfo stackInfo,
        ArchitectureType architectureType,
        string? resolver32BitPath,
        string? resolver64BitPath,
        string symbolFilePath)
    {
        var result = new ParseResult
        {
            OriginalLine = stackInfo.FullLine
        };
        
        try
        {
            // 确定使用的解析器路径
            string? resolverPath = null;
            ArchitectureType actualArch = architectureType;
            
            if (architectureType == ArchitectureType.Auto)
            {
                actualArch = DetectArchitecture(stackInfo.Address);
            }
            
            resolverPath = actualArch == ArchitectureType.Bit32 ? resolver32BitPath : resolver64BitPath;
            
            if (string.IsNullOrEmpty(resolverPath) || !System.IO.File.Exists(resolverPath))
            {
                result.Success = false;
                result.ErrorMessage = $"解析器路径不存在: {resolverPath}";
                return result;
            }
            
            if (string.IsNullOrEmpty(symbolFilePath) || !System.IO.File.Exists(symbolFilePath))
            {
                result.Success = false;
                result.ErrorMessage = $"符号文件不存在: {symbolFilePath}";
                return result;
            }
            
            // 调用解析器
            var processInfo = new ProcessStartInfo
            {
                FileName = resolverPath,
                Arguments = $"-f -e \"{symbolFilePath}\" -C \"{stackInfo.Address}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = new Process { StartInfo = processInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };
            
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();
            
            string output = outputBuilder.ToString().Trim();
            string error = errorBuilder.ToString().Trim();
            
            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                result.Success = true;
                result.SymbolizedLine = $"{stackInfo.Address} -- {output}";
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = string.IsNullOrEmpty(error) ? "解析失败" : error;
                result.SymbolizedLine = $"{stackInfo.Address} -- [解析失败: {result.ErrorMessage}]";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.SymbolizedLine = $"{stackInfo.Address} -- [错误: {ex.Message}]";
        }
        
        return result;
    }
    
    /// <summary>
    /// 批量解析堆栈
    /// </summary>
    public static async Task<List<ParseResult>> ResolveStacksAsync(
        List<StackInfo> stacks,
        ArchitectureType architectureType,
        string? resolver32BitPath,
        string? resolver64BitPath,
        string symbolFilePath)
    {
        var results = new List<ParseResult>();
        
        foreach (var stack in stacks)
        {
            var result = await ResolveStackAsync(
                stack,
                architectureType,
                resolver32BitPath,
                resolver64BitPath,
                symbolFilePath);
            results.Add(result);
        }
        
        return results;
    }
}

