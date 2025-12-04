using System.Collections.Generic;

namespace TodoListPlugin.Models
{
    /// <summary>
    /// 插件全局设置 - 存储所有项目共享的配置
    /// 包括状态列定义、字段模板、优先级、标签等
    /// </summary>
    public class PluginSettings
    {
        /// <summary>
        /// 状态列列表（全局共享，所有项目使用相同的状态定义）
        /// </summary>
        public List<StatusColumn> StatusColumns { get; set; } = new List<StatusColumn>();

        /// <summary>
        /// 自定义字段模板列表（全局共享）
        /// </summary>
        public List<CustomFieldTemplate> FieldTemplates { get; set; } = new List<CustomFieldTemplate>();

        /// <summary>
        /// 优先级列表（全局共享）
        /// </summary>
        public List<PriorityGroup> Priorities { get; set; } = new List<PriorityGroup>();

        /// <summary>
        /// 标签列表（全局共享）
        /// </summary>
        public List<TagGroup> Tags { get; set; } = new List<TagGroup>();

        /// <summary>
        /// 创建默认设置
        /// </summary>
        public static PluginSettings CreateDefault()
        {
            return new PluginSettings
            {
                StatusColumns = new List<StatusColumn>
                {
                    new StatusColumn { Name = "待办", Color = "#FF6B6B", Order = 0, IsDefault = true, Icon = "List" },
                    new StatusColumn { Name = "进行中", Color = "#4ECDC4", Order = 1, Icon = "Play" },
                    new StatusColumn { Name = "已完成", Color = "#95E1A3", Order = 2, Icon = "Checkmark" }
                },
                FieldTemplates = new List<CustomFieldTemplate>
                {
                    // 内置字段：描述
                    new CustomFieldTemplate 
                    { 
                        Id = "builtin_description", 
                        Name = "描述", 
                        FieldType = FieldType.Text, 
                        IsDefault = true, 
                        ShowOnCard = true, 
                        Order = 0 
                    },
                    // 内置字段：优先级
                    new CustomFieldTemplate 
                    { 
                        Id = "builtin_priority", 
                        Name = "优先级", 
                        FieldType = FieldType.Select, 
                        IsDefault = true, 
                        ShowOnCard = true, 
                        Order = 1 
                    },
                    // 内置字段：截止日期
                    new CustomFieldTemplate 
                    { 
                        Id = "builtin_duedate", 
                        Name = "截止日期", 
                        FieldType = FieldType.Date, 
                        IsDefault = true, 
                        ShowOnCard = true, 
                        Order = 2 
                    },
                    // 内置字段：标签
                    new CustomFieldTemplate 
                    { 
                        Id = "builtin_tags", 
                        Name = "标签", 
                        FieldType = FieldType.Text, 
                        IsDefault = true, 
                        ShowOnCard = false, 
                        Order = 3 
                    }
                },
                Priorities = new List<PriorityGroup>
                {
                    new PriorityGroup { Name = "高", Color = "#FF4757" },
                    new PriorityGroup { Name = "中", Color = "#FFA502" },
                    new PriorityGroup { Name = "低", Color = "#2ED573" }
                },
                Tags = new List<TagGroup>()
            };
        }
    }
}
