using Avalonia.Controls;
using MicroDock.Plugin;
using RemoteDesktopPlugin.Models;
using RemoteDesktopPlugin.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RemoteDesktopPlugin
{
    /// <summary>
    /// 远程桌面管理插件
    /// </summary>
    public class RemoteDesktopPlugin : BaseMicroDockPlugin
    {
        private string _dataFolder = string.Empty;
        private List<RemoteConnection> _connections = new();
        private List<ProjectGroup> _groups = new();
        private RemoteDesktopTabView? _desktopTabView;

        public override IMicroTab[] Tabs
        {
            get
            {
                if (_desktopTabView == null)
                {
                    _desktopTabView = new RemoteDesktopTabView(this);
                }
                return new IMicroTab[] { _desktopTabView };
            }
        }

        public override object? GetSettingsControl()
        {
            // 返回null表示没有设置界面，或者可以创建一个简单的设置界面
            return null;
        }

        public override void OnInit()
        {
            base.OnInit();

            LogInfo("远程桌面管理插件初始化中...");

            // 初始化数据文件夹路径
            _dataFolder = Context?.DataPath ?? string.Empty;
            if (string.IsNullOrEmpty(_dataFolder))
            {
                LogError("无法获取插件数据文件夹路径");
                return;
            }

            // 确保数据文件夹存在
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
                LogInfo($"创建数据文件夹: {_dataFolder}");
            }

            // 加载数据
            LoadConnectionsFromFile();
            LoadGroupsFromFile();

            LogInfo($"已加载 {_connections.Count} 个远程连接和 {_groups.Count} 个分组");
        }

        #region 数据管理

        /// <summary>
        /// JSON 序列化选项
        /// </summary>
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// 从文件加载连接列表
        /// </summary>
        private void LoadConnectionsFromFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "connections.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _connections = JsonSerializer.Deserialize<List<RemoteConnection>>(json, _jsonOptions) ?? new List<RemoteConnection>();
                    LogInfo($"从文件加载了 {_connections.Count} 个连接");
                }
                else
                {
                    _connections = new List<RemoteConnection>();
                    LogInfo("连接文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                LogError("从文件加载连接列表失败", ex);
                _connections = new List<RemoteConnection>();
            }
        }

        /// <summary>
        /// 保存连接列表到文件
        /// </summary>
        private void SaveConnectionsToFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "connections.json");
                string json = JsonSerializer.Serialize(_connections, _jsonOptions);
                File.WriteAllText(filePath, json);
                LogInfo("连接列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存连接列表到文件失败", ex);
            }
        }

        /// <summary>
        /// 从文件加载分组列表
        /// </summary>
        private void LoadGroupsFromFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "groups.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _groups = JsonSerializer.Deserialize<List<ProjectGroup>>(json, _jsonOptions) ?? new List<ProjectGroup>();
                    LogInfo($"从文件加载了 {_groups.Count} 个分组");
                }
                else
                {
                    _groups = new List<ProjectGroup>();
                    LogInfo("分组文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                LogError("从文件加载分组列表失败", ex);
                _groups = new List<ProjectGroup>();
            }
        }

        /// <summary>
        /// 保存分组列表到文件
        /// </summary>
        private void SaveGroupsToFile()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "groups.json");
                string json = JsonSerializer.Serialize(_groups, _jsonOptions);
                File.WriteAllText(filePath, json);
                LogInfo("分组列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存分组列表到文件失败", ex);
            }
        }

        #endregion

        #region 连接管理

        /// <summary>
        /// 添加远程连接
        /// </summary>
        public void AddConnection(string name, string host, string username, string password,
            int port = 3389, string? domain = null, string? groupName = null, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("连接名称不能为空", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("主机地址不能为空", nameof(host));
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("用户名不能为空", nameof(username));
            }

            // 检查是否已存在同名连接
            if (_connections.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"连接名称 '{name}' 已存在");
            }

            var connection = new RemoteConnection
            {
                Name = name.Trim(),
                Host = host.Trim(),
                Port = port,
                Username = username.Trim(),
                Password = password, // 在实际应用中应该加密存储
                Domain = domain,
                GroupName = groupName,
                Description = description
            };

            _connections.Add(connection);
            SaveConnectionsToFile();

            LogInfo($"已添加远程连接: {connection.Name} ({connection.Host})");
        }

        /// <summary>
        /// 更新远程连接
        /// </summary>
        public void UpdateConnection(string id, string name, string host, string username, string password,
            int port = 3389, string? groupName = null, string? description = null)
        {
            var connection = _connections.FirstOrDefault(c => c.Id == id);
            if (connection == null)
            {
                throw new InvalidOperationException("连接不存在");
            }

            // 检查新名称是否与其他连接重复
            if (_connections.Any(c => c.Id != id && c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"连接名称 '{name}' 已存在");
            }

            connection.Name = name.Trim();
            connection.Host = host.Trim();
            connection.Port = port;
            connection.Username = username.Trim();
            connection.Password = password;
            connection.GroupName = groupName;
            connection.Description = description;

            SaveConnectionsToFile();
            LogInfo($"已更新远程连接: {connection.Name}");
        }

        /// <summary>
        /// 删除远程连接
        /// </summary>
        public void RemoveConnection(string id)
        {
            var connection = _connections.FirstOrDefault(c => c.Id == id);
            if (connection != null)
            {
                _connections.Remove(connection);
                SaveConnectionsToFile();
                LogInfo($"已删除远程连接: {connection.Name}");
            }
        }

        /// <summary>
        /// 连接到远程桌面
        /// </summary>
        public void ConnectToRemote(RemoteConnection connection)
        {
            try
            {
                // 构建RDP文件内容
                string rdpContent = GenerateRdpFileContent(connection);

                // 创建临时RDP文件
                string tempPath = Path.GetTempPath();
                string rdpFileName = $"{connection.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.rdp";
                string rdpFilePath = Path.Combine(tempPath, rdpFileName);

                File.WriteAllText(rdpFilePath, rdpContent);

                // 启动远程桌面连接
                var startInfo = new ProcessStartInfo
                {
                    FileName = rdpFilePath,
                    UseShellExecute = true
                };

                Process.Start(startInfo);

                // 更新最后连接时间
                connection.LastConnected = DateTime.Now;
                SaveConnectionsToFile();

                LogInfo($"已启动远程连接: {connection.Name} ({connection.Host})");
            }
            catch (Exception ex)
            {
                LogError($"启动远程连接失败: {connection.Name}", ex);
                throw;
            }
        }

        /// <summary>
        /// 生成RDP文件内容
        /// </summary>
        private string GenerateRdpFileContent(RemoteConnection connection)
        {
            var content = $"full address:s:{connection.Host}\n";
            content += $"server port:i:{connection.Port}\n";
            content += $"username:s:{connection.Username}\n";

            if (!string.IsNullOrEmpty(connection.Domain))
            {
                content += $"domain:s:{connection.Domain}\n";
            }

            // 基本RDP设置
            content += "screen mode id:i:2\n"; // 全屏
            content += "desktopwidth:i:1920\n";
            content += "desktopheight:i:1080\n";
            content += "session bpp:i:32\n";
            content += "compression:i:1\n";
            content += "keyboardhook:i:2\n";
            content += "audiomode:i:0\n";
            content += "redirectdrives:i:0\n";
            content += "redirectprinters:i:0\n";
            content += "redirectclipboard:i:1\n";
            content += "redirectposdevices:i:0\n";

            return content;
        }

        /// <summary>
        /// 获取所有连接
        /// </summary>
        public List<RemoteConnection> GetConnections() => new List<RemoteConnection>(_connections);

        #endregion

        #region 分组管理

        /// <summary>
        /// 添加分组
        /// </summary>
        public void AddGroup(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("分组名称不能为空", nameof(name));
            }

            // 检查是否已存在同名分组
            if (_groups.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"分组 '{name}' 已存在");
            }

            var group = new ProjectGroup
            {
                Name = name.Trim()
            };

            _groups.Add(group);
            SaveGroupsToFile();

            LogInfo($"已添加分组: {name}");
        }

        /// <summary>
        /// 更新分组
        /// </summary>
        public void UpdateGroup(string id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("分组名称不能为空", nameof(newName));
            }

            var group = _groups.FirstOrDefault(g => g.Id == id);
            if (group == null)
            {
                throw new InvalidOperationException("分组不存在");
            }

            // 检查新名称是否与其他分组重复
            if (_groups.Any(g => g.Id != id && g.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"分组 '{newName}' 已存在");
            }

            string oldName = group.Name;
            group.Name = newName.Trim();

            // 更新使用该分组的所有连接
            foreach (var connection in _connections.Where(c => c.GroupName == oldName))
            {
                connection.GroupName = newName;
            }

            SaveGroupsToFile();
            SaveConnectionsToFile();

            LogInfo($"已更新分组: {oldName} -> {newName}");
        }

        /// <summary>
        /// 删除分组
        /// </summary>
        public void DeleteGroup(string id)
        {
            var group = _groups.FirstOrDefault(g => g.Id == id);
            if (group == null)
            {
                throw new InvalidOperationException("分组不存在");
            }

            // 检查是否有连接使用该分组
            int usageCount = GetGroupUsageCount(group.Name);
            if (usageCount > 0)
            {
                throw new InvalidOperationException($"无法删除分组 '{group.Name}'，还有 {usageCount} 个连接使用该分组");
            }

            _groups.Remove(group);
            SaveGroupsToFile();

            LogInfo($"已删除分组: {group.Name}");
        }

        /// <summary>
        /// 获取所有分组
        /// </summary>
        public List<ProjectGroup> GetGroups() => new List<ProjectGroup>(_groups);

        /// <summary>
        /// 获取分组使用数量
        /// </summary>
        public int GetGroupUsageCount(string groupName)
        {
            return _connections.Count(c => c.GroupName == groupName);
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogInfo("远程桌面管理插件已销毁");
        }
    }
}