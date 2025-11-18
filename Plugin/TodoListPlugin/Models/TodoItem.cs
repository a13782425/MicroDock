using System;
using System.Collections.Generic;

namespace TodoListPlugin.Models
{
    /// <summary>
    /// 待办事项模型
    /// </summary>
    public class TodoItem
    {
        /// <summary>
        /// 待办事项的唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 简述/描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 所属列的 ID
        /// </summary>
        public string ColumnId { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 提醒间隔类型
        /// </summary>
        public ReminderInterval ReminderIntervalType { get; set; } = ReminderInterval.None;

        /// <summary>
        /// 上次提醒时间
        /// </summary>
        public DateTime? LastReminderTime { get; set; }

        /// <summary>
        /// 是否启用提醒
        /// </summary>
        public bool IsReminderEnabled { get; set; }

        /// <summary>
        /// 优先级名称（可选）
        /// </summary>
        public string? PriorityName { get; set; }

        /// <summary>
        /// 标签列表（多个标签）
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 自定义字段值（字段ID -> 值的字符串表示）
        /// </summary>
        public Dictionary<string, string> CustomFields { get; set; } = new Dictionary<string, string>();
    }
}

