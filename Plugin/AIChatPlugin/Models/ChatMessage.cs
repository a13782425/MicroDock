using System;

namespace AIChatPlugin.Models
{
    /// <summary>
    /// 聊天消息模型
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// 消息 ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 消息角色
        /// </summary>
        public MessageRole Role { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 对话 ID
        /// </summary>
        public string ConversationId { get; set; } = string.Empty;

        /// <summary>
        /// 是否正在流式输出
        /// </summary>
        public bool IsStreaming { get; set; }

        /// <summary>
        /// 流式内容（用于实时更新）
        /// </summary>
        public string StreamedContent { get; set; } = string.Empty;

        /// <summary>
        /// 工具调用列表
        /// </summary>
        public List<ToolCall> ToolCalls { get; set; } = new List<ToolCall>();

        /// <summary>
        /// 工具调用 ID（用于工具结果消息）
        /// </summary>
        public string? ToolCallId { get; set; }
    }

    /// <summary>
    /// 消息角色
    /// </summary>
    public enum MessageRole
    {
        User,
        Assistant,
        System,
        Tool
    }
}

