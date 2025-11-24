using Avalonia.Controls;
using Avalonia.Media;
using MicroDock.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UnityProjectPlugin.Models;
using UnityProjectPlugin.Views;

namespace UnityProjectPlugin
{
    /// <summary>
    /// Unity 项目管理插件
    /// </summary>
    public class UnityProjectPlugin : BaseMicroDockPlugin
    {
        private string _dataFolder = string.Empty;
        private List<UnityProject> _projects = new();
        private List<UnityVersion> _versions = new();
        private List<ProjectGroup> _groups = new();
        private UnityProjectTabView? _projectTabView;
        private UnityVersionSettingsView? _versionSettingsView;

        public override IMicroTab[] Tabs
        {
            get
            {
                if (_projectTabView == null)
                {
                    _projectTabView = new UnityProjectTabView(this);
                }
                return new IMicroTab[] { _projectTabView };
            }
        }

        public override object? GetSettingsControl()
        {
            //// 创建包含多个设置区域的容器
            //StackPanel container = new StackPanel
            //{
            //    Spacing = 24,
            //    Margin = new Avalonia.Thickness(0, 0, 0, 12)
            //};

            // Unity 版本管理
            if (_versionSettingsView == null)
            {
                _versionSettingsView = new UnityVersionSettingsView(this);
            }
            //container.Children.Add(_versionSettingsView);

            //// 添加分隔线
            //Border separator = new Border
            //{
            //    Height = 1,
            //    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(200, 200, 200)),
            //    Opacity = 0.3,
            //    Margin = new Avalonia.Thickness(0, 12, 0, 12)
            //};
            //container.Children.Add(separator);

            //// 分组管理
            //GroupManagementView groupManagementView = new GroupManagementView(this);
            //container.Children.Add(groupManagementView);

            return _versionSettingsView;
        }

        public override void OnInit()
        {
            base.OnInit();

            LogInfo("Unity 项目管理插件初始化中...");

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

            // 异步加载数据
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            Context?.ShowLoading("初始化 Unity 项目插件...");
            try
            {
                await LoadProjectsFromFileAsync();
                await LoadVersionsFromFileAsync();
                await LoadGroupsFromFileAsync();

                LogInfo($"已加载 {_projects.Count} 个项目、{_versions.Count} 个 Unity 版本和 {_groups.Count} 个分组");

                // 如果 Tab 视图已经创建，刷新它
                _projectTabView?.RefreshProjects();
            }
            finally
            {
                Context?.HideLoading();
            }
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
        /// 从文件加载项目列表
        /// </summary>
        private async Task LoadProjectsFromFileAsync()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "projects.json");
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    _projects = JsonSerializer.Deserialize<List<UnityProject>>(json, _jsonOptions) ?? new List<UnityProject>();
                    LogInfo($"从文件加载了 {_projects.Count} 个项目");
                }
                else
                {
                    _projects = new List<UnityProject>();
                    LogInfo("项目文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                LogError("从文件加载项目列表失败", ex);
                _projects = new List<UnityProject>();
            }
        }

