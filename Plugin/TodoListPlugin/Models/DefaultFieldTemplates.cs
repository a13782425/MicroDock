using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoListPlugin.Models
{
    /// <summary>
    /// 默认字段模板定义（硬编码，不保存到文件）
    /// </summary>
    public static class DefaultFieldTemplates
    {
        /// <summary>
        /// 获取所有默认字段模板
        /// </summary>
        public static List<CustomFieldTemplate> GetAll(List<PriorityGroup> priorities, List<TagGroup> tags)
        {
            return new List<CustomFieldTemplate>
            {
                GetTitleField(),
                GetDescriptionField(),
                GetPriorityField(priorities),
                GetTagsField(tags)
            };
        }

        /// <summary>
        /// 获取标题字段
        /// </summary>
        public static CustomFieldTemplate GetTitleField()
        {
            return new CustomFieldTemplate
            {
                Id = "builtin-title",
                Name = "标题",
                FieldType = FieldType.Text,
                IsFilterable = true,
                IsDefault = true,
                Order = -2
            };
        }

        /// <summary>
        /// 获取简述字段
        /// </summary>
        public static CustomFieldTemplate GetDescriptionField()
        {
            return new CustomFieldTemplate
            {
                Id = "builtin-description",
                Name = "简述",
                FieldType = FieldType.Text,
                IsFilterable = true,
                IsDefault = true,
                Order = -1
            };
        }

        /// <summary>
        /// 获取优先级字段
        /// </summary>
        public static CustomFieldTemplate GetPriorityField(List<PriorityGroup> priorities)
        {
            return new CustomFieldTemplate
            {
                Id = "default-priority",
                Name = "优先级",
                FieldType = FieldType.Select,
                IsFilterable = true,
                IsDefault = true,
                Order = 0,
                Options = new List<string>(priorities.Select(p => p.Name))
            };
        }

        /// <summary>
        /// 获取标签字段
        /// </summary>
        public static CustomFieldTemplate GetTagsField(List<TagGroup> tags)
        {
            return new CustomFieldTemplate
            {
                Id = "default-tags",
                Name = "标签",
                FieldType = FieldType.Select,
                IsFilterable = true,
                IsDefault = true,
                Order = 1,
                Options = new List<string>(tags.Select(t => t.Name))
            };
        }

        /// <summary>
        /// 检查是否为默认字段ID
        /// </summary>
        public static bool IsDefaultFieldId(string fieldId)
        {
            return fieldId == "builtin-title" || 
                   fieldId == "builtin-description" || 
                   fieldId == "default-priority" || 
                   fieldId == "default-tags";
        }
    }
}

