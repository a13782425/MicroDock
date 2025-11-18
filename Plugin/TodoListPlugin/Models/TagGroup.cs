using System;

namespace TodoListPlugin.Models
{
    /// <summary>
    /// 标签分组模型
    /// </summary>
    public class TagGroup
    {
        /// <summary>
        /// 标签唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 标签名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}

