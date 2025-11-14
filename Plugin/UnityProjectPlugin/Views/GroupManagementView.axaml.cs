using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Linq;
using UnityProjectPlugin.Models;
using UnityProjectPlugin.ViewModels;

namespace UnityProjectPlugin.Views
{
    /// <summary>
    /// 分组管理视图
    /// </summary>
    public partial class GroupManagementView : UserControl
    {
        private readonly UnityProjectPlugin _plugin;
        private readonly GroupManagementViewModel _viewModel;

        private Button? _addGroupButton;
        private ListBox? _groupsListBox;
        private TextBlock? _emptyStateText;

        public GroupManagementView(UnityProjectPlugin plugin)
        {
            _plugin = plugin;
            _viewModel = new GroupManagementViewModel(plugin);

            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _addGroupButton = this.FindControl<Button>("AddGroupButton");
            _groupsListBox = this.FindControl<ListBox>("GroupsListBox");
            _emptyStateText = this.FindControl<TextBlock>("EmptyStateText");

            // 设置 DataContext
            DataContext = _viewModel;

            // 更新空状态显示
            UpdateEmptyState();
        }

        private void AttachEventHandlers()
        {
            if (_addGroupButton != null)
            {
                _addGroupButton.Click += AddGroupButton_Click;
            }

            if (_groupsListBox != null)
            {
                // 监听集合变化以更新使用数量和空状态
                _viewModel.Groups.CollectionChanged += (s, e) =>
                {
                    UpdateEmptyState();
                    UpdateUsageCounts();
                };
            }
        }

        private void UpdateEmptyState()
        {
            if (_emptyStateText != null)
            {
                _emptyStateText.IsVisible = _viewModel.Groups.Count == 0;
            }
        }

        private void UpdateUsageCounts()
        {
            // TODO: 实现使用数量更新
            // 由于 Avalonia 的 ItemContainerGenerator API 差异，暂时跳过此功能
        }

        private async void AddGroupButton_Click(object? sender, RoutedEventArgs e)
        {
            await ShowAddGroupDialog();
        }

        private async System.Threading.Tasks.Task ShowAddGroupDialog()
        {
            try
            {
                // 简化实现：使用固定名称或让用户在编辑 Flyout 中输入
                // TODO: 实现更友好的输入对话框
                string groupName = $"新分组{DateTime.Now:HHmmss}";
                _viewModel.AddGroup(groupName);
            }
            catch (Exception ex)
            {
                // TODO: 显示错误消息对话框
            }
        }

        public async void EditButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProjectGroup group)
            {
                await ShowEditGroupDialog(group);
            }
        }

        private async System.Threading.Tasks.Task ShowEditGroupDialog(ProjectGroup group)
        {
            try
            {
                // 简化实现：直接在列表中编辑或使用 Flyout
                // TODO: 实现更友好的编辑对话框
                string newName = group.Name + "_编辑";
                _viewModel.UpdateGroup(group.Id, newName);
            }
            catch (Exception ex)
            {
                // TODO: 显示错误消息对话框
            }
        }

        public async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProjectGroup group)
            {
                await DeleteGroup(group);
            }
        }

        private async System.Threading.Tasks.Task DeleteGroup(ProjectGroup group)
        {
            try
            {
                // 检查是否有项目使用该分组
                int usageCount = _viewModel.GetGroupUsageCount(group.Name);
                if (usageCount > 0)
                {
                    // TODO: 显示错误消息对话框
                    return;
                }

                // TODO: 显示确认对话框，暂时直接删除
                _viewModel.DeleteGroup(group.Id);
            }
            catch (Exception ex)
            {
                // TODO: 显示错误消息对话框
            }
        }
    }
}

