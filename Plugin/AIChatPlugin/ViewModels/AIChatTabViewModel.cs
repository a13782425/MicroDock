using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using AIChatPlugin.Models;
using AIChatPlugin.Services;
using ReactiveUI;

namespace AIChatPlugin.ViewModels
{
    /// <summary>
    /// AI 对话标签页视图模型
    /// </summary>
    public class AIChatTabViewModel : ReactiveObject
    {
        private readonly AIChatPlugin _plugin;
        private string _inputText = string.Empty;
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;
        private ChatConversation? _currentConversation;
        private CancellationTokenSource? _currentCancellationTokenSource;

        public AIChatTabViewModel(AIChatPlugin plugin)
        {
            _plugin = plugin;
            Messages = new ObservableCollection<ChatMessage>();
            Conversations = new ObservableCollection<ChatConversation>();

            SendMessageCommand = ReactiveCommand.CreateFromTask(SendMessageAsync, 
                this.WhenAnyValue(x => x.IsLoading, x => x.InputText, 
                    (loading, text) => !loading && !string.IsNullOrWhiteSpace(text)));
            
            ClearMessagesCommand = ReactiveCommand.Create(ClearMessages);
            CreateNewConversationCommand = ReactiveCommand.Create(CreateNewConversation);
        }

        /// <summary>
        /// 消息列表
        /// </summary>
        public ObservableCollection<ChatMessage> Messages { get; }

        /// <summary>
        /// 对话列表
        /// </summary>
        public ObservableCollection<ChatConversation> Conversations { get; }

        /// <summary>
        /// 输入文本
        /// </summary>
        public string InputText
        {
            get => _inputText;
            set => this.RaiseAndSetIfChanged(ref _inputText, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        /// <summary>
        /// 当前对话
        /// </summary>
        public ChatConversation? CurrentConversation
        {
            get => _currentConversation;
            set => this.RaiseAndSetIfChanged(ref _currentConversation, value);
        }

        /// <summary>
        /// 发送消息命令
        /// </summary>
        public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }

        /// <summary>
        /// 清空消息命令
        /// </summary>
        public ReactiveCommand<Unit, Unit> ClearMessagesCommand { get; }

        /// <summary>
        /// 创建新对话命令
        /// </summary>
        public ReactiveCommand<Unit, Unit> CreateNewConversationCommand { get; }

        /// <summary>
        /// 发送消息
        /// </summary>
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText) || IsLoading)
            {
                return;
            }

            string userMessage = InputText.Trim();
            InputText = string.Empty;
            ErrorMessage = string.Empty;

            // 确保有当前对话
            if (CurrentConversation == null)
            {
                CreateNewConversation();
            }

            // 添加用户消息
            ChatMessage userMsg = new ChatMessage
            {
                Content = userMessage,
                Role = MessageRole.User,
                ConversationId = CurrentConversation!.Id,
                Timestamp = DateTime.Now
            };
            Messages.Add(userMsg);

            // 创建 AI 回复消息（流式更新）
            ChatMessage aiMsg = new ChatMessage
            {
                Content = string.Empty,
                Role = MessageRole.Assistant,
                ConversationId = CurrentConversation!.Id,
                IsStreaming = true,
                Timestamp = DateTime.Now
            };
            Messages.Add(aiMsg);

            // 发送到 AI
            IsLoading = true;
            _currentCancellationTokenSource = new CancellationTokenSource();

