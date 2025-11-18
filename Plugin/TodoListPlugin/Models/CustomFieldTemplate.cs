using System;

namespace TodoListPlugin.Models
{
    /// <summary>
    /// 自定义字段模板
    /// </summary>
    public class CustomFieldTemplate
    {
        /// <summary>
        /// 字段的唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 字段名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 字段类型
        /// </summary>
        public FieldType FieldType { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// 默认值（字符串表示）
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// 是否支持筛选
        /// </summary>
        public bool IsFilterable { get; set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否为默认字段（默认字段不可删除和修改）
        /// </summary>
        public bool IsDefault { get; set; }
    }
}

