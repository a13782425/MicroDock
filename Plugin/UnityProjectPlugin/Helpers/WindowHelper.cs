using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UnityProjectPlugin.Helpers;

public static class WindowHelper
{
    #region Windows API
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private const int SW_RESTORE = 9;
    #endregion
    /// <summary>
    /// 根据项目名模糊查找 Unity 窗口句柄
    /// </summary>
    /// <param name="projectName">项目名称（支持模糊匹配）</param>
    /// <returns>匹配的窗口信息列表</returns>
    public static List<UnityWindowInfo> FindUnityWindowsByProjectName(string projectName)
    {
        var results = new List<UnityWindowInfo>();
        if (string.IsNullOrWhiteSpace(projectName))
            return results;
        EnumWindows((hWnd, lParam) =>
        {
            // 只处理可见窗口
            if (!IsWindowVisible(hWnd))
                return true;
            // 获取窗口标题
            var titleBuilder = new StringBuilder(512);
            GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
            string title = titleBuilder.ToString();
            // 检查是否是 Unity 窗口（标题包含 "Unity" 且包含项目名）
            if (IsUnityEditorWindow(title) &&
                title.Contains(projectName, StringComparison.OrdinalIgnoreCase))
            {
                // 获取进程信息
                GetWindowThreadProcessId(hWnd, out uint processId);
                results.Add(new UnityWindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    ProcessId = (int)processId,
                    ProjectName = ExtractProjectName(title)
                });
            }
            return true; // 继续枚举
        }, IntPtr.Zero);
        return results;
    }
    /// <summary>
    /// 查找所有 Unity 编辑器窗口
    /// </summary>
    public static List<UnityWindowInfo> FindAllUnityWindows()
    {
        var results = new List<UnityWindowInfo>();
        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;
            var titleBuilder = new StringBuilder(512);
            GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
            string title = titleBuilder.ToString();
            if (IsUnityEditorWindow(title))
            {
                GetWindowThreadProcessId(hWnd, out uint processId);
                results.Add(new UnityWindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    ProcessId = (int)processId,
                    ProjectName = ExtractProjectName(title)
                });
            }
            return true;
        }, IntPtr.Zero);
        return results;
    }
    /// <summary>
    /// 检查窗口标题是否属于 Unity 编辑器
    /// </summary>
    private static bool IsUnityEditorWindow(string title)
    {
        if (string.IsNullOrEmpty(title))
            return false;
        // Unity 编辑器窗口标题通常包含 "Unity" 和版本号格式
        // 例如: "MyProject - SampleScene - Windows - Unity 2021.3.39f1"
        return title.Contains("Unity") &&
               (title.Contains(" - ") || title.StartsWith("Unity "));
    }
    /// <summary>
    /// 从窗口标题提取项目名称
    /// Unity 标题格式: "{项目名} - {场景名} - {平台} - Unity {版本}"
    /// </summary>
    private static string ExtractProjectName(string title)
    {
        if (string.IsNullOrEmpty(title))
            return string.Empty;
        // 取第一个 " - " 之前的部分作为项目名
        int dashIndex = title.IndexOf(" - ");
        if (dashIndex > 0)
        {
            return title.Substring(0, dashIndex).Trim();
        }
        return title;
    }
    /// <summary>
    /// 根据项目名激活 Unity 窗口
    /// </summary>
    /// <param name="projectName">项目名称</param>
    /// <returns>是否成功激活</returns>
    public static bool ActivateUnityWindowByProjectName(string projectName)
    {
        var windows = FindUnityWindowsByProjectName(projectName);

        if (windows.Count > 0)
        {
            // 激活第一个匹配的窗口
            return ActivateWindow(windows[0].Handle);
        }
        return false;
    }
    /// <summary>
    /// 激活并将窗口带到前台
    /// </summary>
    public static bool ActivateWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return false;
        try
        {
            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
            }
            IntPtr foregroundWindow = GetForegroundWindow();
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            uint currentThreadId = GetCurrentThreadId();
            uint targetThreadId = GetWindowThreadProcessId(hWnd, out _);
            if (foregroundThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, true);
                AttachThreadInput(currentThreadId, targetThreadId, true);
            }
            BringWindowToTop(hWnd);
            SetForegroundWindow(hWnd);
            if (foregroundThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
                AttachThreadInput(currentThreadId, targetThreadId, false);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
    /// <summary>
    /// 获取窗口标题
    /// </summary>
    public static string GetWindowTitle(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return string.Empty;
        var sb = new StringBuilder(512);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
/// <summary>
/// Unity 窗口信息
/// </summary>
public class UnityWindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}
