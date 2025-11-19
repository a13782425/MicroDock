using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using AIChatPlugin.Models;
using ReactiveUI;

namespace AIChatPlugin.ViewModels
{
    /// <summary>
    /// 对话列表视图模型
    /// </summary>
    public class ConversationListViewModel : ReactiveObject
    {
        private ChatConversation? _selectedConversation;

        public ConversationListViewModel()
        {
            Conversations = new ObservableCollection<ChatConversation>();
        }

        /// <summary>
        /// 对话列表
        /// </summary>
        public ObservableCollection<ChatConversation> Conversations { get; }

        /// <summary>
        /// 选中的对话
        /// </summary>
        public ChatConversation? SelectedConversation
        {
            get => _selectedConversation;
            set => this.RaiseAndSetIfChanged(ref _selectedConversation, value);
        }

        /// <summary>
        /// 添加对话
        /// </summary>
        public void AddConversation(ChatConversation conversation)
        {
            Conversations.Insert(0, conversation);
            SelectedConversation = conversation;
        }

        /// <summary>
        /// 删除对话
        /// </summary>
        public void RemoveConversation(ChatConversation conversation)
        {
            Conversations.Remove(conversation);
            if (SelectedConversation == conversation)
            {
                SelectedConversation = Conversations.FirstOrDefault();
            }
        }

        /// <summary>
        /// 更新对话
        /// </summary>
        public void UpdateConversation(ChatConversation conversation)
        {
            ChatConversation? existing = Conversations.FirstOrDefault(c => c.Id == conversation.Id);
            if (existing != null)
            {
                int index = Conversations.IndexOf(existing);
                Conversations[index] = conversation;
                
                // 按更新时间排序
                Conversations.Move(index, 0);
            }
        }
    }
}