        /// <summary>
        /// 保存项目列表到文件
        /// </summary>
        private async Task SaveProjectsToFileAsync()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "projects.json");
                string json = JsonSerializer.Serialize(_projects, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                LogInfo("项目列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存项目列表到文件失败", ex);
            }
        }

        /// <summary>
        /// 从文件加载 Unity 版本列表
        /// </summary>
        private async Task LoadVersionsFromFileAsync()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "versions.json");
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    _versions = JsonSerializer.Deserialize<List<UnityVersion>>(json, _jsonOptions) ?? new List<UnityVersion>();
                    LogInfo($"从文件加载了 {_versions.Count} 个 Unity 版本");
                }
                else
                {
                    _versions = new List<UnityVersion>();
                    LogInfo("版本文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                LogError("从文件加载 Unity 版本列表失败", ex);
                _versions = new List<UnityVersion>();
            }
        }

        /// <summary>
        /// 保存 Unity 版本列表到文件
        /// </summary>
        private async Task SaveVersionsToFileAsync()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "versions.json");
                string json = JsonSerializer.Serialize(_versions, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                LogInfo("Unity 版本列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存 Unity 版本列表到文件失败", ex);
            }
        }

        /// <summary>
        /// 从文件加载分组列表
        /// </summary>
        private async Task LoadGroupsFromFileAsync()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "groups.json");
                if (File.Exists(filePath))
                {
                    string json = await File.ReadAllTextAsync(filePath);
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
        private async Task SaveGroupsToFileAsync()
        {
            try
            {
                string filePath = Path.Combine(_dataFolder, "groups.json");
                string json = JsonSerializer.Serialize(_groups, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                LogInfo("分组列表已保存到文件");
            }
            catch (Exception ex)
            {
                LogError("保存分组列表到文件失败", ex);
            }
        }

        /// <summary>
        /// 添加项目
        /// </summary>
        public async Task AddProjectAsync(string path, string? name = null, string? groupName = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("项目路径不能为空", nameof(path));
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"项目路径不存在: {path}");
            }

            // 规范化路径并去除末尾的反斜杠
            string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // 检查是否已存在
            if (_projects.Any(p => p.Id == fullPath.ToLowerInvariant()))
            {
                throw new InvalidOperationException("该项目已存在");
            }

            UnityProject project = new UnityProject
            {
                Name = name ?? Path.GetFileName(fullPath),
                Path = fullPath,
                GroupName = groupName,
                LastOpened = DateTime.Now
            };

            // 尝试读取项目的 Unity 版本
            try
            {
                string versionFile = Path.Combine(fullPath, "ProjectSettings", "ProjectVersion.txt");
                if (File.Exists(versionFile))
                {
                    string[] lines = await File.ReadAllLinesAsync(versionFile);
                    string? versionLine = lines.FirstOrDefault(l => l.StartsWith("m_EditorVersion:"));
                    if (versionLine != null)
                    {
                        project.UnityVersion = versionLine.Split(':')[1].Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"无法读取项目版本: {ex.Message}");
            }

            _projects.Add(project);
            await SaveProjectsToFileAsync();

            LogInfo($"已添加项目: {project.Name} ({project.Path})");
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        public async Task RemoveProjectAsync(string path)
        {
            string fullPath = Path.GetFullPath(path).ToLowerInvariant();
            UnityProject? project = _projects.FirstOrDefault(p => p.Id == fullPath);

            if (project != null)
            {
                _projects.Remove(project);
                await SaveProjectsToFileAsync();
                LogInfo($"已删除项目: {project.Name}");
            }
        }

        /// <summary>
        /// 更新项目信息
        /// </summary>
        public async Task UpdateProjectAsync(string projectPath, string newName, string? groupName)
        {
            UnityProject? project = _projects.FirstOrDefault(p => p.Path == projectPath);
            if (project != null)
            {
                project.Name = newName;
                project.GroupName = groupName;
                await SaveProjectsToFileAsync();
                LogInfo($"已更新项目: {project.Name}");
            }
        }

        /// <summary>
        /// 添加 Unity 版本
        /// </summary>
        public async Task AddVersionAsync(string version, string editorPath)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("版本号不能为空", nameof(version));
            }

            if (string.IsNullOrWhiteSpace(editorPath))
            {
                throw new ArgumentException("Editor 路径不能为空", nameof(editorPath));
            }

            if (!File.Exists(editorPath))
            {
                throw new FileNotFoundException($"Unity Editor 不存在: {editorPath}");
            }

            // 检查是否已存在
            if (_versions.Any(v => v.Version == version))
            {
                throw new InvalidOperationException($"版本 {version} 已存在");
            }

            UnityVersion unityVersion = new UnityVersion
            {
                Version = version,
                EditorPath = editorPath
            };

            _versions.Add(unityVersion);
            await SaveVersionsToFileAsync();

            LogInfo($"已添加 Unity 版本: {version} ({editorPath})");
        }

        /// <summary>
        /// 删除 Unity 版本
        /// </summary>
        public async Task RemoveVersionAsync(string version)
        {
            UnityVersion? unityVersion = _versions.FirstOrDefault(v => v.Version == version);

            if (unityVersion != null)
            {
                _versions.Remove(unityVersion);
                await SaveVersionsToFileAsync();
                LogInfo($"已删除 Unity 版本: {version}");
            }
        }

        /// <summary>
        /// 添加分组
        /// </summary>
        public async Task AddGroupAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("分组名称不能为空", nameof(name));
            }

            // 检查是否已存在同名分组
            if (_groups.Any(g => g.Name == name))
            {
                throw new InvalidOperationException($"分组 '{name}' 已存在");
            }

            ProjectGroup group = new ProjectGroup
            {
                Name = name
            };

            _groups.Add(group);
            await SaveGroupsToFileAsync();

            LogInfo($"已添加分组: {name}");
        }

        /// <summary>
        /// 更新分组
        /// </summary>
        public async Task UpdateGroupAsync(string id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("分组名称不能为空", nameof(newName));
            }

            ProjectGroup? group = _groups.FirstOrDefault(g => g.Id == id);
            if (group == null)
            {
                throw new InvalidOperationException($"分组不存在");
            }

            // 检查新名称是否与其他分组重复
            if (_groups.Any(g => g.Id != id && g.Name == newName))
            {
                throw new InvalidOperationException($"分组 '{newName}' 已存在");
            }

            string oldName = group.Name;
            group.Name = newName;

            // 更新使用该分组的所有项目
            foreach (UnityProject project in _projects.Where(p => p.GroupName == oldName))
            {
                project.GroupName = newName;
            }

            await SaveGroupsToFileAsync();
            await SaveProjectsToFileAsync();

            LogInfo($"已更新分组: {oldName} -> {newName}");
        }

        /// <summary>
        /// 删除分组
        /// </summary>
        public async Task DeleteGroupAsync(string id)
        {
            ProjectGroup? group = _groups.FirstOrDefault(g => g.Id == id);
            if (group == null)
            {
                throw new InvalidOperationException($"分组不存在");
            }

            // 检查是否有项目使用该分组
            int usageCount = GetGroupUsageCount(group.Name);
            if (usageCount > 0)
            {
                throw new InvalidOperationException($"无法删除分组 '{group.Name}'，还有 {usageCount} 个项目使用该分组");
            }

            _groups.Remove(group);
            await SaveGroupsToFileAsync();

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
            return _projects.Count(p => p.GroupName == groupName);
        }

        /// <summary>
        /// 获取所有项目
        /// </summary>
        public List<UnityProject> GetProjects() => new List<UnityProject>(_projects);

        /// <summary>
        /// 获取所有版本
        /// </summary>
        public List<UnityVersion> GetVersions() => new List<UnityVersion>(_versions);

        #endregion

        #region 工具方法

        /// <summary>
        /// 判断指定文件夹是否是 Unity 项目
        /// </summary>
        [MicroTool("unity.is_project",
            Description = "判断指定文件夹是否是 Unity 项目",
            ReturnDescription = "true 表示是 Unity 项目，false 表示不是")]
        public async Task<string> IsUnityProject(
            [ToolParameter("path", Description = "项目文件夹路径")] string path)
        {
            await Task.CompletedTask;

            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    return JsonSerializer.Serialize(new { success = false, isProject = false, message = "路径不存在" });
                }

                // 检查是否存在 Assets 和 ProjectSettings 文件夹
                var assetsPath = Path.Combine(path, "Assets");
                var projectSettingsPath = Path.Combine(path, "ProjectSettings");

                bool isProject = Directory.Exists(assetsPath) && Directory.Exists(projectSettingsPath);

                LogInfo($"检查路径 {path} 是否为 Unity 项目: {isProject}");

                return JsonSerializer.Serialize(new { success = true, isProject, message = isProject ? "是 Unity 项目" : "不是 Unity 项目" });
            }
            catch (Exception ex)
            {
                LogError($"判断 Unity 项目时出错: {path}", ex);
                return JsonSerializer.Serialize(new { success = false, isProject = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 使用指定版本的 Unity 打开项目
        /// </summary>
        [MicroTool("unity.open_project",
            Description = "使用指定版本的 Unity 打开项目",
            ReturnDescription = "操作结果消息")]
        public async Task<string> OpenUnityProject(
            [ToolParameter("projectPath", Description = "项目路径")] string projectPath,
            [ToolParameter("editorPath", Description = "Unity Editor 路径", Required = false)] string? editorPath = null)
        {
            await Task.CompletedTask;

            try
            {
                // 验证项目路径
                if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
                {
                    return JsonSerializer.Serialize(new { success = false, message = "项目路径不存在" });
                }

                // 检查是否是 Unity 项目
                var assetsPath = Path.Combine(projectPath, "Assets");
                var projectSettingsPath = Path.Combine(projectPath, "ProjectSettings");
                if (!Directory.Exists(assetsPath) || !Directory.Exists(projectSettingsPath))
                {
                    return JsonSerializer.Serialize(new { success = false, message = "不是有效的 Unity 项目" });
                }

                // 确定 Unity Editor 路径
                string? finalEditorPath = editorPath;

                if (string.IsNullOrEmpty(finalEditorPath))
                {
                    // 尝试从项目版本文件读取版本
                    try
                    {
                        var versionFile = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
                        if (File.Exists(versionFile))
                        {
                            var lines = await File.ReadAllLinesAsync(versionFile);
                            var versionLine = lines.FirstOrDefault(l => l.StartsWith("m_EditorVersion:"));
                            if (versionLine != null)
                            {
                                var projectVersion = versionLine.Split(':')[1].Trim();

                                // 在已配置的版本中查找匹配版本
                                var matchedVersion = _versions.FirstOrDefault(v => v.Version == projectVersion);
                                if (matchedVersion != null)
                                {
                                    finalEditorPath = matchedVersion.EditorPath;
                                    LogInfo($"使用匹配的 Unity 版本: {projectVersion}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"读取项目版本失败: {ex.Message}");
                    }
                }

                // 如果还是没有找到 Editor 路径
                if (string.IsNullOrEmpty(finalEditorPath))
                {
                    // 使用第一个可用版本
                    if (_versions.Count > 0)
                    {
                        finalEditorPath = _versions[0].EditorPath;
                        LogInfo($"使用默认 Unity 版本: {_versions[0].Version}");
                    }
                    else
                    {
                        return JsonSerializer.Serialize(new { success = false, message = "未配置任何 Unity 版本，请先在设置中添加 Unity 版本" });
                    }
                }

                // 验证 Editor 路径
                if (!File.Exists(finalEditorPath))
                {
                    return JsonSerializer.Serialize(new { success = false, message = $"Unity Editor 不存在: {finalEditorPath}" });
                }
                // 启动 Unity
                var startInfo = new ProcessStartInfo
                {
                    FileName = finalEditorPath,
                    Arguments = $"-projectPath \"{projectPath}\"",
                    UseShellExecute = true
                };

                Process? process = Process.Start(startInfo);

                // 只有成功启动后才更新最后打开时间
                if (process != null)
                {
                    UnityProject? project = _projects.FirstOrDefault(p => p.Id == Path.GetFullPath(projectPath).ToLowerInvariant());
                    if (project != null)
                    {
                        project.LastOpened = DateTime.Now;
                        await SaveProjectsToFileAsync();
                    }

                    LogInfo($"已打开 Unity 项目: {projectPath}");
                    return JsonSerializer.Serialize(new { success = true, message = "Unity 项目已打开" });
                }
                else
                {
                    return JsonSerializer.Serialize(new { success = false, message = "无法启动 Unity 编辑器" });
                }
            }
            catch (Exception ex)
            {
                LogError($"打开 Unity 项目时出错: {projectPath}", ex);
                return JsonSerializer.Serialize(new { success = false, message = ex.Message });
            }
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogInfo("Unity 项目管理插件已销毁");
        }
    }
}

