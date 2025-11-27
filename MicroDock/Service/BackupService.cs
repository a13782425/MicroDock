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
/// 备份服务，提供插件数据和主程序数据的备份与恢复功能
/// </summary>
public class BackupService
{
    private readonly HttpClient _httpClient;
    private const string LOG_TAG = "BackupService";

    public BackupService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // 上传大文件可能需要较长时间
    }

    #region 插件数据备份

    /// <summary>
    /// 备份插件数据到服务器
    /// </summary>
    /// <param name="pluginName">插件唯一名称</param>
    /// <param name="dataFolderPath">插件data文件夹路径</param>
    /// <returns>成功返回true</returns>
    public async Task<(bool success, string message)> BackupPluginDataAsync(string pluginName, string dataFolderPath)
    {
        var settings = DBContext.GetSetting();
        
        if (string.IsNullOrEmpty(settings.ServerAddress))
        {
            return (false, "请先在高级设置中配置服务器地址");
        }

        if (string.IsNullOrEmpty(settings.BackupPassword))
        {
            return (false, "请先在高级设置中配置备份密码");
        }

        if (!Directory.Exists(dataFolderPath))
        {
            return (false, $"插件数据文件夹不存在: {dataFolderPath}");
        }

        try
        {
            // 创建临时ZIP文件
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"plugin_backup_{pluginName}_{DateTime.Now:yyyyMMddHHmmss}.zip");
            
            Log.Information("[{Tag}] 开始压缩插件数据: {PluginName}", LOG_TAG, pluginName);
            
            // 压缩data文件夹
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }
            ZipFile.CreateFromDirectory(dataFolderPath, tempZipPath, CompressionLevel.Optimal, false);

            // 上传到服务器
            var result = await UploadBackupAsync(tempZipPath, "plugin", pluginName, settings.ServerAddress, settings.BackupPassword);

            // 清理临时文件
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 备份插件数据失败: {PluginName}", LOG_TAG, pluginName);
            return (false, $"备份失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从服务器恢复插件数据
    /// </summary>
    /// <param name="pluginName">插件唯一名称</param>
    /// <param name="dataFolderPath">插件data文件夹路径</param>
    /// <returns>成功返回true</returns>
    public async Task<(bool success, string message)> RestorePluginDataAsync(string pluginName, string dataFolderPath)
    {
        var settings = DBContext.GetSetting();
        
        if (string.IsNullOrEmpty(settings.ServerAddress))
        {
            return (false, "请先在高级设置中配置服务器地址");
        }

        if (string.IsNullOrEmpty(settings.BackupPassword))
        {
            return (false, "请先在高级设置中配置备份密码");
        }

        try
        {
            // 下载备份文件
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"plugin_restore_{pluginName}_{DateTime.Now:yyyyMMddHHmmss}.zip");
            
            var downloadResult = await DownloadBackupAsync(tempZipPath, "plugin", pluginName, settings.ServerAddress, settings.BackupPassword);
            if (!downloadResult.success)
            {
                return downloadResult;
            }

            Log.Information("[{Tag}] 开始恢复插件数据: {PluginName}", LOG_TAG, pluginName);

            // 备份当前数据（以防恢复失败）
            string backupPath = dataFolderPath + "_backup_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            if (Directory.Exists(dataFolderPath))
            {
                Directory.Move(dataFolderPath, backupPath);
            }

            try
            {
                // 解压到data文件夹
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
            finally
            {
                // 清理临时文件
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 恢复插件数据失败: {PluginName}", LOG_TAG, pluginName);
            return (false, $"恢复失败: {ex.Message}");
        }
    }

    #endregion

    #region 主程序数据备份

    /// <summary>
    /// 备份主程序数据库到服务器
    /// </summary>
    /// <returns>成功返回true</returns>
    public async Task<(bool success, string message)> BackupAppDataAsync()
    {
        var settings = DBContext.GetSetting();
        
        if (string.IsNullOrEmpty(settings.ServerAddress))
        {
            return (false, "请先在高级设置中配置服务器地址");
        }

        if (string.IsNullOrEmpty(settings.BackupPassword))
        {
            return (false, "请先在高级设置中配置备份密码");
        }

        try
        {
            string dbPath = Path.Combine(AppConfig.CONFIG_FOLDER, "microdock");
            if (!File.Exists(dbPath))
            {
                return (false, "数据库文件不存在");
            }

            // 创建临时ZIP文件
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"app_backup_{DateTime.Now:yyyyMMddHHmmss}.zip");
            
            Log.Information("[{Tag}] 开始备份主程序数据", LOG_TAG);

            // 压缩数据库文件
            using (var archive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(dbPath, "microdock", CompressionLevel.Optimal);
            }

            // 上传到服务器
            var result = await UploadBackupAsync(tempZipPath, "app", "main", settings.ServerAddress, settings.BackupPassword);

            if (result.success)
            {
                // 更新备份时间
                DBContext.UpdateSetting(s => s.LastAppBackupTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }

            // 清理临时文件
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 备份主程序数据失败", LOG_TAG);
            return (false, $"备份失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从服务器恢复主程序数据库
    /// </summary>
    /// <returns>成功返回true</returns>
    public async Task<(bool success, string message)> RestoreAppDataAsync()
    {
        var settings = DBContext.GetSetting();
        
        if (string.IsNullOrEmpty(settings.ServerAddress))
        {
            return (false, "请先在高级设置中配置服务器地址");
        }

        if (string.IsNullOrEmpty(settings.BackupPassword))
        {
            return (false, "请先在高级设置中配置备份密码");
        }

        try
        {
            string dbPath = Path.Combine(AppConfig.CONFIG_FOLDER, "microdock");
            
            // 下载备份文件
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"app_restore_{DateTime.Now:yyyyMMddHHmmss}.zip");
            
            var downloadResult = await DownloadBackupAsync(tempZipPath, "app", "main", settings.ServerAddress, settings.BackupPassword);
            if (!downloadResult.success)
            {
                return downloadResult;
            }

            Log.Information("[{Tag}] 开始恢复主程序数据", LOG_TAG);

            // 备份当前数据库
            string backupPath = dbPath + "_backup_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, backupPath, true);
            }

            try
            {
                // 解压并替换数据库
                using (var archive = ZipFile.OpenRead(tempZipPath))
                {
                    var entry = archive.GetEntry("microdock");
                    if (entry == null)
                    {
                        return (false, "备份文件格式错误");
                    }
                    entry.ExtractToFile(dbPath, true);
                }

                // 删除临时备份
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                Log.Information("[{Tag}] 主程序数据恢复成功，需要重启应用", LOG_TAG);
                return (true, "恢复成功，请重启应用以生效");
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
            finally
            {
                // 清理临时文件
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 恢复主程序数据失败", LOG_TAG);
            return (false, $"恢复失败: {ex.Message}");
        }
    }

    #endregion

    #region HTTP通信

    /// <summary>
    /// 上传备份文件到服务器
    /// </summary>
    private async Task<(bool success, string message)> UploadBackupAsync(
        string zipFilePath, 
        string type, 
        string identifier,
        string serverAddress,
        string password)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            
            // 添加文件
            var fileBytes = await File.ReadAllBytesAsync(zipFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "file", Path.GetFileName(zipFilePath));
            
            // 添加参数
            content.Add(new StringContent(type), "type");
            content.Add(new StringContent(identifier), "identifier");

            // 设置密码头
            _httpClient.DefaultRequestHeaders.Remove("X-Backup-Password");
            _httpClient.DefaultRequestHeaders.Add("X-Backup-Password", password);

            var url = $"{serverAddress.TrimEnd('/')}/api/backup/upload";
            Log.Debug("[{Tag}] 上传备份到: {Url}", LOG_TAG, url);

            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                Log.Information("[{Tag}] 备份上传成功: {Type}/{Identifier}", LOG_TAG, type, identifier);
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
    /// 从服务器下载备份文件
    /// </summary>
    private async Task<(bool success, string message)> DownloadBackupAsync(
        string destZipPath,
        string type,
        string identifier,
        string serverAddress,
        string password)
    {
        try
        {
            // 设置密码头
            _httpClient.DefaultRequestHeaders.Remove("X-Backup-Password");
            _httpClient.DefaultRequestHeaders.Add("X-Backup-Password", password);

            var url = $"{serverAddress.TrimEnd('/')}/api/backup/download?type={type}&identifier={Uri.EscapeDataString(identifier)}";
            Log.Debug("[{Tag}] 下载备份从: {Url}", LOG_TAG, url);

            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(destZipPath, bytes);
                
                Log.Information("[{Tag}] 备份下载成功: {Type}/{Identifier}", LOG_TAG, type, identifier);
                return (true, "下载成功");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, "服务器上没有找到备份数据");
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                Log.Warning("[{Tag}] 备份下载失败: {StatusCode} - {Error}", LOG_TAG, response.StatusCode, errorMsg);
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
    /// 测试服务器连接
    /// </summary>
    public async Task<(bool success, string message)> TestConnectionAsync(string serverAddress)
    {
        if (string.IsNullOrEmpty(serverAddress))
        {
            return (false, "服务器地址不能为空");
        }

        try
        {
            var url = $"{serverAddress.TrimEnd('/')}/api/health";
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

    #endregion
}

