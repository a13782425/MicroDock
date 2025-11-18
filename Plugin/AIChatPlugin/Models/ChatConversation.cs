using System;

namespace AIChatPlugin.Models
{
    /// <summary>
    /// 对话模型
    /// </summary>
    public class ChatConversation
    {
        /// <summary>
        /// 对话 ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 对话标题
        /// </summary>
        public string Title { get; set; } = "新对话";

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 消息数量
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// 自动生成标题（基于第一条用户消息）
        /// </summary>
        public static string GenerateTitle(string firstUserMessage)
        {
            if (string.IsNullOrWhiteSpace(firstUserMessage))
            {
                return "新对话";
            }

            // 取前 30 个字符作为标题
            string title = firstUserMessage.Trim();
            if (title.Length > 30)
            {
                title = title.Substring(0, 30) + "...";
            }

            return title;
        }
    }
}

