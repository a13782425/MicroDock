using MicroDock.Database;
using Serilog;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MicroDock.Service;

/// <summary>
/// 插件上传服务，提供将插件上传到服务器的功能
/// </summary>
public class PluginUploadService
{
    private readonly HttpClient _httpClient;
    private const string LOG_TAG = "PluginUploadService";

    public PluginUploadService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // 上传大插件可能需要较长时间
    }

    /// <summary>
    /// 上传插件到服务器
    /// </summary>
    /// <param name="pluginName">插件唯一名称</param>
    /// <param name="pluginFolderPath">插件文件夹路径</param>
    /// <param name="uploadKey">上传验证Key</param>
    /// <returns>上传结果</returns>
    public async Task<(bool success, string message)> UploadPluginAsync(
        string pluginName, 
        string pluginFolderPath, 
        string uploadKey)
    {
        var settings = DBContext.GetSetting();
        
        if (string.IsNullOrEmpty(settings.ServerAddress))
        {
            return (false, "请先在高级设置中配置服务器地址");
        }

        if (string.IsNullOrEmpty(uploadKey))
        {
            return (false, "上传验证Key不能为空");
        }

        if (!Directory.Exists(pluginFolderPath))
        {
            return (false, $"插件文件夹不存在: {pluginFolderPath}");
        }

        try
        {
            // 创建临时ZIP文件
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"plugin_upload_{pluginName}_{DateTime.Now:yyyyMMddHHmmss}.zip");
            
            Log.Information("[{Tag}] 开始压缩插件: {PluginName}", LOG_TAG, pluginName);

            // 压缩整个插件文件夹（排除data和temp_data目录，因为那是用户数据）
            await CreatePluginZipAsync(pluginFolderPath, tempZipPath);

            // 上传到服务器
            var result = await UploadToServerAsync(tempZipPath, pluginName, uploadKey, settings.ServerAddress);

            // 清理临时文件
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 上传插件失败: {PluginName}", LOG_TAG, pluginName);
            return (false, $"上传失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建插件ZIP包（排除用户数据目录）
    /// </summary>
    private async Task CreatePluginZipAsync(string pluginFolderPath, string zipPath)
    {
        await Task.Run(() =>
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            
            var pluginDir = new DirectoryInfo(pluginFolderPath);
            AddDirectoryToZip(archive, pluginDir, pluginDir.FullName);
        });
    }

    /// <summary>
    /// 递归添加目录到ZIP（排除data、temp_data目录）
    /// </summary>
    private void AddDirectoryToZip(ZipArchive archive, DirectoryInfo directory, string rootPath)
    {
        // 排除的目录名
        string[] excludedDirs = { "data", "temp_data" };

        foreach (var file in directory.GetFiles())
        {
            string entryName = GetRelativePath(file.FullName, rootPath);
            archive.CreateEntryFromFile(file.FullName, entryName, CompressionLevel.Optimal);
        }

        foreach (var subDir in directory.GetDirectories())
        {
            // 跳过排除的目录
            if (Array.Exists(excludedDirs, d => d.Equals(subDir.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Log.Debug("[{Tag}] 跳过目录: {Dir}", LOG_TAG, subDir.Name);
                continue;
            }

            AddDirectoryToZip(archive, subDir, rootPath);
        }
    }

    /// <summary>
    /// 获取相对路径
    /// </summary>
    private string GetRelativePath(string fullPath, string rootPath)
    {
        if (!rootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            rootPath += Path.DirectorySeparatorChar;
        }
        
        if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(rootPath.Length);
        }
        
        return fullPath;
    }

    /// <summary>
    /// 上传ZIP文件到服务器
    /// </summary>
    private async Task<(bool success, string message)> UploadToServerAsync(
        string zipFilePath,
        string pluginName,
        string uploadKey,
        string serverAddress)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            
            // 添加文件
            var fileBytes = await File.ReadAllBytesAsync(zipFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "file", $"{pluginName}.zip");
            
            // 添加插件名称
            content.Add(new StringContent(pluginName), "pluginName");

            // 设置上传Key头
            _httpClient.DefaultRequestHeaders.Remove("X-Upload-Key");
            _httpClient.DefaultRequestHeaders.Add("X-Upload-Key", uploadKey);

            var url = $"{serverAddress.TrimEnd('/')}/api/plugins/upload";
            Log.Debug("[{Tag}] 上传插件到: {Url}", LOG_TAG, url);

            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                Log.Information("[{Tag}] 插件上传成功: {PluginName}", LOG_TAG, pluginName);
                return (true, "上传成功");
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                Log.Warning("[{Tag}] 插件上传失败: {StatusCode} - {Error}", LOG_TAG, response.StatusCode, errorMsg);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return (false, "上传验证Key无效");
                }
                
                return (false, $"服务器返回错误: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "[{Tag}] 网络请求失败", LOG_TAG);
            return (false, $"网络连接失败: {ex.Message}");
        }
    }
}

