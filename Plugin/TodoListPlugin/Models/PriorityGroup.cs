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
        /// 优先级颜色
        /// </summary>
        public string Color { get; set; } = "#808080";

        /// <summary>
        /// 优先级级别（数值越小优先级越高）
        /// </summary>
        public int Level { get; set; } = 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}

