using System.Collections.Generic;

namespace UnityProjectPlugin.Models
{
    /// <summary>
    /// Unity 平台模型
    /// </summary>
    public class UnityPlatform
    {
        /// <summary>
        /// 平台唯一标识（如 "Android", "iOS"）
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// PlaybackEngines 目录中的文件夹名称
        /// </summary>
        public string FolderName { get; set; } = string.Empty;

        /// <summary>
        /// 是否已安装该平台模块
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// 预定义的平台列表
        /// </summary>
        public static List<UnityPlatform> GetPredefinedPlatforms()
        {
            return new List<UnityPlatform>
            {
                new UnityPlatform
                {
                    Id = "Windows",
                    DisplayName = "Windows",
                    FolderName = "WindowsStandaloneSupport"
                },
                new UnityPlatform
                {
                    Id = "Android",
                    DisplayName = "Android",
                    FolderName = "AndroidPlayer"
                },
                new UnityPlatform
                {
                    Id = "iOS",
                    DisplayName = "iOS",
                    FolderName = "iOSSupport"
                },
                new UnityPlatform
                {
                    Id = "WebGL",
                    DisplayName = "WebGL",
                    FolderName = "WebGLSupport"
                },
                new UnityPlatform
                {
                    Id = "Linux",
                    DisplayName = "Linux",
                    FolderName = "LinuxStandaloneSupport"
                },
                new UnityPlatform
                {
                    Id = "macOS",
                    DisplayName = "macOS",
                    FolderName = "MacStandaloneSupport"
                },
                new UnityPlatform
                {
                    Id = "UWP",
                    DisplayName = "Universal Windows Platform",
                    FolderName = "MetroSupport"
                },
                new UnityPlatform
                {
                    Id = "tvOS",
                    DisplayName = "tvOS",
                    FolderName = "AppleTVSupport"
                }
            };
        }
    }
}
