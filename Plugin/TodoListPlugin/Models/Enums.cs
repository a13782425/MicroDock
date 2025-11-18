namespace TodoListPlugin.Models
{
    /// <summary>
    /// 自定义字段类型
    /// </summary>
    public enum FieldType
    {
        /// <summary>
        /// 文本（多行）
        /// </summary>
        Text,

        /// <summary>
        /// 布尔值
        /// </summary>
        Bool,

        /// <summary>
        /// 日期
        /// </summary>
        Date,

        /// <summary>
        /// 数字（支持整数和小数）
        /// </summary>
        Number,

        /// <summary>
        /// 单选项（下拉选择）
        /// </summary>
        Select
    }

    /// <summary>
    /// 筛选操作符
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>
        /// 等于（所有类型）
        /// </summary>
        Equals,

        /// <summary>
        /// 包含（Text）
        /// </summary>
        Contains,

        /// <summary>
        /// 大于（Number, Date）
        /// </summary>
        GreaterThan,

        /// <summary>
        /// 小于（Number, Date）
        /// </summary>
        LessThan,

        /// <summary>
        /// 大于等于（Number, Date）
        /// </summary>
        GreaterOrEqual,

        /// <summary>
        /// 小于等于（Number, Date）
        /// </summary>
        LessOrEqual
    }

    /// <summary>
    /// 通知类型
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 提醒间隔类型
    /// </summary>
    public enum ReminderInterval
    {
        None = 0,
        Every15Minutes = 1,
        Every30Minutes = 2,
        Every1Hour = 3,
        Every2Hours = 4,
        EveryDayAt9AM = 5
    }
}

