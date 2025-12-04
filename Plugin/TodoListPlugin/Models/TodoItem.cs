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
        /// 所属项目的 ID
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// 所属状态列的 ID（如：待办、进行中、已完成）
        /// </summary>
        public string StatusColumnId { get; set; } = string.Empty;

        /// <summary>
        /// 所属列 ID（旧版兼容，现在使用 ProjectId）
        /// </summary>
        public string ColumnId
        {
            get => ProjectId;
            set => ProjectId = value;
        }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 简述/描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedTime { get; set; }

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
        /// 优先级ID（可选）
        /// </summary>
        public string? PriorityId { get; set; }

        /// <summary>
        /// 优先级颜色（缓存，用于UI显示）
        /// </summary>
        public string? PriorityColor { get; set; }

        /// <summary>
        /// 截止日期（可选）
        /// </summary>
        public DateTime? DueDate { get; set; }

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

