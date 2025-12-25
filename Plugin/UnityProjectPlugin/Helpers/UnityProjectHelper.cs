using System.Diagnostics;
using System.Text.Json;
using UnityProjectPlugin.Models;
using static UnityProjectPlugin.Models.UnityProjectData;

namespace UnityProjectPlugin.Helpers;

internal static class UnityProjectHelper
{
    /// <summary>
    /// 尝试激活已运行项目的窗口（通过项目名模糊匹配）
    /// </summary>
    private static bool TryActivateExistingWindowByName(string projectName)
    {
        // 直接通过项目名查找 Unity 窗口
        var windows = WindowHelper.FindUnityWindowsByProjectName(projectName);

        if (windows.Count > 0)
        {
            UnityProjectPlugin.Instance.Context?.LogInfo($"找到 {windows.Count} 个匹配的 Unity 窗口: {windows[0].Title}");
            return WindowHelper.ActivateWindow(windows[0].Handle);
        }
        return false;
    }

    /// <summary>
    /// 使用项目配置的 Unity 版本和目标平台打开项目
    /// </summary>
    /// <param name="project">Unity 项目对象</param>
    /// <returns>操作结果</returns>
    public static async Task<string> OpenUnityProject(UnityProject project)
    {
        if (project == null)
        {
            return JsonSerializer.Serialize(new { success = false, message = "项目对象为空" });
        }

        try
        {
            // 验证项目路径
            if (string.IsNullOrWhiteSpace(project.Path) || !Directory.Exists(project.Path))
            {
                return JsonSerializer.Serialize(new { success = false, message = "项目路径不存在" });
            }

            // 先尝试通过项目名激活已有窗口
            if (TryActivateExistingWindowByName(project.Name))
            {
                UnityProjectPlugin.Instance.Context?.LogInfo($"激活已运行的 Unity 项目: {project.Name}");
                return JsonSerializer.Serialize(new { success = true, message = "已激活运行中的项目窗口" });
            }

            // 确定 Unity Editor 路径
            string? editorPath = null;

            // 优先使用项目配置的 UnityVersion
            if (!string.IsNullOrEmpty(project.UnityVersion))
            {
                var matchedVersion = Versions.FirstOrDefault(v => v.Version == project.UnityVersion);
                if (matchedVersion != null)
                {
                    editorPath = matchedVersion.EditorPath;
                    UnityProjectPlugin.Instance.Context?.LogInfo($"使用项目配置的 Unity 版本: {project.UnityVersion}");
                }
                else
                {
                    UnityProjectPlugin.Instance.Context?.LogWarning($"未找到匹配的 Unity 版本: {project.UnityVersion}，将使用默认版本");
                }
            }

            // 如果没有匹配的版本，使用第一个可用版本
            if (string.IsNullOrEmpty(editorPath))
            {
                if (Versions.Count > 0)
                {
                    editorPath = Versions[0].EditorPath;
                    UnityProjectPlugin.Instance.Context?.LogInfo($"使用默认 Unity 版本: {Versions[0].Version}");
                }
                else
                {
                    return JsonSerializer.Serialize(new { success = false, message = "未配置任何 Unity 版本，请先在设置中添加 Unity 版本" });
                }
            }

            // 验证 Editor 路径
            if (!File.Exists(editorPath))
            {
                return JsonSerializer.Serialize(new { success = false, message = $"Unity Editor 不存在: {editorPath}" });
            }

            // 构建启动参数
            var arguments = $"-projectPath \"{project.Path}\"";

            // 添加目标平台参数
            if (!string.IsNullOrEmpty(project.TargetPlatform))
            {
                string buildTarget = ConvertToBuildTarget(project.TargetPlatform);
                if (!string.IsNullOrEmpty(buildTarget))
                {
                    arguments += $" -buildTarget {buildTarget}";
                    UnityProjectPlugin.Instance.Context?.LogInfo($"使用目标平台: {project.TargetPlatform}");
                }
            }

            // 启动 Unity
            var startInfo = new ProcessStartInfo
            {
                FileName = editorPath,
                Arguments = arguments,
                UseShellExecute = true
            };

            Process? process = Process.Start(startInfo);

            if (process != null)
            {
                // 更新最后打开时间
                project.LastOpened = DateTime.Now;
                await UnityProjectPlugin.Instance.SaveProjectsToFileAsync();

                UnityProjectPlugin.Instance.Context?.LogInfo($"已打开 Unity 项目: {project.Path}");
                return JsonSerializer.Serialize(new { success = true, message = "Unity 项目已打开" });
            }
            else
            {
                return JsonSerializer.Serialize(new { success = false, message = "无法启动 Unity 编辑器" });
            }
        }
        catch (Exception ex)
        {
            UnityProjectPlugin.Instance.Context?.LogError($"打开 Unity 项目时出错: {project.Path}", ex);
            return JsonSerializer.Serialize(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 将平台 ID 转换为 Unity 命令行 buildTarget 参数
    /// </summary>
    private static string ConvertToBuildTarget(string platformId)
    {
        return platformId switch
        {
            "Windows" => "Win64",
            "Android" => "Android",
            "iOS" => "iOS",
            "WebGL" => "WebGL",
            "Linux" => "Linux64",
            "macOS" => "OSXUniversal",
            "UWP" => "WindowsStoreApps",
            "tvOS" => "tvOS",
            _ => string.Empty
        };
    }
}
