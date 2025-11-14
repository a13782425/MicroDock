using System;

namespace UnityProjectPlugin.Models
{
    /// <summary>
    /// 项目分组模型
    /// </summary>
    public class ProjectGroup
    {
        /// <summary>
        /// 分组唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 分组名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}

