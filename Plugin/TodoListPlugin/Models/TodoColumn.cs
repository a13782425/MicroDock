using System;

namespace TodoListPlugin.Models
{
    /// <summary>
    /// 待办列/页签模型
    /// </summary>
    public class TodoColumn
    {
        /// <summary>
        /// 列的唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 列名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 列的颜色（可选，格式：#RRGGBB）
        /// </summary>
        public string? Color { get; set; }
    }
}

