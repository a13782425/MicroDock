using System;

namespace TodoListPlugin.Models
{
    /// <summary>
    /// 状态列模型 - 表示看板中的一个状态（如：待办、进行中、已完成）
    /// 状态列是全局共享的，所有项目使用相同的状态定义
    /// </summary>
    public class StatusColumn
    {
        /// <summary>
        /// 状态列的唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 状态名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 状态颜色（格式：#RRGGBB）
        /// </summary>
        public string Color { get; set; } = "#808080";

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 图标（可选，FluentAvalonia Symbol 名称）
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 是否为默认状态（新建待办事项时使用）
        /// </summary>
        public bool IsDefault { get; set; }
    }
}
