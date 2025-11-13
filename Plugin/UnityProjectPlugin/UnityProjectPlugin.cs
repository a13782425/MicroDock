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
        private const string PROJECTS_KEY = "projects";
        private const string VERSIONS_KEY = "unity_versions";

        private List<UnityProject> _projects = new();
        private List<UnityVersion> _versions = new();
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
            if (_versionSettingsView == null)
            {
                _versionSettingsView = new UnityVersionSettingsView(this);
            }
            return _versionSettingsView;
        }

        public override void OnInit()
        {
            base.OnInit();

            LogInfo("Unity 项目管理插件初始化中...");

            // 加载数据
            LoadProjects();
            LoadVersions();

            LogInfo($"已加载 {_projects.Count} 个项目和 {_versions.Count} 个 Unity 版本");
        }

        #region 数据管理

        /// <summary>
        /// 加载项目列表
        /// </summary>
        private void LoadProjects()
        {
            try
            {
                var json = GetSettings(PROJECTS_KEY);
                if (!string.IsNullOrEmpty(json))
                {
                    _projects = JsonSerializer.Deserialize<List<UnityProject>>(json) ?? new List<UnityProject>();
                }
            }
            catch (Exception ex)
            {
                LogError("加载项目列表失败", ex);
                _projects = new List<UnityProject>();
            }
        }

        /// <summary>
        /// 保存项目列表
        /// </summary>
        private void SaveProjects()
        {
            try
            {
                var json = JsonSerializer.Serialize(_projects);
                SetSettings(PROJECTS_KEY, json, "Unity 项目列表");
                LogInfo("项目列表已保存");
            }
            catch (Exception ex)
            {
                LogError("保存项目列表失败", ex);
            }
        }

        /// <summary>
        /// 加载 Unity 版本列表
        /// </summary>
        private void LoadVersions()
        {
            try
            {
                var json = GetSettings(VERSIONS_KEY);
                if (!string.IsNullOrEmpty(json))
                {
                    _versions = JsonSerializer.Deserialize<List<UnityVersion>>(json) ?? new List<UnityVersion>();
                }
            }
            catch (Exception ex)
            {
                LogError("加载 Unity 版本列表失败", ex);
                _versions = new List<UnityVersion>();
            }
        }

        /// <summary>
        /// 保存 Unity 版本列表
        /// </summary>
        private void SaveVersions()
        {
            try
            {
                var json = JsonSerializer.Serialize(_versions);
                SetSettings(VERSIONS_KEY, json, "Unity 版本列表");
                LogInfo("Unity 版本列表已保存");
            }
            catch (Exception ex)
            {
                LogError("保存 Unity 版本列表失败", ex);
            }
        }

        /// <summary>
        /// 添加项目
        /// </summary>
        public void AddProject(string path, string? name = null)
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
            var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // 检查是否已存在
            if (_projects.Any(p => p.Id == fullPath.ToLowerInvariant()))
            {
                throw new InvalidOperationException("该项目已存在");
            }

            var project = new UnityProject
            {
                Name = name ?? Path.GetFileName(fullPath),
                Path = fullPath,
                LastOpened = DateTime.Now
            };

            // 尝试读取项目的 Unity 版本
            try
            {
                var versionFile = Path.Combine(fullPath, "ProjectSettings", "ProjectVersion.txt");
                if (File.Exists(versionFile))
                {
                    var lines = File.ReadAllLines(versionFile);
                    var versionLine = lines.FirstOrDefault(l => l.StartsWith("m_EditorVersion:"));
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
            SaveProjects();

            LogInfo($"已添加项目: {project.Name} ({project.Path})");
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        public void RemoveProject(string path)
        {
            var fullPath = Path.GetFullPath(path).ToLowerInvariant();
            var project = _projects.FirstOrDefault(p => p.Id == fullPath);

            if (project != null)
            {
                _projects.Remove(project);
                SaveProjects();
                LogInfo($"已删除项目: {project.Name}");
            }
        }

        /// <summary>
        /// 添加 Unity 版本
        /// </summary>
        public void AddVersion(string version, string editorPath)
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

            var unityVersion = new UnityVersion
            {
                Version = version,
                EditorPath = editorPath
            };

            _versions.Add(unityVersion);
            SaveVersions();

            LogInfo($"已添加 Unity 版本: {version} ({editorPath})");
        }

        /// <summary>
        /// 删除 Unity 版本
        /// </summary>
        public void RemoveVersion(string version)
        {
            var unityVersion = _versions.FirstOrDefault(v => v.Version == version);

            if (unityVersion != null)
            {
                _versions.Remove(unityVersion);
                SaveVersions();
                LogInfo($"已删除 Unity 版本: {version}");
            }
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
                            var lines = File.ReadAllLines(versionFile);
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

                Process.Start(startInfo);

                // 更新最后打开时间
                var project = _projects.FirstOrDefault(p => p.Id == Path.GetFullPath(projectPath).ToLowerInvariant());
                if (project != null)
                {
                    project.LastOpened = DateTime.Now;
                    SaveProjects();
                }

                LogInfo($"已打开 Unity 项目: {projectPath}");
                return JsonSerializer.Serialize(new { success = true, message = "Unity 项目已打开" });
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

