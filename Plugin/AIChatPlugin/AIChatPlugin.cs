using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AIChatPlugin.Models;
using AIChatPlugin.Views;
using Avalonia.Controls;
using MicroDock.Plugin;

namespace AIChatPlugin
{
    /// <summary>
    /// AI 对话插件
    /// </summary>
    public class AIChatPlugin : BaseMicroDockPlugin
    {
        private AIChatTabView? _tabView;
        private ChatSettingsView? _settingsView;
        private ChatConfig _config = new ChatConfig();
        private string _dataFolder = string.Empty;
        private readonly JsonSerializerOptions _jsonOptions;

        public AIChatPlugin()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        public override IMicroTab[] Tabs
        {
            get
            {
                if (_tabView == null)
                {
                    _tabView = new AIChatTabView(this);
                }
                return new IMicroTab[] { _tabView };
            }
        }

        public override object? GetSettingsControl()
        {
            if (_settingsView == null)
            {
                _settingsView = new ChatSettingsView(this);
            }
            return _settingsView;
        }

        public override void OnInit()
        {
            base.OnInit();
            Context!.LogInfo("AI 对话插件初始化中...");

            // 初始化数据文件夹
            string? dataPath = Context?.DataPath;
            if (string.IsNullOrEmpty(dataPath))
            {
                Context!.LogError("无法获取插件数据文件夹路径");
                return;
            }

            _dataFolder = dataPath;

            // 确保数据文件夹存在
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
                Context!.LogInfo($"创建数据文件夹: {_dataFolder}");
            }

            // 加载配置
            LoadConfig();

            Context!.LogInfo("AI 对话插件初始化完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Context!.LogInfo("AI 对话插件已销毁");
        }

        #region 配置管理

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfig()
        {
            try
            {
                // 优先从设置中读取
                string? apiKey = GetSettings("api_key");
                string? baseUrl = GetSettings("base_url");
                string? model = GetSettings("model");
                string? temperature = GetSettings("temperature");
                string? maxTokens = GetSettings("max_tokens");

                if (!string.IsNullOrEmpty(apiKey))
                {
                    _config.ApiKey = apiKey;
                }
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    _config.BaseUrl = baseUrl;
                }
                if (!string.IsNullOrEmpty(model))
                {
                    _config.Model = model;
                }
                if (!string.IsNullOrEmpty(temperature) && double.TryParse(temperature, out double temp))
                {
                    _config.Temperature = temp;
                }
                if (!string.IsNullOrEmpty(maxTokens) && int.TryParse(maxTokens, out int max))
                {
                    _config.MaxTokens = max;
                }

                Context!.LogInfo("已加载配置");
            }
            catch (Exception ex)
            {
                Context!.LogError("加载配置失败", ex);
                _config = new ChatConfig();
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                SetSettings("api_key", _config.ApiKey, "AI API 密钥");
                SetSettings("base_url", _config.BaseUrl, "API 基础地址");
                SetSettings("model", _config.Model, "模型名称");
                SetSettings("temperature", _config.Temperature.ToString(), "温度参数");
                SetSettings("max_tokens", _config.MaxTokens.ToString(), "最大 Token 数");

                Context!.LogInfo("配置已保存");
            }
            catch (Exception ex)
            {
                Context!.LogError("保存配置失败", ex);
            }
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        public ChatConfig GetConfig()
        {
            return _config;
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        public void UpdateConfig(ChatConfig config)
        {
            _config = config;
            SaveConfig();
            
            // 通知视图更新
            _tabView?.RefreshConfig();
        }

        #endregion

        #region 对话数据管理

        /// <summary>
        /// 保存对话
        /// </summary>
        public async Task SaveConversationAsync(ChatConversation conversation, List<ChatMessage> messages)
        {
            try
            {
                if (string.IsNullOrEmpty(_dataFolder))
                {
                    Context!.LogWarning("数据文件夹未初始化，无法保存对话");
                    return;
                }

                // 更新对话信息
                conversation.UpdatedTime = DateTime.Now;
                conversation.MessageCount = messages.Count;

                // 保存对话元数据
                string conversationsFile = Path.Combine(_dataFolder, "conversations.json");
                List<ChatConversation> conversations = await LoadConversationsAsync();
                
                ChatConversation? existing = conversations.FirstOrDefault(c => c.Id == conversation.Id);
                if (existing != null)
                {
                    int index = conversations.IndexOf(existing);
                    conversations[index] = conversation;
                }
                else
                {
                    conversations.Insert(0, conversation);
                }

                string conversationsJson = JsonSerializer.Serialize(conversations, _jsonOptions);
                await File.WriteAllTextAsync(conversationsFile, conversationsJson);

                // 保存消息
                string messagesFile = Path.Combine(_dataFolder, $"messages_{conversation.Id}.json");
                string messagesJson = JsonSerializer.Serialize(messages, _jsonOptions);
                await File.WriteAllTextAsync(messagesFile, messagesJson);

                Context!.LogDebug($"已保存对话: {conversation.Title}");
            }
            catch (Exception ex)
            {
                Context!.LogError("保存对话失败", ex);
            }
        }

        /// <summary>
        /// 加载对话列表
        /// </summary>
        public async Task<List<ChatConversation>> LoadConversationsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_dataFolder))
                {
                    return new List<ChatConversation>();
                }

