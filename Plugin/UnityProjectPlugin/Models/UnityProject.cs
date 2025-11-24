using System;

namespace UnityProjectPlugin.Models
{
    /// <summary>
    /// Unity 项目模型
    /// </summary>
    public class UnityProject
    {
        private string _path = string.Empty;
        private string _id = string.Empty;

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 项目路径
        /// </summary>
        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                // 在设置路径时计算 ID，避免重复计算
                _id = string.IsNullOrEmpty(value)
                    ? string.Empty
                    : System.IO.Path.GetFullPath(value).ToLowerInvariant();
            }
        }

        /// <summary>
        /// 分组名称（可选）
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 使用的 Unity 版本（可选）
        /// </summary>
        public string? UnityVersion { get; set; }

        /// <summary>
        /// 最后打开时间
        /// </summary>
        public DateTime LastOpened { get; set; }

        /// <summary>
        /// 项目唯一标识（基于路径，已缓存）
        /// </summary>
        public string Id => _id;
    }
}

