using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityProjectPlugin.Models;

namespace UnityProjectPlugin.Helpers
{
    /// <summary>
    /// 平台检测辅助类
    /// </summary>
    public static class PlatformDetector
    {
        /// <summary>
        /// 检测指定 Unity Editor 路径已安装的平台
        /// </summary>
        /// <param name="editorPath">Unity Editor 可执行文件路径（如 C:\...\Editor\Unity.exe）</param>
        /// <returns>带有安装状态的平台列表</returns>
        public static List<UnityPlatform> DetectInstalledPlatforms(string editorPath)
        {
            List<UnityPlatform> platforms = UnityPlatform.GetPredefinedPlatforms();

            if (string.IsNullOrEmpty(editorPath))
            {
                return platforms;
            }

            try
            {
                // 从 Editor 路径推算 PlaybackEngines 路径
                // Unity.exe 位于: {Unity安装目录}/Editor/Unity.exe
                // PlaybackEngines 位于: {Unity安装目录}/Editor/Data/PlaybackEngines
                string? editorDir = Path.GetDirectoryName(editorPath);
                if (string.IsNullOrEmpty(editorDir))
                {
                    return platforms;
                }

                string playbackEnginesPath = Path.Combine(editorDir, "Data", "PlaybackEngines");

                if (!Directory.Exists(playbackEnginesPath))
                {
                    return platforms;
                }

                // 获取已安装的平台文件夹
                HashSet<string> installedFolders = Directory.GetDirectories(playbackEnginesPath)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

                // 更新平台安装状态
                foreach (UnityPlatform platform in platforms)
                {
                    platform.IsInstalled = installedFolders.Contains(platform.FolderName);
                }
            }
            catch
            {
                // 检测失败时保持默认状态（未安装）
            }

            return platforms;
        }

        /// <summary>
        /// 获取所有预定义平台（不检测安装状态）
        /// </summary>
        public static List<UnityPlatform> GetAllPlatforms()
        {
            return UnityPlatform.GetPredefinedPlatforms();
        }

        /// <summary>
        /// 根据平台 ID 获取平台信息
        /// </summary>
        public static UnityPlatform? GetPlatformById(string platformId)
        {
            return UnityPlatform.GetPredefinedPlatforms()
                .FirstOrDefault(p => p.Id.Equals(platformId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
