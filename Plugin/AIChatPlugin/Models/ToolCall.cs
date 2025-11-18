namespace AIChatPlugin.Models
{
    /// <summary>
    /// 工具调用模型
    /// </summary>
    public class ToolCall
    {
        /// <summary>
        /// 工具调用 ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 工具调用类型（通常是 "function"）
        /// </summary>
        public string Type { get; set; } = "function";

        /// <summary>
        /// 函数调用信息
        /// </summary>
        public FunctionCall Function { get; set; } = new FunctionCall();
    }

    /// <summary>
    /// 函数调用模型
    /// </summary>
    public class FunctionCall
    {
        /// <summary>
        /// 函数名称（工具名称）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 函数参数（JSON 字符串）
        /// </summary>
        public string Arguments { get; set; } = string.Empty;
    }
}

