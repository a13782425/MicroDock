using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ReactiveUI;

namespace AIChatPlugin.Models
{
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

    /// <summary>
    /// 消息类型
    /// </summary>
    public enum MessageType
    {
        Text,
        Image,
        ToolResult
    }

    /// <summary>
    /// 聊天消息视图模型 (重构后)
    /// </summary>
    public class MessageViewModel : ReactiveObject
    {
        private string _rawContent = string.Empty;
        private string _content = string.Empty;
        private bool _isStreaming;
        private string _thinkContent = string.Empty;
        private string _mermaidCode = string.Empty;
        private MessageType _type = MessageType.Text;
        private Avalonia.Media.Imaging.Bitmap? _mermaidImage;
        private bool _isMermaidLoading;

        public MessageViewModel()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// 消息 ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 原始内容（未处理的完整内容）
        /// </summary>
        public string RawContent
        {
            get => _rawContent;
            set
            {
                if (this.RaiseAndSetIfChanged(ref _rawContent, value) == value)
                {
                    // 当原始内容变化时，自动解析
                    ParseContent();
                }
            }
        }

        /// <summary>
        /// 消息内容（解析后的最终显示内容）
        /// </summary>
        public string Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        /// <summary>
        /// 消息角色
        /// </summary>
        public MessageRole Role { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public MessageType Type
        {
            get => _type;
            set => this.RaiseAndSetIfChanged(ref _type, value);
        }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 对话 ID
        /// </summary>
        public string ConversationId { get; set; } = string.Empty;

        /// <summary>
        /// 是否正在流式输出
        /// </summary>
        public bool IsStreaming
        {
            get => _isStreaming;
            set => this.RaiseAndSetIfChanged(ref _isStreaming, value);
        }

        /// <summary>
        /// 工具调用列表
        /// </summary>
        public List<ToolCall> ToolCalls { get; set; } = new List<ToolCall>();

        /// <summary>
        /// 工具调用 ID（用于工具结果消息）
        /// </summary>
        public string? ToolCallId { get; set; }

        /// <summary>
        /// 思考内容（解析自回复）
        /// </summary>
        public string ThinkContent
        {
            get => _thinkContent;
            set => this.RaiseAndSetIfChanged(ref _thinkContent, value);
        }

        /// <summary>
        /// Mermaid 代码（解析自回复）
        /// </summary>
        public string MermaidCode
        {
            get => _mermaidCode;
            set => this.RaiseAndSetIfChanged(ref _mermaidCode, value);
        }

        /// <summary>
        /// Mermaid 图片（转换后的图片）
        /// </summary>
        public Avalonia.Media.Imaging.Bitmap? MermaidImage
        {
            get => _mermaidImage;
            set => this.RaiseAndSetIfChanged(ref _mermaidImage, value);
        }

        /// <summary>
        /// Mermaid 是否正在加载
        /// </summary>
        public bool IsMermaidLoading
        {
            get => _isMermaidLoading;
            set => this.RaiseAndSetIfChanged(ref _isMermaidLoading, value);
        }

        /// <summary>
        /// 兼容旧代码的 StreamedContent 属性
        /// </summary>
        public string StreamedContent
        {
            get => Content;
            set => Content = value;
        }

        /// <summary>
        /// 解析原始内容，支持 JSON 格式
        /// </summary>
        private void ParseContent()
        {
            if (string.IsNullOrEmpty(_rawContent))
            {
                Content = "（空内容）";
                ThinkContent = string.Empty;
                MermaidCode = string.Empty;
                return;
            }

            // 打印调试信息：从 AI 获取的原始数据
            System.Diagnostics.Debug.WriteLine("=== AI 原始响应 ===");
            System.Diagnostics.Debug.WriteLine($"长度: {_rawContent.Length} 字符");
            System.Diagnostics.Debug.WriteLine($"内容:\n{_rawContent}");
            System.Diagnostics.Debug.WriteLine("==================");

            // 尝试解析 JSON
            if (!TryParseJson(_rawContent, out var response, out string error))
            {
                // 解析失败，显示错误
                System.Diagnostics.Debug.WriteLine($"⚠️ JSON 解析失败: {error}");
                Content = $"⚠️ JSON 解析失败\n\n错误: {error}\n\n原始内容:\n```\n{_rawContent}\n```";
                ThinkContent = string.Empty;
                MermaidCode = string.Empty;
                return;
            }

            // 解析成功
            System.Diagnostics.Debug.WriteLine("✅ JSON 解析成功");
            System.Diagnostics.Debug.WriteLine($"Think: {(string.IsNullOrEmpty(response.Think) ? "(无)" : response.Think.Substring(0, Math.Min(50, response.Think.Length)) + "...")}");
            System.Diagnostics.Debug.WriteLine($"Content: {response.Content.Substring(0, Math.Min(100, response.Content.Length))}...");
            System.Diagnostics.Debug.WriteLine($"Attachments: {response.Attachments?.Count ?? 0} 个");
            
            ThinkContent = response.Think ?? string.Empty;
            
            // 处理内容和附件
            ProcessContentWithAttachments(response.Content, response.Attachments);
        }

        /// <summary>
        /// 尝试解析 JSON
        /// </summary>
        private bool TryParseJson(string rawContent, out AiResponse? response, out string error)
        {
            try
            {
                string jsonText = ExtractJsonFromMarkdown(rawContent);
                
                response = System.Text.Json.JsonSerializer.Deserialize<AiResponse>(jsonText,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
                    });
                
                if (response == null)
                {
                    error = "JSON 反序列化返回 null";
                    return false;
                }
                
                if (string.IsNullOrEmpty(response.Content))
                {
                    error = "缺少必需的 'content' 字段";
                    return false;
                }
                
                error = string.Empty;
                return true;
            }
            catch (System.Text.Json.JsonException ex)
            {
                response = null;
                error = $"JSON 格式错误: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                response = null;
                error = $"未知错误: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 从 Markdown 代码块中提取 JSON
        /// </summary>
        private string ExtractJsonFromMarkdown(string content)
        {
            string trimmed = content.Trim();
            
            // 移除可能的 markdown 代码块标记
            if (trimmed.StartsWith("```json"))
            {
                var match = Regex.Match(trimmed, @"```json\s*\n?(.*?)\n?```", 
                    RegexOptions.Singleline);
                if (match.Success)
                    return match.Groups[1].Value.Trim();
            }
            else if (trimmed.StartsWith("```"))
            {
                var match = Regex.Match(trimmed, @"```\s*\n?(.*?)\n?```", 
                    RegexOptions.Singleline);
                if (match.Success)
                    return match.Groups[1].Value.Trim();
            }
            
            return trimmed;
        }

        /// <summary>
        /// 处理内容和附件（替换占位符）
        /// </summary>
        private void ProcessContentWithAttachments(string content, 
            Dictionary<string, ContentAttachment>? attachments)
        {
            if (attachments == null || attachments.Count == 0)
            {
                Content = content;
                System.Diagnostics.Debug.WriteLine("无附件，直接使用 content");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"开始处理 {attachments.Count} 个附件:");
            
            string processedContent = content;
            bool hasMermaid = false;
            
            // 替换占位符
            foreach (var (id, attachment) in attachments)
            {
                System.Diagnostics.Debug.WriteLine($"  - 附件 '{id}': 类型={attachment.Type}, 内容长度={attachment.Content.Length}");
                
                // 支持多种占位符格式
                string[] placeholders = new[]
                {
                    $"{{{{{attachment.Type}:{id}}}}}",  // {{type:id}}
                    $"{{{{{attachment.Type.ToLower()}:{id}}}}}",  // {{type:id}} (小写)
                };
                
                string replacement = "";
                
                switch (attachment.Type.ToLower())
                {
                    case "mermaid":
                        if (!hasMermaid)
                        {
                            MermaidCode = attachment.Content;
                            hasMermaid = true;
                            System.Diagnostics.Debug.WriteLine($"    → 设置 MermaidCode (长度: {attachment.Content.Length})");
                        }
                        replacement = "\n\n[📊 Mermaid 图表]\n\n";
                        break;
                        
                    case "code":
                        replacement = $"\n\n```{attachment.Language ?? "text"}\n{attachment.Content}\n```\n\n";
                        System.Diagnostics.Debug.WriteLine($"    → 代码块 (语言: {attachment.Language ?? "text"})");
                        break;
                        
                    case "latex":
                        replacement = $"\n\n$$\n{attachment.Content}\n$$\n\n";
                        System.Diagnostics.Debug.WriteLine($"    → LaTeX 公式");
                        break;
                        
                    case "image":
                        replacement = $"\n\n![{attachment.Title ?? "图片"}]({attachment.Content})\n\n";
                        System.Diagnostics.Debug.WriteLine($"    → 图片: {attachment.Title ?? "无标题"}");
                        break;
                        
                    default:
                        replacement = $"\n\n[未知类型: {attachment.Type}]\n\n";
                        System.Diagnostics.Debug.WriteLine($"    ⚠️ 未知类型: {attachment.Type}");
                        break;
                }
                
                // 替换所有格式的占位符
                bool replaced = false;
                foreach (var placeholder in placeholders)
                {
                    if (processedContent.Contains(placeholder))
                    {
                        processedContent = processedContent.Replace(placeholder, replacement);
                        replaced = true;
                        System.Diagnostics.Debug.WriteLine($"    ✓ 替换占位符: {placeholder}");
                    }
                }
                
                if (!replaced)
                {
                    System.Diagnostics.Debug.WriteLine($"    ⚠️ 未找到占位符: {placeholders[0]}");
                }
            }
            
            Content = processedContent.Trim();
            System.Diagnostics.Debug.WriteLine($"最终 Content 长度: {Content.Length}");
        }
    }

    /// <summary>
    /// 聊天消息模型 (别名，兼容旧代码)
    /// </summary>
    public class ChatMessage : MessageViewModel
    {
    }
}


