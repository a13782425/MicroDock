using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AIChatPlugin.Models
{
    /// <summary>
    /// AI 响应模型（JSON 格式）
    /// </summary>
    public class AiResponse
    {
        /// <summary>
        /// 思考过程（可选）
        /// </summary>
        [JsonPropertyName("think")]
        public string? Think { get; set; }
        
        /// <summary>
        /// 主要内容（使用 Markdown 格式，支持占位符）
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 附件（图表、代码等）
        /// </summary>
        [JsonPropertyName("attachments")]
        public Dictionary<string, ContentAttachment>? Attachments { get; set; }
    }
    
    /// <summary>
    /// 内容附件（图表、代码等）
    /// </summary>
    public class ContentAttachment
    {
        /// <summary>
        /// 附件类型（mermaid, code, latex, image 等）
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";
        
        /// <summary>
        /// 附件内容
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 代码语言（仅 code 类型需要）
        /// </summary>
        [JsonPropertyName("language")]
        public string? Language { get; set; }
        
        /// <summary>
        /// 标题（可选）
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }
}

