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
    /// 解析单个堆栈地址（使用符号文件夹）
    /// </summary>
    public static async Task<ParseResult> ResolveStackAsync(
        StackInfo stackInfo,
        ArchitectureType architectureType,
        string? resolver32BitPath,
        string? resolver64BitPath,
        SymbolFileScanner.ScanResult symbolScanResult)
    {
        var result = new ParseResult
        {
            OriginalLine = stackInfo.FullLine
        };
        
        try
        {
            // 确定使用的架构类型
            ArchitectureType actualArch = architectureType;
            if (architectureType == ArchitectureType.Auto)
            {
                actualArch = DetectArchitecture(stackInfo.Address);
            }
            
            // 确定使用的解析器路径
            string? resolverPath = actualArch == ArchitectureType.Bit32 ? resolver32BitPath : resolver64BitPath;
            
            if (string.IsNullOrEmpty(resolverPath) || !System.IO.File.Exists(resolverPath))
            {
                result.Success = false;
                result.ErrorMessage = $"解析器路径不存在: {resolverPath}";
                return result;
            }
            
            // 从堆栈信息中提取 .so 文件名（不含路径）
            string soFileName = System.IO.Path.GetFileName(stackInfo.SoFile);
            
            // 特殊处理：如果是 libil2cpp.so，使用 libil2cpp.sym.so
            string symbolFileName = soFileName;
            if (soFileName.Equals("libil2cpp.so", StringComparison.OrdinalIgnoreCase))
            {
                symbolFileName = "libil2cpp.sym.so";
            }
            
            // 根据架构类型查找对应的符号文件
            string? symbolFilePath = null;
            
            if (actualArch == ArchitectureType.Bit32)
            {
                // 优先查找32位符号文件
                if (symbolScanResult.Symbol32BitMap.TryGetValue(symbolFileName, out symbolFilePath))
                {
                    // 找到32位符号文件
                }
                else if (symbolScanResult.Symbol64BitMap.TryGetValue(symbolFileName, out symbolFilePath))
                {
                    // 降级：使用64位符号文件
                }
            }
            else // Bit64
            {
                // 优先查找64位符号文件
                if (symbolScanResult.Symbol64BitMap.TryGetValue(symbolFileName, out symbolFilePath))
                {
                    // 找到64位符号文件
                }
                else if (symbolScanResult.Symbol32BitMap.TryGetValue(symbolFileName, out symbolFilePath))
                {
                    // 降级：使用32位符号文件
                }
            }
            
            if (string.IsNullOrEmpty(symbolFilePath) || !System.IO.File.Exists(symbolFilePath))
            {
                result.Success = false;
                result.ErrorMessage = $"未找到符号文件: {symbolFileName}";
                result.SymbolizedLine = $"{stackInfo.Address} -- [未找到符号文件: {symbolFileName}]";
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
    /// 批量解析堆栈（使用符号文件夹）
    /// </summary>
    public static async Task<List<ParseResult>> ResolveStacksAsync(
        List<StackInfo> stacks,
        ArchitectureType architectureType,
        string? resolver32BitPath,
        string? resolver64BitPath,
        SymbolFileScanner.ScanResult symbolScanResult)
    {
        var results = new List<ParseResult>();
        
        foreach (var stack in stacks)
        {
            var result = await ResolveStackAsync(
                stack,
                architectureType,
                resolver32BitPath,
                resolver64BitPath,
                symbolScanResult);
            results.Add(result);
        }
        
        return results;
    }
}

/// <summary>
/// 符号文件扫描器 - 扫描符号文件夹中的 .so 文件
/// </summary>
public class SymbolFileScanner
{
    /// <summary>
    /// 符号文件扫描结果
    /// </summary>
    public class ScanResult
    {
        /// <summary>
        /// 32位符号文件映射 (文件名 -> 完整路径)
        /// </summary>
        public Dictionary<string, string> Symbol32BitMap { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// 64位符号文件映射 (文件名 -> 完整路径)
        /// </summary>
        public Dictionary<string, string> Symbol64BitMap { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// 未知架构的符号文件列表
        /// </summary>
        public List<string> UnknownArchFiles { get; set; } = new List<string>();
        
        /// <summary>
        /// 32位符号文件总数
        /// </summary>
        public int Count32Bit => Symbol32BitMap.Count;
        
        /// <summary>
        /// 64位符号文件总数
        /// </summary>
        public int Count64Bit => Symbol64BitMap.Count;
        
        /// <summary>
        /// 未知架构文件总数
        /// </summary>
        public int CountUnknown => UnknownArchFiles.Count;
        
        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalCount => Count32Bit + Count64Bit + CountUnknown;
    }
    
    /// <summary>
    /// 扫描符号文件夹
    /// </summary>
    /// <param name="folderPath">符号文件夹路径</param>
    /// <returns>扫描结果</returns>
    public static ScanResult ScanSymbolFolder(string folderPath)
    {
        var result = new ScanResult();
        
        if (string.IsNullOrEmpty(folderPath) || !System.IO.Directory.Exists(folderPath))
        {
            return result;
        }
        
        try
        {
            // 使用 EnumerationOptions 优化性能
            var options = new System.IO.EnumerationOptions
            {
                RecurseSubdirectories = true,
                MatchCasing = System.IO.MatchCasing.CaseInsensitive,
                AttributesToSkip = System.IO.FileAttributes.System | System.IO.FileAttributes.Hidden
            };
            
            // 递归搜索所有 .so 文件
            var soFiles = System.IO.Directory.GetFiles(folderPath, "*.so", options);
            
            foreach (var soFilePath in soFiles)
            {
                // 获取文件名（不含路径）
                string fileName = System.IO.Path.GetFileName(soFilePath);
                
                // 判断架构位数（根据父文件夹名称）
                int archBits = DetectArchitectureFromPath(soFilePath);
                
                if (archBits == 32)
                {
                    // 32位：如果已存在同名文件，保留第一个找到的
                    if (!result.Symbol32BitMap.ContainsKey(fileName))
                    {
                        result.Symbol32BitMap[fileName] = soFilePath;
                    }
                }
                else if (archBits == 64)
                {
                    // 64位：如果已存在同名文件，保留第一个找到的
                    if (!result.Symbol64BitMap.ContainsKey(fileName))
                    {
                        result.Symbol64BitMap[fileName] = soFilePath;
                    }
                }
                else
                {
                    // 未知架构
                    result.UnknownArchFiles.Add(soFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            // 扫描出错，返回空结果
            System.Diagnostics.Debug.WriteLine($"符号文件夹扫描失败: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 根据文件路径检测架构位数
    /// </summary>
    /// <param name="soFilePath">so文件完整路径</param>
    /// <returns>32 或 64，未知返回 0</returns>
    private static int DetectArchitectureFromPath(string soFilePath)
    {
        try
        {
            // 获取父文件夹名称
            string? parentDir = System.IO.Path.GetDirectoryName(soFilePath);
            if (string.IsNullOrEmpty(parentDir))
            {
                return 0;
            }
            
            string parentFolderName = System.IO.Path.GetFileName(parentDir);
            
            // 根据标准 Android NDK 架构文件夹名称判断
            if (parentFolderName.Equals("armeabi-v7a", StringComparison.OrdinalIgnoreCase) ||
                parentFolderName.Equals("armeabi", StringComparison.OrdinalIgnoreCase))
            {
                return 32;
            }
            else if (parentFolderName.Equals("arm64-v8a", StringComparison.OrdinalIgnoreCase) ||
                     parentFolderName.Equals("arm64", StringComparison.OrdinalIgnoreCase))
            {
                return 64;
            }
            
            // 未知架构
            return 0;
        }
        catch
        {
            return 0;
        }
    }
    
    /// <summary>
    /// 异步扫描符号文件夹
    /// </summary>
    /// <param name="folderPath">符号文件夹路径</param>
    /// <returns>扫描结果</returns>
    public static Task<ScanResult> ScanSymbolFolderAsync(string folderPath)
    {
        return Task.Run(() => ScanSymbolFolder(folderPath));
    }
}

