using System;

namespace TodoListPlugin.Models
{
    /// <summary>
    /// 优先级分组模型
    /// </summary>
    public class PriorityGroup
    {
        /// <summary>
        /// 优先级唯一标识
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 优先级名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}

