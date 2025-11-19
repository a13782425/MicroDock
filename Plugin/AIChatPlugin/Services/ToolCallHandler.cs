using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AIChatPlugin.Models;
using MicroDock.Plugin;

namespace AIChatPlugin.Services
{
    /// <summary>
    /// 工具调用处理器
    /// </summary>
    public class ToolCallHandler
    {
        private readonly IPluginContext _context;

        public ToolCallHandler(IPluginContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 执行工具调用
        /// </summary>
        public async Task<string> ExecuteToolCallAsync(ToolCall toolCall)
        {
            try
            {
                _context.LogInfo($"执行工具调用: {toolCall.Function.Name}");

                // 解析工具参数
                Dictionary<string, string> parameters = ParseToolParameters(toolCall.Function.Arguments);

                // 调用工具（通过插件框架）
                string result = await _context.CallToolAsync(toolCall.Function.Name, parameters);

                _context.LogInfo($"工具调用成功: {toolCall.Function.Name}");
                return result;
            }
            catch (Exception ex)
            {
                _context.LogError($"工具调用失败: {toolCall.Function.Name}", ex);
                
                // 返回错误信息 JSON
                return JsonSerializer.Serialize(new
                {
                    error = true,
                    message = ex.Message,
                    toolName = toolCall.Function.Name
                });
            }
        }

        /// <summary>
        /// 批量执行工具调用
        /// </summary>
        public async Task<List<string>> ExecuteToolCallsAsync(List<ToolCall> toolCalls)
        {
            List<string> results = new List<string>();

            foreach (ToolCall toolCall in toolCalls)
            {
                string result = await ExecuteToolCallAsync(toolCall);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// 解析工具参数（从 JSON 字符串）
        /// </summary>
        private Dictionary<string, string> ParseToolParameters(string argumentsJson)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            try
            {
                JsonDocument doc = JsonDocument.Parse(argumentsJson);
                JsonElement root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty property in root.EnumerateObject())
                    {
                        // 将值转换为字符串
                        string value = property.Value.ValueKind switch
                        {
                            JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                            JsonValueKind.Number => property.Value.GetRawText(),
                            JsonValueKind.True => "true",
                            JsonValueKind.False => "false",
                            JsonValueKind.Null => string.Empty,
                            _ => property.Value.GetRawText()
                        };

                        parameters[property.Name] = value;
                    }
                }
            }
            catch (JsonException ex)
            {
                _context.LogError($"解析工具参数失败: {argumentsJson}", ex);
                throw new ArgumentException("工具参数格式错误", nameof(argumentsJson), ex);
            }

            return parameters;
        }
    }
}