            try
            {
                ChatConfig config = _plugin.GetConfig();
                AIService aiService = new AIService(config);

                // 构建消息历史（转换为 API 格式）
                List<ChatMessage> history = Messages
                    .Where(m => m.Role != MessageRole.System && !m.IsStreaming)
                    .TakeLast(20) // 保留最近 20 条消息
                    .ToList();

                // 获取可用工具
                List<object> tools = GetAvailableTools();

                // 流式发送
                Progress<string> progress = new Progress<string>(content =>
                {
                    aiMsg.Content = content;
                    aiMsg.StreamedContent = content;
                    this.RaisePropertyChanged(nameof(Messages));
                });

                Services.StreamResponse streamResponse = await aiService.SendMessageStreamAsync(
                    history,
                    progress,
                    _currentCancellationTokenSource.Token,
                    tools.Count > 0 ? tools : null);

                // 流式完成
                aiMsg.IsStreaming = false;
                aiMsg.Content = streamResponse.Content;
                aiMsg.StreamedContent = streamResponse.Content;

                // 处理工具调用
                if (streamResponse.ToolCalls != null && streamResponse.ToolCalls.Count > 0)
                {
                    aiMsg.ToolCalls = streamResponse.ToolCalls;
                    
                    // 执行工具调用
                    Services.ToolCallHandler toolHandler = new Services.ToolCallHandler(_plugin.Context!);
                    List<string> toolResults = await toolHandler.ExecuteToolCallsAsync(streamResponse.ToolCalls);

                    // 添加工具结果消息
                    for (int i = 0; i < streamResponse.ToolCalls.Count && i < toolResults.Count; i++)
                    {
                        ChatMessage toolMsg = new ChatMessage
                        {
                            Content = toolResults[i],
                            Role = MessageRole.Tool,
                            ConversationId = CurrentConversation!.Id,
                            ToolCallId = streamResponse.ToolCalls[i].Id,
                            Timestamp = DateTime.Now
                        };
                        Messages.Add(toolMsg);
                    }

                    // 继续发送包含工具结果的请求
                    List<ChatMessage> updatedHistory = Messages
                        .Where(m => m.Role != MessageRole.System && !m.IsStreaming)
                        .TakeLast(20)
                        .ToList();

                    Services.StreamResponse finalResponse = await aiService.SendMessageStreamAsync(
                        updatedHistory,
                        new Progress<string>(content =>
                        {
                            // 更新最终回复
                            if (Messages.Count > 0 && Messages[Messages.Count - 1].Role == MessageRole.Assistant)
                            {
                                ChatMessage finalMsg = Messages[Messages.Count - 1];
                                finalMsg.Content = content;
                                finalMsg.StreamedContent = content;
                            }
                            else
                            {
                                // 创建新的最终回复消息
                                ChatMessage finalMsg = new ChatMessage
                                {
                                    Content = content,
                                    Role = MessageRole.Assistant,
                                    ConversationId = CurrentConversation!.Id,
                                    IsStreaming = true,
                                    Timestamp = DateTime.Now
                                };
                                Messages.Add(finalMsg);
                            }
                            this.RaisePropertyChanged(nameof(Messages));
                        }),
                        _currentCancellationTokenSource.Token,
                        tools.Count > 0 ? tools : null);

                    // 更新最终消息
                    if (Messages.Count > 0 && Messages[Messages.Count - 1].Role == MessageRole.Assistant)
                    {
                        ChatMessage finalMsg = Messages[Messages.Count - 1];
                        finalMsg.IsStreaming = false;
                        finalMsg.Content = finalResponse.Content;
                        finalMsg.StreamedContent = finalResponse.Content;
                    }
                }

                // 保存对话
                await _plugin.SaveConversationAsync(CurrentConversation!, Messages.ToList());
            }
            catch (OperationCanceledException)
            {
                Messages.Remove(aiMsg);
                ErrorMessage = "请求已取消";
            }
            catch (Exception ex)
            {
                Messages.Remove(aiMsg);
                ErrorMessage = $"错误: {ex.Message}";
                _plugin.Context!.LogError("发送消息失败", ex);
            }
            finally
            {
                IsLoading = false;
                _currentCancellationTokenSource?.Dispose();
                _currentCancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 获取可用工具定义
        /// </summary>
        private List<object> GetAvailableTools()
        {
            List<object> tools = new List<object>();

            try
            {
                List<MicroDock.Plugin.ToolInfo> availableTools = _plugin.Context!.GetAvailableTools();

                foreach (MicroDock.Plugin.ToolInfo toolInfo in availableTools)
                {
                    // 构建工具定义（OpenAI 格式）
                    Dictionary<string, object> toolDef = new Dictionary<string, object>
                    {
                        ["type"] = "function",
                        ["function"] = new Dictionary<string, object>
                        {
                            ["name"] = toolInfo.Name,
                            ["description"] = toolInfo.Description ?? string.Empty,
                            ["parameters"] = new Dictionary<string, object>
                            {
                                ["type"] = "object",
                                ["properties"] = BuildToolParameters(toolInfo),
                                ["required"] = toolInfo.Parameters
                                    .Where(p => p.Required)
                                    .Select(p => p.Name)
                                    .ToList()
                            }
                        }
                    };

                    tools.Add(toolDef);
                }
            }
            catch (Exception ex)
            {
                _plugin.Context!.LogError("获取可用工具失败", ex);
            }

            return tools;
        }

        /// <summary>
        /// 构建工具参数定义
        /// </summary>
        private Dictionary<string, object> BuildToolParameters(MicroDock.Plugin.ToolInfo toolInfo)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            foreach (MicroDock.Plugin.ToolParameterInfo param in toolInfo.Parameters)
            {
                Dictionary<string, object> paramDef = new Dictionary<string, object>
                {
                    ["type"] = MapTypeToJsonType(param.TypeName),
                    ["description"] = param.Description ?? string.Empty
                };

                properties[param.Name] = paramDef;
            }

            return properties;
        }

        /// <summary>
        /// 映射类型到 JSON Schema 类型
        /// </summary>
        private string MapTypeToJsonType(string typeName)
        {
            typeName = typeName.ToLower();
            
            if (typeName.Contains("string"))
                return "string";
            if (typeName.Contains("int") || typeName.Contains("long") || typeName.Contains("short") || typeName.Contains("byte"))
                return "integer";
            if (typeName.Contains("double") || typeName.Contains("float") || typeName.Contains("decimal"))
                return "number";
            if (typeName.Contains("bool"))
                return "boolean";
            
            return "string"; // 默认
        }

        /// <summary>
        /// 清空消息
        /// </summary>
        private void ClearMessages()
        {
            Messages.Clear();
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// 创建新对话
        /// </summary>
        private void CreateNewConversation()
        {
            ChatConversation newConversation = new ChatConversation
            {
                Title = "新对话",
                CreatedTime = DateTime.Now,
                UpdatedTime = DateTime.Now
            };

            CurrentConversation = newConversation;
            Conversations.Add(newConversation);
            Messages.Clear();
        }

        /// <summary>
        /// 加载对话
        /// </summary>
        public async Task LoadConversationAsync(ChatConversation conversation)
        {
            CurrentConversation = conversation;
            Messages.Clear();

            List<ChatMessage> messages = await _plugin.LoadConversationMessagesAsync(conversation.Id);
            foreach (ChatMessage msg in messages)
            {
                Messages.Add(msg);
            }
        }

        /// <summary>
        /// 加载对话列表
        /// </summary>
        public async Task LoadConversationsAsync()
        {
            Conversations.Clear();
            List<ChatConversation> conversations = await _plugin.LoadConversationsAsync();
            foreach (ChatConversation conv in conversations)
            {
                Conversations.Add(conv);
            }
        }
    }
}

