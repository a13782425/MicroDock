using System;

namespace TodoListPlugin.Models
{
    /// <summary>
    /// 项目/分组模型
    /// </summary>
    public class Project
    {
        /// <summary>
        /// 项目的唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 项目颜色（格式：#RRGGBB）
        /// </summary>
        public string Color { get; set; } = "#808080";

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否展开
        /// </summary>
        public bool IsExpanded { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 项目图标（可选）
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 项目描述（可选）
        /// </summary>
        public string? Description { get; set; }
    }
}
