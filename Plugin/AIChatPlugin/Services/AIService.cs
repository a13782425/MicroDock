using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AIChatPlugin.Models;
using System.Linq; // Added for .Any()

namespace AIChatPlugin.Services
{
    /// <summary>
    /// AI 服务，用于调用 AI API
    /// </summary>
    public class AIService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ChatConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public AIService(ChatConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = new HttpClient();
            
            // 设置请求头
            if (!string.IsNullOrEmpty(_config.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
            }
            
            // 注意：Content-Type 是内容头，应该在 HttpContent 对象上设置
            // StringContent 构造函数会自动设置 Content-Type，无需在此设置

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// 发送消息（非流式）
        /// </summary>
        public async Task<string> SendMessageAsync(List<ChatMessage> messages, List<object>? tools = null)
        {
            if (string.IsNullOrEmpty(_config.ApiKey))
            {
                throw new InvalidOperationException("API Key 未配置");
            }

            try
            {
                string apiUrl = _config.GetApiUrl();
                
                // 构建请求体
                object requestBody = BuildRequestBody(messages, tools, stream: false);

                // 发送请求
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(apiUrl, requestBody, _jsonOptions);
                response.EnsureSuccessStatusCode();

                // 解析响应
                JsonDocument jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                JsonElement root = jsonDoc.RootElement;
                
                if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                {
                    JsonElement firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out JsonElement message))
                    {
                        // 检查是否有工具调用
                        if (message.TryGetProperty("tool_calls", out JsonElement toolCalls))
                        {
                            // 返回工具调用信息
                            return message.GetRawText();
                        }
                        
                        if (message.TryGetProperty("content", out JsonElement content))
                        {
                            return content.GetString() ?? string.Empty;
                        }
                    }
                }

                throw new Exception("无法解析 AI 响应");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"API 请求失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 发送消息（流式）
        /// </summary>
        public async Task<StreamResponse> SendMessageStreamAsync(
            List<ChatMessage> messages,
            IProgress<string> progress,
            CancellationToken cancellationToken = default,
            List<object>? tools = null)
        {
            if (string.IsNullOrEmpty(_config.ApiKey))
            {
                throw new InvalidOperationException("API Key 未配置");
            }

            try
            {
                string apiUrl = _config.GetApiUrl();
                
                // 构建请求体（流式）
                object requestBody = BuildRequestBody(messages, tools, stream: true);

                // 发送请求（流式读取）
                string jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
                HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = content
                };
                
                HttpResponseMessage response = await _httpClient.SendAsync(
                    request, 
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                
                response.EnsureSuccessStatusCode();

                // 流式读取响应
                using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using StreamReader reader = new StreamReader(stream);

                string? line;
                StringBuilder currentContent = new StringBuilder();
                List<ToolCall> toolCalls = new List<ToolCall>();

                while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                {
                    if (line.StartsWith("data: "))
                    {
                        string json = line.Substring(6).Trim();
                        
                        // 检查是否是结束标记
                        if (json == "[DONE]")
                        {
                            break;
                        }

                        try
                        {
                            JsonDocument doc = JsonDocument.Parse(json);
                            JsonElement root = doc.RootElement;

                            if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                            {
                                JsonElement firstChoice = choices[0];
                                
                                // 检查是否是增量更新
                                if (firstChoice.TryGetProperty("delta", out JsonElement delta))
                                {
                                    // 内容增量
                                    if (delta.TryGetProperty("content", out JsonElement contentElement))
                                    {
                                        string? contentText = contentElement.GetString();
                                        if (!string.IsNullOrEmpty(contentText))
                                        {
                                            currentContent.Append(contentText);
                                            progress.Report(currentContent.ToString());
                                        }
                                    }
                                    
                                    // 工具调用增量
                                    if (delta.TryGetProperty("tool_calls", out JsonElement deltaToolCalls))
                                    {
                                        foreach (JsonElement toolCallElement in deltaToolCalls.EnumerateArray())
                                        {
                                            // 解析工具调用增量
                                            if (toolCallElement.TryGetProperty("index", out JsonElement indexElement))
                                            {
                                                int index = indexElement.GetInt32();
                                                
                                                // 确保有足够的工具调用对象
                                                while (toolCalls.Count <= index)
                                                {
                                                    toolCalls.Add(new ToolCall { Id = string.Empty, Type = "function", Function = new FunctionCall() });
                                                }
                                                
                                                ToolCall toolCall = toolCalls[index];
                                                
                                                if (toolCallElement.TryGetProperty("id", out JsonElement idElement))
                                                {
                                                    toolCall.Id = idElement.GetString() ?? string.Empty;
                                                }
                                                
                                                if (toolCallElement.TryGetProperty("function", out JsonElement functionElement))
                                                {
                                                    if (functionElement.TryGetProperty("name", out JsonElement nameElement))
                                                    {
                                                        toolCall.Function.Name += nameElement.GetString() ?? string.Empty;
                                                    }
                                                    
                                                    if (functionElement.TryGetProperty("arguments", out JsonElement argsElement))
                                                    {
                                                        toolCall.Function.Arguments += argsElement.GetString() ?? string.Empty;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                // 检查是否是完整消息
                                else if (firstChoice.TryGetProperty("message", out JsonElement message))
                                {
                                    if (message.TryGetProperty("content", out JsonElement messageContent))
                                    {
                                        string? contentText = messageContent.GetString();
                                        if (!string.IsNullOrEmpty(contentText))
                                        {
                                            currentContent.Append(contentText);
                                            progress.Report(currentContent.ToString());
                                        }
                                    }
                                    
                                    // 解析完整消息中的工具调用
                                    if (message.TryGetProperty("tool_calls", out JsonElement messageToolCalls))
                                    {
                                        toolCalls = ParseToolCalls(message);
                                    }
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // 忽略 JSON 解析错误，继续处理下一行
                            continue;
                        }
                    }
                }

                return new StreamResponse
                {
                    Content = currentContent.ToString(),
                    ToolCalls = toolCalls
                };
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"API 请求失败: {ex.Message}", ex);
            }
            catch (TaskCanceledException)
            {
                throw new OperationCanceledException("请求已取消");
            }
        }

        /// <summary>
        /// 构建请求体
        /// </summary>
        private object BuildRequestBody(List<ChatMessage> messages, List<object>? tools, bool stream)
        {
            // 添加系统提示（如果没有）
            if (!messages.Any(m => m.Role == MessageRole.System))
            {
                messages.Insert(0, new ChatMessage 
                { 
                    Role = MessageRole.System, 
                    Content = @"你是一个智能助手。请严格按照以下 JSON 格式返回你的回复：

{
  ""think"": ""你的思考过程（可选）"",
  ""content"": ""你的回复内容，使用 Markdown 格式。如果需要插入图表或代码，使用占位符：{{mermaid:id}} 或 {{code:id}}"",
  ""attachments"": {
    ""id"": {
      ""type"": ""mermaid"" 或 ""code"",
      ""content"": ""实际内容"",
      ""language"": ""代码语言（仅 code 类型需要）""
    }
  }
}

注意：
1. 必须返回有效的 JSON
2. content 字段必需，think 和 attachments 可选
3. 如果需要 Mermaid 图表，在 content 中使用 {{mermaid:id}} 占位符，并在 attachments 中提供图表代码
4. 如果需要代码块，在 content 中使用 {{code:id}} 占位符，并在 attachments 中提供代码内容

示例：
{
  ""think"": ""用户询问冒泡排序，需要提供流程图和代码实现"",
  ""content"": ""冒泡排序的工作流程如下：\n\n{{mermaid:flow}}\n\nPython 实现：\n\n{{code:impl}}"",
  ""attachments"": {
    ""flow"": {
      ""type"": ""mermaid"",
      ""content"": ""flowchart TD\n    A[开始] --> B[比较相邻元素]\n    B --> C{需要交换?}\n    C -->|是| D[交换]\n    C -->|否| E[继续]\n    D --> E\n    E --> F{完成?}\n    F -->|否| B\n    F -->|是| G[结束]""
    },
    ""impl"": {
      ""type"": ""code"",
      ""language"": ""python"",
      ""content"": ""def bubble_sort(arr):\n    n = len(arr)\n    for i in range(n):\n        for j in range(0, n-i-1):\n            if arr[j] > arr[j+1]:\n                arr[j], arr[j+1] = arr[j+1], arr[j]\n    return arr""
    }
  }
}"
                });
            }

            // 转换消息格式
            List<object> requestMessages = new List<object>();
            foreach (ChatMessage msg in messages)
            {
                Dictionary<string, object> messageObj = new Dictionary<string, object>
                {
                    ["role"] = msg.Role.ToString().ToLower()
                };

                if (!string.IsNullOrEmpty(msg.Content))
                {
                    messageObj["content"] = msg.Content;
                }

                // 工具调用消息
                if (msg.Role == MessageRole.Tool && !string.IsNullOrEmpty(msg.ToolCallId))
                {
                    messageObj["tool_call_id"] = msg.ToolCallId;
                }

                // 工具调用
                if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                {
                    List<object> toolCalls = new List<object>();
                    foreach (ToolCall toolCall in msg.ToolCalls)
                    {
                        toolCalls.Add(new
                        {
                            id = toolCall.Id,
                            type = toolCall.Type,
                            function = new
                            {
                                name = toolCall.Function.Name,
                                arguments = toolCall.Function.Arguments
                            }
                        });
                    }
                    messageObj["tool_calls"] = toolCalls;
                }

                requestMessages.Add(messageObj);
            }

            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                ["model"] = _config.Model,
                ["messages"] = requestMessages,
                ["temperature"] = _config.Temperature,
                ["max_tokens"] = _config.MaxTokens,
                ["stream"] = stream
            };

            // 添加工具定义
            if (tools != null && tools.Count > 0)
            {
                requestBody["tools"] = tools;
            }

            return requestBody;
        }

        /// <summary>
        /// 解析工具调用响应
        /// </summary>
        public static List<ToolCall> ParseToolCalls(JsonElement messageElement)
        {
            List<ToolCall> toolCalls = new List<ToolCall>();

            if (messageElement.TryGetProperty("tool_calls", out JsonElement toolCallsElement))
            {
                foreach (JsonElement toolCallElement in toolCallsElement.EnumerateArray())
                {
                    ToolCall toolCall = new ToolCall();

                    if (toolCallElement.TryGetProperty("id", out JsonElement idElement))
                    {
                        toolCall.Id = idElement.GetString() ?? string.Empty;
                    }

                    if (toolCallElement.TryGetProperty("type", out JsonElement typeElement))
                    {
                        toolCall.Type = typeElement.GetString() ?? "function";
                    }

                    if (toolCallElement.TryGetProperty("function", out JsonElement functionElement))
                    {
                        FunctionCall functionCall = new FunctionCall();

                        if (functionElement.TryGetProperty("name", out JsonElement nameElement))
                        {
                            functionCall.Name = nameElement.GetString() ?? string.Empty;
                        }

                        if (functionElement.TryGetProperty("arguments", out JsonElement argumentsElement))
                        {
                            functionCall.Arguments = argumentsElement.GetRawText();
                        }

                        toolCall.Function = functionCall;
                    }

                    toolCalls.Add(toolCall);
                }
            }

            return toolCalls;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// 流式响应结果
    /// </summary>
    public class StreamResponse
    {
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 工具调用列表
        /// </summary>
        public List<ToolCall> ToolCalls { get; set; } = new List<ToolCall>();
    }
}

