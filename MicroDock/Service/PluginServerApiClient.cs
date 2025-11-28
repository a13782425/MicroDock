using MicroDock.Database;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MicroDock.Service;

/// <summary>
/// PluginServer API 客户端，统一管理所有与服务器的 HTTP 通信
/// </summary>
public class PluginServerApiClient
{
    private static readonly HttpClient _httpClient;
    private const string LOG_TAG = "PluginServerApiClient";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    static PluginServerApiClient()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // 上传大文件可能需要较长时间
    }

    #region API 响应模型

    /// <summary>
    /// 通用 API 响应
    /// </summary>
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    /// <summary>
    /// 远程插件信息
    /// </summary>
    public class RemotePluginInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("license")]
        public string? License { get; set; }

        [JsonPropertyName("homepage")]
        public string? Homepage { get; set; }

        [JsonPropertyName("main_dll")]
        public string? MainDll { get; set; }

        [JsonPropertyName("entry_class")]
        public string? EntryClass { get; set; }

        [JsonPropertyName("current_version")]
        public string CurrentVersion { get; set; } = string.Empty;

        [JsonPropertyName("is_enabled")]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("is_deprecated")]
        public bool IsDeprecated { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }
    }

    /// <summary>
    /// 插件详情响应数据
    /// </summary>
    public class PluginDetailData
    {
        [JsonPropertyName("plugin")]
        public RemotePluginInfo? Plugin { get; set; }

        [JsonPropertyName("versions")]
        public List<RemotePluginVersion>? Versions { get; set; }
    }

    /// <summary>
    /// 远程插件版本信息
    /// </summary>
    public class RemotePluginVersion
    {
        [JsonPropertyName("plugin_name")]
        public string PluginName { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        [JsonPropertyName("file_size")]
        public long FileSize { get; set; }

        [JsonPropertyName("file_hash")]
        public string? FileHash { get; set; }

        [JsonPropertyName("changelog")]
        public string? Changelog { get; set; }

        [JsonPropertyName("dependencies")]
        public Dictionary<string, string>? Dependencies { get; set; }

        [JsonPropertyName("engines")]
        public Dictionary<string, string>? Engines { get; set; }

        [JsonPropertyName("is_deprecated")]
        public bool IsDeprecated { get; set; }

        [JsonPropertyName("download_count")]
        public int DownloadCount { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
    }

    /// <summary>
    /// 备份信息
    /// </summary>
    public class BackupInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("user_key")]
        public string UserKey { get; set; } = string.Empty;

        [JsonPropertyName("backup_type")]
        public string BackupType { get; set; } = string.Empty;

        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }

        [JsonPropertyName("file_size")]
        public long FileSize { get; set; }

        [JsonPropertyName("file_hash")]
        public string? FileHash { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
    }

    /// <summary>
    /// 备份列表响应数据
    /// </summary>
    public class BackupListData
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("backups")]
        public List<BackupInfo>? Backups { get; set; }
    }

    /// <summary>
    /// 健康检查响应数据
    /// </summary>
    public class HealthData
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("database")]
        public string Database { get; set; } = string.Empty;
    }

    #endregion

    #region 基础 HTTP 方法

    /// <summary>
    /// 获取服务器地址
    /// </summary>
    private static string GetServerAddress()
    {
        return DBContext.GetSetting().ServerAddress ?? string.Empty;
    }

    /// <summary>
    /// 获取用户密钥（使用备份密码）
    /// </summary>
    private static string GetUserKey()
    {
        return DBContext.GetSetting().BackupPassword ?? string.Empty;
    }

    /// <summary>
    /// 构建完整 URL
    /// </summary>
    private static string BuildUrl(string endpoint)
    {
        string serverAddress = GetServerAddress();
        return $"{serverAddress.TrimEnd('/')}{endpoint}";
    }

    /// <summary>
    /// 发送 GET 请求
    /// </summary>
    private static async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            string url = BuildUrl(endpoint);
            Log.Debug("[{Tag}] GET {Url}", LOG_TAG, url);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions)
                    ?? new ApiResponse<T> { Success = false, Message = "响应解析失败" };
            }
            else
            {
                Log.Warning("[{Tag}] GET 请求失败: {StatusCode} - {Content}", LOG_TAG, response.StatusCode, content);
                return new ApiResponse<T> { Success = false, Message = $"服务器返回错误: {response.StatusCode}" };
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "[{Tag}] 网络请求失败", LOG_TAG);
            return new ApiResponse<T> { Success = false, Message = $"网络连接失败: {ex.Message}" };
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse<T> { Success = false, Message = "请求超时" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 请求异常", LOG_TAG);
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// 发送 GET 请求（直接返回数组，不是 ApiResponse 包装）
    /// </summary>
    private static async Task<ApiResponse<List<T>>> GetListDirectAsync<T>(string endpoint)
    {
        try
        {
            string url = BuildUrl(endpoint);
            Log.Debug("[{Tag}] GET (Direct List) {Url}", LOG_TAG, url);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var list = JsonSerializer.Deserialize<List<T>>(content, _jsonOptions);
                return new ApiResponse<List<T>>
                {
                    Success = true,
                    Message = "获取成功",
                    Data = list ?? new List<T>()
                };
            }
            else
            {
                Log.Warning("[{Tag}] GET 请求失败: {StatusCode} - {Content}", LOG_TAG, response.StatusCode, content);
                return new ApiResponse<List<T>> { Success = false, Message = $"服务器返回错误: {response.StatusCode}" };
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "[{Tag}] 网络请求失败", LOG_TAG);
            return new ApiResponse<List<T>> { Success = false, Message = $"网络连接失败: {ex.Message}" };
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse<List<T>> { Success = false, Message = "请求超时" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 请求异常", LOG_TAG);
            return new ApiResponse<List<T>> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// 发送 POST 请求（直接返回数组，不是 ApiResponse 包装）
    /// </summary>
    private static async Task<ApiResponse<List<T>>> PostListDirectAsync<T>(string endpoint, object body)
    {
        try
        {
            string url = BuildUrl(endpoint);
            string json = JsonSerializer.Serialize(body, _jsonOptions);
            Log.Debug("[{Tag}] POST (Direct List) {Url} Body: {Body}", LOG_TAG, url, json);

            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, httpContent);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var list = JsonSerializer.Deserialize<List<T>>(content, _jsonOptions);
                return new ApiResponse<List<T>>
                {
                    Success = true,
                    Message = "获取成功",
                    Data = list ?? new List<T>()
                };
            }
            else
            {
                Log.Warning("[{Tag}] POST 请求失败: {StatusCode} - {Content}", LOG_TAG, response.StatusCode, content);
                return new ApiResponse<List<T>> { Success = false, Message = $"服务器返回错误: {response.StatusCode}" };
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "[{Tag}] 网络请求失败", LOG_TAG);
            return new ApiResponse<List<T>> { Success = false, Message = $"网络连接失败: {ex.Message}" };
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse<List<T>> { Success = false, Message = "请求超时" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 请求异常", LOG_TAG);
            return new ApiResponse<List<T>> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// 发送 POST 请求（直接返回对象，不是 ApiResponse 包装）
    /// </summary>
    private static async Task<ApiResponse<T>> PostDirectAsync<T>(string endpoint, object body)
    {
        try
        {
            string url = BuildUrl(endpoint);
            string json = JsonSerializer.Serialize(body, _jsonOptions);
            Log.Debug("[{Tag}] POST (Direct) {Url} Body: {Body}", LOG_TAG, url, json);

            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, httpContent);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return new ApiResponse<T>
                {
                    Success = true,
                    Message = "获取成功",
                    Data = data
                };
            }
            else
            {
                Log.Warning("[{Tag}] POST 请求失败: {StatusCode} - {Content}", LOG_TAG, response.StatusCode, content);
                return new ApiResponse<T> { Success = false, Message = $"服务器返回错误: {response.StatusCode}" };
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "[{Tag}] 网络请求失败", LOG_TAG);
            return new ApiResponse<T> { Success = false, Message = $"网络连接失败: {ex.Message}" };
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse<T> { Success = false, Message = "请求超时" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 请求异常", LOG_TAG);
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// 发送 POST 请求（JSON 体，期望 ApiResponse 包装响应）
    /// </summary>
    private static async Task<ApiResponse<T>> PostJsonAsync<T>(string endpoint, object body)
    {
        try
        {
            string url = BuildUrl(endpoint);
            string json = JsonSerializer.Serialize(body, _jsonOptions);
            Log.Debug("[{Tag}] POST {Url} Body: {Body}", LOG_TAG, url, json);

            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, httpContent);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions)
                    ?? new ApiResponse<T> { Success = false, Message = "响应解析失败" };
            }
            else
            {
                Log.Warning("[{Tag}] POST 请求失败: {StatusCode} - {Content}", LOG_TAG, response.StatusCode, content);
                return new ApiResponse<T> { Success = false, Message = $"服务器返回错误: {response.StatusCode}" };
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "[{Tag}] 网络请求失败", LOG_TAG);
            return new ApiResponse<T> { Success = false, Message = $"网络连接失败: {ex.Message}" };
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse<T> { Success = false, Message = "请求超时" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 请求异常", LOG_TAG);
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// 发送 POST 请求下载文件
    /// </summary>
    private static async Task<(bool success, string message, byte[]? data)> PostDownloadAsync(string endpoint, object body)
    {
        try
        {
            string url = BuildUrl(endpoint);
            string json = JsonSerializer.Serialize(body, _jsonOptions);
            Log.Debug("[{Tag}] POST Download {Url}", LOG_TAG, url);

            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, httpContent);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsByteArrayAsync();
                return (true, "下载成功", data);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, "资源不存在", null);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Log.Warning("[{Tag}] 下载失败: {StatusCode} - {Content}", LOG_TAG, response.StatusCode, content);
                return (false, $"服务器返回错误: {response.StatusCode}", null);
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "[{Tag}] 网络请求失败", LOG_TAG);
            return (false, $"网络连接失败: {ex.Message}", null);
        }
        catch (TaskCanceledException)
        {
            return (false, "请求超时", null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 请求异常", LOG_TAG);
            return (false, ex.Message, null);
        }
    }

    #endregion

    #region 系统 API

    /// <summary>
    /// 测试服务器连接
    /// </summary>
    public static async Task<(bool success, string message)> TestConnectionAsync(string? serverAddress = null)
    {
        try
        {
            string address = serverAddress ?? GetServerAddress();
            if (string.IsNullOrEmpty(address))
            {
                return (false, "服务器地址不能为空");
            }

            string url = $"{address.TrimEnd('/')}/api/health";
            Log.Debug("[{Tag}] 测试连接: {Url}", LOG_TAG, url);

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return (true, "连接成功");
            }
            else
            {
                return (false, $"服务器返回: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            return (false, $"连接失败: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return (false, "连接超时");
        }
    }

    /// <summary>
    /// 获取服务器健康状态
    /// </summary>
    public static async Task<ApiResponse<HealthData>> GetHealthAsync()
    {
        return await GetAsync<HealthData>("/api/health");
    }

    #endregion

    #region 插件 API

    /// <summary>
    /// 获取插件列表
    /// </summary>
    public static async Task<ApiResponse<List<RemotePluginInfo>>> GetPluginListAsync()
    {
        // 注意：后端直接返回数组，而非 ApiResponse 包装
        return await GetAsync<List<RemotePluginInfo>>("/api/plugins/list");
    }

    /// <summary>
    /// 获取插件详情
    /// </summary>
    public static async Task<ApiResponse<PluginDetailData>> GetPluginDetailAsync(string pluginName)
    {
        // 注意：后端直接返回对象，而非 ApiResponse 包装
        return await PostJsonAsync<PluginDetailData>("/api/plugins/detail", new { name = pluginName });
    }

    /// <summary>
    /// 获取插件版本列表
    /// </summary>
    public static async Task<ApiResponse<List<RemotePluginVersion>>> GetPluginVersionsAsync(string pluginName)
    {
        // 注意：后端直接返回数组，而非 ApiResponse 包装
        return await PostJsonAsync<List<RemotePluginVersion>>("/api/plugins/versions", new { name = pluginName });
    }

    /// <summary>
    /// 下载插件（当前版本）
    /// </summary>
    public static async Task<(bool success, string message, byte[]? data)> DownloadPluginAsync(string pluginName)
    {
        return await PostDownloadAsync("/api/plugins/download", new { name = pluginName });
    }

    /// <summary>
    /// 下载插件指定版本
    /// </summary>
    public static async Task<(bool success, string message, byte[]? data)> DownloadPluginVersionAsync(string pluginName, string version)
    {
        return await PostDownloadAsync("/api/plugins/version/download", new { name = pluginName, version });
    }

    /// <summary>
    /// 上传插件到服务器
    /// </summary>
    /// <param name="pluginFolderPath">插件文件夹路径</param>
    /// <param name="pluginKey">上传验证 Key</param>
    /// <returns>上传结果</returns>
    public static async Task<(bool success, string message)> UploadPluginAsync(string pluginFolderPath, string pluginKey)
    {
        string serverAddress = GetServerAddress();

        if (string.IsNullOrEmpty(serverAddress))
        {
            return (false, "请先在高级设置中配置服务器地址");
        }

        if (string.IsNullOrEmpty(pluginKey))
        {
            return (false, "上传验证 Key 不能为空");
        }

        if (!Directory.Exists(pluginFolderPath))
        {
            return (false, $"插件文件夹不存在: {pluginFolderPath}");
        }

        string? tempZipPath = null;

        try
        {
            // 获取插件名称（文件夹名）
            string pluginName = new DirectoryInfo(pluginFolderPath).Name;

            // 创建临时 ZIP 文件
            tempZipPath = Path.Combine(AppConfig.TEMP_BACKUP_FOLDER, $"plugin_upload_{pluginName}_{DateTime.Now:yyyyMMddHHmmss}.zip");

            Log.Information("[{Tag}] 开始压缩插件: {PluginName}", LOG_TAG, pluginName);

            // 压缩插件文件夹（排除 data 和 temp_data 目录）
            await CreatePluginZipAsync(pluginFolderPath, tempZipPath);

            // 上传到服务器
            var result = await UploadPluginZipAsync(tempZipPath, pluginKey, serverAddress);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 上传插件失败", LOG_TAG);
            return (false, $"上传失败: {ex.Message}");
        }
        finally
        {
            // 清理临时文件
            if (tempZipPath != null && File.Exists(tempZipPath))
            {
                try { File.Delete(tempZipPath); } catch { }
            }
        }
    }

    /// <summary>
    /// 创建插件 ZIP 包（排除用户数据目录）
    /// </summary>
    private static async Task CreatePluginZipAsync(string pluginFolderPath, string zipPath)
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
    /// 递归添加目录到 ZIP（排除 data、temp_data 目录）
    /// </summary>
    private static void AddDirectoryToZip(ZipArchive archive, DirectoryInfo directory, string rootPath)
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
    private static string GetRelativePath(string fullPath, string rootPath)
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
    /// 上传 ZIP 文件到服务器
    /// </summary>
    private static async Task<(bool success, string message)> UploadPluginZipAsync(string zipFilePath, string pluginKey, string serverAddress)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            // 添加文件
            var fileBytes = await File.ReadAllBytesAsync(zipFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "file", Path.GetFileName(zipFilePath));

            // 添加 plugin_key 作为表单参数（符合 API 规范）
            content.Add(new StringContent(pluginKey), "plugin_key");

            string url = $"{serverAddress.TrimEnd('/')}/api/plugins/upload";
            Log.Debug("[{Tag}] 上传插件到: {Url}", LOG_TAG, url);

            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Log.Information("[{Tag}] 插件上传成功", LOG_TAG);
                return (true, "上传成功");
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                Log.Warning("[{Tag}] 插件上传失败: {StatusCode} - {Error}", LOG_TAG, response.StatusCode, errorMsg);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return (false, "上传验证 Key 无效");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    return (false, "插件版本已存在");
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

    #endregion

    #region 备份 API

    /// <summary>
    /// 上传备份文件到服务器
    /// </summary>
    /// <param name="zipFilePath">ZIP 文件路径</param>
    /// <param name="backupType">备份类型：program 或 plugin</param>
    /// <param name="description">备份描述（可选）</param>
    /// <returns>上传结果</returns>
    public static async Task<(bool success, string message)> UploadBackupAsync(string pluginName, string zipFilePath, string backupType, string? description = null)
    {
        string serverAddress = GetServerAddress();
        string userKey = GetUserKey();

        if (string.IsNullOrEmpty(serverAddress))
        {
            return (false, "请先在高级设置中配置服务器地址");
        }

        if (string.IsNullOrEmpty(userKey))
        {
            return (false, "请先在高级设置中配置备份密码");
        }

        try
        {
            using var content = new MultipartFormDataContent();

            // 添加文件
            var fileBytes = await File.ReadAllBytesAsync(zipFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", Path.GetFileName(zipFilePath));

            // 添加参数（符合 API 规范）
            content.Add(new StringContent(userKey), "user_key");
            content.Add(new StringContent(backupType), "backup_type");
            content.Add(new StringContent(pluginName), "plugin_name");

            if (!string.IsNullOrEmpty(description))
            {
                content.Add(new StringContent(description), "description");
            }

            string url = $"{serverAddress.TrimEnd('/')}/api/backups/upload";
            Log.Debug("[{Tag}] 上传备份到: {Url}", LOG_TAG, url);

            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Log.Information("[{Tag}] 备份上传成功: {Type}", LOG_TAG, backupType);
                return (true, "备份成功");
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                Log.Warning("[{Tag}] 备份上传失败: {StatusCode} - {Error}", LOG_TAG, response.StatusCode, errorMsg);
                return (false, $"服务器返回错误: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "[{Tag}] 网络请求失败", LOG_TAG);
            return (false, $"网络连接失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取用户备份列表
    /// </summary>
    public static async Task<ApiResponse<BackupListData>> GetBackupListAsync()
    {
        string userKey = GetUserKey();

        if (string.IsNullOrEmpty(userKey))
        {
            return new ApiResponse<BackupListData> { Success = false, Message = "请先在高级设置中配置备份密码" };
        }

        return await PostJsonAsync<BackupListData>("/api/backups/list", new { user_key = userKey });
    }

    /// <summary>
    /// 下载备份文件
    /// </summary>
    /// <param name="backupId">备份 ID</param>
    /// <returns>下载结果和文件数据</returns>
    public static async Task<(bool success, string message, byte[]? data)> DownloadBackupAsync(int backupId)
    {
        string userKey = GetUserKey();

        if (string.IsNullOrEmpty(userKey))
        {
            return (false, "请先在高级设置中配置备份密码", null);
        }

        return await PostDownloadAsync("/api/backups/download", new { user_key = userKey, id = backupId });
    }

    #endregion

    #region 高级备份方法（本地文件操作 + 网络）

    /// <summary>
    /// 备份插件数据到服务器
    /// </summary>
    /// <param name="pluginName">插件唯一名称</param>
    /// <param name="dataFolderPath">插件 data 文件夹路径</param>
    public static async Task<(bool success, string message)> BackupPluginDataAsync(string pluginName, string dataFolderPath)
    {
        if (!Directory.Exists(dataFolderPath))
        {
            return (false, $"插件数据文件夹不存在: {dataFolderPath}");
        }

        string? tempZipPath = null;

        try
        {
            // 创建临时 ZIP 文件
            tempZipPath = Path.Combine(AppConfig.TEMP_BACKUP_FOLDER, $"plugin_backup_{pluginName}_{DateTime.Now:yyyyMMddHHmmss}.zip");

            Log.Information("[{Tag}] 开始压缩插件数据: {PluginName}", LOG_TAG, pluginName);

            // 压缩 data 文件夹
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }
            ZipFile.CreateFromDirectory(dataFolderPath, tempZipPath, CompressionLevel.Optimal, false);

            // 上传到服务器
            var result = await UploadBackupAsync(pluginName, tempZipPath, "plugin", $"插件备份: {pluginName}");

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 备份插件数据失败: {PluginName}", LOG_TAG, pluginName);
            return (false, $"备份失败: {ex.Message}");
        }
        finally
        {
            // 清理临时文件
            if (tempZipPath != null && File.Exists(tempZipPath))
            {
                try { File.Delete(tempZipPath); } catch { }
            }
        }
    }

    /// <summary>
    /// 从服务器恢复插件数据
    /// </summary>
    /// <param name="pluginName">插件唯一名称</param>
    /// <param name="dataFolderPath">插件 data 文件夹路径</param>
    public static async Task<(bool success, string message)> RestorePluginDataAsync(string pluginName, string dataFolderPath)
    {
        // 首先获取备份列表，找到该插件的最新备份
        var listResponse = await GetBackupListAsync();
        if (!listResponse.Success || listResponse.Data?.Backups == null)
        {
            return (false, listResponse.Message ?? "获取备份列表失败");
        }

        // 查找该插件的最新备份（按创建时间倒序）
        var pluginBackup = listResponse.Data.Backups
            .Where(b => b.BackupType == "plugin" && (b.Description?.Contains(pluginName) ?? false))
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefault();

        if (pluginBackup == null)
        {
            return (false, "服务器上没有找到该插件的备份数据");
        }

        string? tempZipPath = null;
        string? backupPath = null;

        try
        {
            // 下载备份文件
            tempZipPath = Path.Combine(AppConfig.TEMP_BACKUP_FOLDER, $"plugin_restore_{pluginName}_{DateTime.Now:yyyyMMddHHmmss}.zip");

            var (downloadSuccess, downloadMessage, data) = await DownloadBackupAsync(pluginBackup.Id);
            if (!downloadSuccess || data == null)
            {
                return (false, downloadMessage);
            }

            await File.WriteAllBytesAsync(tempZipPath, data);

            Log.Information("[{Tag}] 开始恢复插件数据: {PluginName}", LOG_TAG, pluginName);

            // 备份当前数据（以防恢复失败）
            backupPath = dataFolderPath + "_backup_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            if (Directory.Exists(dataFolderPath))
            {
                Directory.Move(dataFolderPath, backupPath);
            }

            try
            {
                // 解压到 data 文件夹
                Directory.CreateDirectory(dataFolderPath);
                ZipFile.ExtractToDirectory(tempZipPath, dataFolderPath, true);

                // 删除临时备份
                if (Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                }

                Log.Information("[{Tag}] 插件数据恢复成功: {PluginName}", LOG_TAG, pluginName);
                return (true, "恢复成功");
            }
            catch
            {
                // 恢复失败，还原原数据
                if (Directory.Exists(dataFolderPath))
                {
                    Directory.Delete(dataFolderPath, true);
                }
                if (Directory.Exists(backupPath))
                {
                    Directory.Move(backupPath, dataFolderPath);
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 恢复插件数据失败: {PluginName}", LOG_TAG, pluginName);
            return (false, $"恢复失败: {ex.Message}");
        }
        finally
        {
            // 清理临时文件
            if (tempZipPath != null && File.Exists(tempZipPath))
            {
                try { File.Delete(tempZipPath); } catch { }
            }
        }
    }

    /// <summary>
    /// 备份主程序数据库到服务器
    /// </summary>
    public static async Task<(bool success, string message)> BackupAppDataAsync()
    {
        string dbPath = Path.Combine(AppConfig.CONFIG_FOLDER, "microdock");
        if (!File.Exists(dbPath))
        {
            return (false, "数据库文件不存在");
        }

        string? tempZipPath = null;
        string? tempDbPath = null;

        try
        {
            // 创建临时文件路径
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            tempZipPath = Path.Combine(AppConfig.TEMP_BACKUP_FOLDER, $"app_backup_{timestamp}.zip");
            tempDbPath = Path.Combine(AppConfig.TEMP_BACKUP_FOLDER, $"microdock_temp_{timestamp}");

            Log.Information("[{Tag}] 开始备份主程序数据", LOG_TAG);

            // 使用共享读取模式复制数据库文件到临时位置（避免文件锁定冲突）
            using (var sourceStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var destStream = new FileStream(tempDbPath, FileMode.Create, FileAccess.Write))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            // 压缩临时数据库文件
            using (var archive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(tempDbPath, "microdock", CompressionLevel.Optimal);
            }

            // 上传到服务器
            var result = await UploadBackupAsync("", tempZipPath, "program", "主程序数据库备份");

            if (result.success)
            {
                // 更新备份时间
                DBContext.UpdateSetting(s => s.LastAppBackupTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }

            return result;
        }
        catch (Exception ex)
        {
            LogError("备份主程序数据失败", LOG_TAG, ex);
            return (false, $"备份失败: {ex.Message}");
        }
        finally
        {
            // 清理临时文件
            if (tempZipPath != null && File.Exists(tempZipPath))
            {
                try { File.Delete(tempZipPath); } catch { }
            }
            if (tempDbPath != null && File.Exists(tempDbPath))
            {
                try { File.Delete(tempDbPath); } catch { }
            }
        }
    }

    /// <summary>
    /// 待恢复数据库文件路径
    /// </summary>
    private static string PendingRestoreDbPath => Path.Combine(AppConfig.TEMP_BACKUP_FOLDER, "pending_restore_microdock");

    /// <summary>
    /// 从服务器恢复主程序数据库
    /// 由于数据库文件在运行时被占用，恢复操作会保存到临时位置，重启后生效
    /// </summary>
    public static async Task<(bool success, string message)> RestoreAppDataAsync()
    {
        // 首先获取备份列表，找到主程序的最新备份
        var listResponse = await GetBackupListAsync();
        if (!listResponse.Success || listResponse.Data?.Backups == null)
        {
            return (false, listResponse.Message ?? "获取备份列表失败");
        }

        // 查找主程序的最新备份
        var appBackup = listResponse.Data.Backups
            .Where(b => b.BackupType == "program")
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefault();

        if (appBackup == null)
        {
            return (false, "服务器上没有找到主程序备份数据");
        }

        string? tempZipPath = null;

        try
        {
            // 下载备份文件
            tempZipPath = Path.Combine(AppConfig.TEMP_BACKUP_FOLDER, $"app_restore_{DateTime.Now:yyyyMMddHHmmss}.zip");

            (bool downloadSuccess, string downloadMessage, byte[] data) = await DownloadBackupAsync(appBackup.Id);
            if (!downloadSuccess || data == null)
            {
                return (false, downloadMessage);
            }

            await File.WriteAllBytesAsync(tempZipPath, data);

            Log.Information("[{Tag}] 开始准备恢复主程序数据", LOG_TAG);

            // 解压到待恢复位置（重启后生效）
            using (var archive = ZipFile.OpenRead(tempZipPath))
            {
                var entry = archive.GetEntry("microdock");
                if (entry == null)
                {
                    return (false, "备份文件格式错误");
                }
                // 解压到待恢复位置
                entry.ExtractToFile(PendingRestoreDbPath, true);
            }

            Log.Information("[{Tag}] 主程序数据已准备恢复，重启后生效", LOG_TAG);
            return (true, "恢复数据已准备就绪");
        }
        catch (Exception ex)
        {
            LogError("恢复主程序数据失败", LOG_TAG, ex);
            return (false, $"恢复失败: {ex.Message}");
        }
        finally
        {
            // 清理临时文件
            if (tempZipPath != null && File.Exists(tempZipPath))
            {
                try { File.Delete(tempZipPath); } catch { }
            }
        }
    }

    /// <summary>
    /// 检查并执行待恢复的数据库（应在程序启动时调用，数据库初始化之前）
    /// </summary>
    public static void ApplyPendingRestore()
    {
        try
        {
            if (!File.Exists(PendingRestoreDbPath))
            {
                return;
            }

            string dbPath = Path.Combine(AppConfig.CONFIG_FOLDER, "microdock");
            string backupPath = dbPath + "_backup_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            Log.Information("[{Tag}] 检测到待恢复的数据库，开始恢复", LOG_TAG);

            // 备份当前数据库
            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, backupPath, true);
            }

            try
            {
                // 用待恢复文件替换当前数据库
                File.Copy(PendingRestoreDbPath, dbPath, true);

                // 删除待恢复文件
                File.Delete(PendingRestoreDbPath);

                // 删除旧备份
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                Log.Information("[{Tag}] 主程序数据恢复成功", LOG_TAG);
            }
            catch
            {
                // 恢复失败，还原原数据
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, dbPath, true);
                    File.Delete(backupPath);
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            LogError("应用待恢复数据失败", LOG_TAG, ex);
        }
    }

    #endregion
}