                string conversationsFile = Path.Combine(_dataFolder, "conversations.json");
                if (File.Exists(conversationsFile))
                {
                    string json = await File.ReadAllTextAsync(conversationsFile);
                    List<ChatConversation>? conversations = JsonSerializer.Deserialize<List<ChatConversation>>(json, _jsonOptions);
                    return conversations ?? new List<ChatConversation>();
                }
            }
            catch (Exception ex)
            {
                Context!.LogError("加载对话列表失败", ex);
            }

            return new List<ChatConversation>();
        }

        /// <summary>
        /// 加载对话消息
        /// </summary>
        public async Task<List<ChatMessage>> LoadConversationMessagesAsync(string conversationId)
        {
            try
            {
                if (string.IsNullOrEmpty(_dataFolder))
                {
                    return new List<ChatMessage>();
                }

                string messagesFile = Path.Combine(_dataFolder, $"messages_{conversationId}.json");
                if (File.Exists(messagesFile))
                {
                    string json = await File.ReadAllTextAsync(messagesFile);
                    List<ChatMessage>? messages = JsonSerializer.Deserialize<List<ChatMessage>>(json, _jsonOptions);
                    return messages ?? new List<ChatMessage>();
                }
            }
            catch (Exception ex)
            {
                Context!.LogError($"加载对话消息失败: {conversationId}", ex);
            }

            return new List<ChatMessage>();
        }

        /// <summary>
        /// 删除对话
        /// </summary>
        public async Task DeleteConversationAsync(string conversationId)
        {
            try
            {
                if (string.IsNullOrEmpty(_dataFolder))
                {
                    return;
                }

                // 删除消息文件
                string messagesFile = Path.Combine(_dataFolder, $"messages_{conversationId}.json");
                if (File.Exists(messagesFile))
                {
                    File.Delete(messagesFile);
                }

                // 从对话列表中移除
                List<ChatConversation> conversations = await LoadConversationsAsync();
                conversations.RemoveAll(c => c.Id == conversationId);

                string conversationsFile = Path.Combine(_dataFolder, "conversations.json");
                string json = JsonSerializer.Serialize(conversations, _jsonOptions);
                await File.WriteAllTextAsync(conversationsFile, json);

                Context!.LogInfo($"已删除对话: {conversationId}");
            }
            catch (Exception ex)
            {
                Context!.LogError($"删除对话失败: {conversationId}", ex);
            }
        }

        #endregion

        #region 工具定义

        /// <summary>
        /// 发送消息到 AI 对话
        /// </summary>
        [MicroTool("aichat.send_message",
            Description = "发送消息到 AI 对话并获取回复",
            ReturnDescription = "AI 回复的 JSON 字符串")]
        public async Task<string> SendMessageTool(
            [ToolParameter("conversation_id", Description = "对话 ID")] string conversationId,
            [ToolParameter("message", Description = "消息内容")] string message)
        {
            try
            {
                Context!.LogInfo($"工具调用: 发送消息到对话 {conversationId}");

                // 加载对话消息
                List<ChatMessage> messages = await LoadConversationMessagesAsync(conversationId);

                // 添加用户消息
                ChatMessage userMsg = new ChatMessage
                {
                    Content = message,
                    Role = MessageRole.User,
                    ConversationId = conversationId,
                    Timestamp = DateTime.Now
                };
                messages.Add(userMsg);

                // 调用 AI
                Services.AIService aiService = new Services.AIService(_config);
                string response = await aiService.SendMessageAsync(messages);

                // 添加 AI 回复
                ChatMessage aiMsg = new ChatMessage
                {
                    Content = response,
                    Role = MessageRole.Assistant,
                    ConversationId = conversationId,
                    Timestamp = DateTime.Now
                };
                messages.Add(aiMsg);

                // 保存对话
                ChatConversation? conversation = (await LoadConversationsAsync())
                    .FirstOrDefault(c => c.Id == conversationId);
                if (conversation != null)
                {
                    await SaveConversationAsync(conversation, messages);
                }

                return JsonSerializer.Serialize(new { success = true, response = response });
            }
            catch (Exception ex)
            {
                Context!.LogError("工具调用失败: send_message", ex);
                return JsonSerializer.Serialize(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 获取对话列表
        /// </summary>
        [MicroTool("aichat.get_conversations",
            Description = "获取所有对话列表",
            ReturnDescription = "对话列表的 JSON 字符串")]
        public async Task<string> GetConversationsTool()
        {
            try
            {
                List<ChatConversation> conversations = await LoadConversationsAsync();
                return JsonSerializer.Serialize(conversations, _jsonOptions);
            }
            catch (Exception ex)
            {
                Context!.LogError("工具调用失败: get_conversations", ex);
                return JsonSerializer.Serialize(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 创建新对话
        /// </summary>
        [MicroTool("aichat.create_conversation",
            Description = "创建新的对话",
            ReturnDescription = "新对话的 JSON 字符串")]
        public async Task<string> CreateConversationTool(
            [ToolParameter("title", Description = "对话标题", Required = false)] string? title = null)
        {
            try
            {
                ChatConversation conversation = new ChatConversation
                {
                    Title = title ?? "新对话",
                    CreatedTime = DateTime.Now,
                    UpdatedTime = DateTime.Now
                };

                // 保存对话
                List<ChatConversation> conversations = await LoadConversationsAsync();
                conversations.Insert(0, conversation);
                
                if (!string.IsNullOrEmpty(_dataFolder))
                {
                    string conversationsFile = Path.Combine(_dataFolder, "conversations.json");
                    string json = JsonSerializer.Serialize(conversations, _jsonOptions);
                    await File.WriteAllTextAsync(conversationsFile, json);
                }

                return JsonSerializer.Serialize(conversation, _jsonOptions);
            }
            catch (Exception ex)
            {
                Context!.LogError("工具调用失败: create_conversation", ex);
                return JsonSerializer.Serialize(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 删除对话
        /// </summary>
        [MicroTool("aichat.delete_conversation",
            Description = "删除指定的对话",
            ReturnDescription = "操作结果的 JSON 字符串")]
        public async Task<string> DeleteConversationTool(
            [ToolParameter("conversation_id", Description = "对话 ID")] string conversationId)
        {
            try
            {
                await DeleteConversationAsync(conversationId);
                return JsonSerializer.Serialize(new { success = true, message = "对话已删除" });
            }
            catch (Exception ex)
            {
                Context!.LogError("工具调用失败: delete_conversation", ex);
                return JsonSerializer.Serialize(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 获取配置信息
        /// </summary>
        [MicroTool("aichat.get_config",
            Description = "获取 AI 配置信息（不包含敏感信息）",
            ReturnDescription = "配置信息的 JSON 字符串")]
        public async Task<string> GetConfigTool()
        {
            await Task.CompletedTask;
            try
            {
                return JsonSerializer.Serialize(new
                {
                    baseUrl = _config.BaseUrl,
                    model = _config.Model,
                    temperature = _config.Temperature,
                    maxTokens = _config.MaxTokens,
                    hasApiKey = !string.IsNullOrEmpty(_config.ApiKey)
                }, _jsonOptions);
            }
            catch (Exception ex)
            {
                Context!.LogError("工具调用失败: get_config", ex);
                return JsonSerializer.Serialize(new { success = false, error = ex.Message });
            }
        }

        #endregion
    }
}

