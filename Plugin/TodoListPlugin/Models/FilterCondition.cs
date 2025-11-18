namespace TodoListPlugin.Models
{
    /// <summary>
    /// 筛选条件模型
    /// </summary>
    public class FilterCondition
    {
        /// <summary>
        /// 字段 ID
        /// </summary>
        public string FieldId { get; set; } = string.Empty;

        /// <summary>
        /// 字段名称（用于显示）
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 字段类型
        /// </summary>
        public FieldType FieldType { get; set; }

        /// <summary>
        /// 筛选值（字符串表示）
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 筛选操作符
        /// </summary>
        public FilterOperator Operator { get; set; }
    }
}

