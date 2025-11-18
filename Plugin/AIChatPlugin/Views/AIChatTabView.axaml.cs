using AIChatPlugin.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MicroDock.Plugin;

namespace AIChatPlugin.Views
{
    /// <summary>
    /// AI 对话标签页视图
    /// </summary>
    public partial class AIChatTabView : UserControl, IMicroTab
    {
        private readonly AIChatPlugin _plugin;
        private AIChatTabViewModel? _viewModel;
        private ScrollViewer? _messagesScrollViewer;

        public AIChatTabView(AIChatPlugin plugin)
        {
            _plugin = plugin;
            InitializeComponent();
            DataContext = _viewModel = new AIChatTabViewModel(plugin);
            
            // 初始化控件引用
            _messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");
            
            // 订阅消息变化，自动滚动到底部
            if (_viewModel != null)
            {
                _viewModel.Messages.CollectionChanged += (s, e) =>
                {
                    ScrollToBottom();
                };
                
                // 订阅当前对话变化
                _viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(_viewModel.CurrentConversation))
                    {
                        LoadConversationMessagesAsync();
                    }
                };
            }
            
            // 加载对话列表
            LoadConversationsAsync();
        }
        
        /// <summary>
        /// 加载对话消息
        /// </summary>
        private async System.Threading.Tasks.Task LoadConversationMessagesAsync()
        {
            if (_viewModel != null && _viewModel.CurrentConversation != null)
            {
                await _viewModel.LoadConversationAsync(_viewModel.CurrentConversation);
            }
        }

        public string TabName => "AI 对话";

        public IconSymbolEnum IconSymbol => IconSymbolEnum.Message;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// 滚动到底部
        /// </summary>
        private void ScrollToBottom()
        {
            if (_messagesScrollViewer != null)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _messagesScrollViewer.ScrollToEnd();
                }, Avalonia.Threading.DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// 加载对话列表
        /// </summary>
        private async System.Threading.Tasks.Task LoadConversationsAsync()
        {
            if (_viewModel != null)
            {
                await _viewModel.LoadConversationsAsync();
                
                // 如果有对话，默认选择第一个
                if (_viewModel.Conversations.Count > 0 && _viewModel.CurrentConversation == null)
                {
                    _viewModel.CurrentConversation = _viewModel.Conversations[0];
                    await LoadConversationMessagesAsync();
                }
            }
        }

        /// <summary>
        /// 刷新配置
        /// </summary>
        public void RefreshConfig()
        {
            // 配置更新时，可以重新创建服务等
        }

        /// <summary>
        /// 获取插件实例
        /// </summary>
        public AIChatPlugin GetPlugin()
        {
            return _plugin;
        }
    }
}

