using AIChatPlugin.Models;
using AIChatPlugin.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using System;

namespace AIChatPlugin.Views
{
    /// <summary>
    /// 对话列表项
    /// </summary>
    public partial class ConversationItem : UserControl
    {
        public ConversationItem()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// 点击切换对话
        /// </summary>
        private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is ChatConversation conversation)
            {
                // 查找父级的 ViewModel
                AIChatTabView? parentView = VisualTreeHelper.FindAncestorOfType<AIChatTabView>(this);
                if (parentView?.DataContext is AIChatTabViewModel viewModel)
                {
                    await viewModel.LoadConversationAsync(conversation);
                }
            }
        }

        /// <summary>
        /// 删除对话
        /// </summary>
        private async void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            e.Handled = true; // 阻止事件冒泡
            
            if (DataContext is ChatConversation conversation)
            {
                // 查找父级的 ViewModel
                AIChatTabView? parentView = VisualTreeHelper.FindAncestorOfType<AIChatTabView>(this);
                if (parentView?.DataContext is AIChatTabViewModel viewModel)
                {
                    // 确认删除
                    ContentDialog dialog = new ContentDialog
                    {
                        Title = "确认删除",
                        Content = $"确定要删除对话「{conversation.Title}」吗？",
                        PrimaryButtonText = "删除",
                        CloseButtonText = "取消"
                    };

                    Window? parentWindow = VisualTreeHelper.FindAncestorOfType<Window>(this);
                    ContentDialogResult result = await dialog.ShowAsync(parentWindow);
                    
                    if (result == ContentDialogResult.Primary)
                    {
                        // 删除对话
                        AIChatPlugin? plugin = parentView.GetPlugin();
                        if (plugin != null)
                        {
                            await plugin.DeleteConversationAsync(conversation.Id);
                            await viewModel.LoadConversationsAsync();
                            
                            // 如果删除的是当前对话，清空消息
                            if (viewModel.CurrentConversation?.Id == conversation.Id)
                            {
                                viewModel.CurrentConversation = null;
                                viewModel.Messages.Clear();
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 查找可视树祖先的扩展方法
    /// </summary>
    public static class VisualTreeHelper
    {
        public static T? FindAncestorOfType<T>(Control control) where T : class
        {
            Avalonia.Visual? current = control.GetVisualParent();
            while (current != null)
            {
                if (current is T result)
                    return result;
                current = current.GetVisualParent();
            }
            return null;
        }
    }
}

