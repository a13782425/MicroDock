using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityProjectPlugin.Models;

namespace UnityProjectPlugin.Views
{
    /// <summary>
    /// 分组选择器组件
    /// </summary>
    public partial class GroupSelector : UserControl
    {
        private ComboBox? _groupComboBox;
        private ItemsControl? _groupsListControl;
        private TextBox? _newGroupNameTextBox;
        private Button? _addGroupButton;
        private UnityProjectPlugin? _plugin;

        /// <summary>
        /// 当前选中的分组
        /// </summary>
        public static readonly StyledProperty<string?> SelectedGroupProperty =
            AvaloniaProperty.Register<GroupSelector, string?>(nameof(SelectedGroup));

        public string? SelectedGroup
        {
            get => GetValue(SelectedGroupProperty);
            set => SetValue(SelectedGroupProperty, value);
        }

        /// <summary>
        /// 分组变化事件
        /// </summary>
        public event EventHandler<string?>? GroupChanged;

        /// <summary>
        /// 分组列表刷新事件
        /// </summary>
        public event EventHandler? GroupsRefreshed;

        public GroupSelector()
        {
            InitializeComponent();

            _groupComboBox = this.FindControl<ComboBox>("GroupComboBox");
            _groupsListControl = this.FindControl<ItemsControl>("GroupsListControl");
            _newGroupNameTextBox = this.FindControl<TextBox>("NewGroupNameTextBox");
            _addGroupButton = this.FindControl<Button>("AddGroupButton");

            // 绑定事件
            if (_groupComboBox != null)
            {
                _groupComboBox.SelectionChanged += OnGroupSelectionChanged;
            }

            if (_addGroupButton != null)
            {
                _addGroupButton.Click += OnAddGroupButtonClick;
            }

            if (_newGroupNameTextBox != null)
            {
                _newGroupNameTextBox.KeyDown += OnNewGroupNameKeyDown;
            }

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _plugin = UnityProjectPlugin.Instance;
            // 加载数据
            LoadGroupsData();

            // 绑定删除按钮事件
            BindDeleteButtons();
        }

      
        /// <summary>
        /// 加载分组数据
        /// </summary>
        public void LoadGroupsData()
        {
            if (_plugin == null) return;

            var groups = UnityProjectData.Groups;
            var groupNames = groups.Select(g => g.Name).ToList();

            // 更新 ComboBox
            if (_groupComboBox != null)
            {
                _groupComboBox.ItemsSource = groupNames;

                // 设置当前选中
                if (!string.IsNullOrEmpty(SelectedGroup) && groupNames.Contains(SelectedGroup))
                {
                    _groupComboBox.SelectedItem = SelectedGroup;
                }
            }

            // 更新分组列表
            if (_groupsListControl != null)
            {
                _groupsListControl.ItemsSource = groups;
            }

            // 延迟绑定删除按钮
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => BindDeleteButtons());
        }

        /// <summary>
        /// 绑定删除按钮事件
        /// </summary>
        private void BindDeleteButtons()
        {
            if (_groupsListControl == null) return;

            var deleteButtons = _groupsListControl
                .GetVisualDescendants()
                .OfType<Button>()
                .Where(b => b.Name == "DeleteGroupButton")
                .ToList();

            foreach (var button in deleteButtons)
            {
                button.Click -= OnDeleteGroupClick;
                button.Click += OnDeleteGroupClick;
            }
        }

        /// <summary>
        /// 分组选择变化
        /// </summary>
        private void OnGroupSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_groupComboBox == null) return;

            string? selectedGroup = _groupComboBox.SelectedItem as string;
            SelectedGroup = selectedGroup;
            GroupChanged?.Invoke(this, selectedGroup);
        }

        /// <summary>
        /// 添加分组按钮点击
        /// </summary>
        private async void OnAddGroupButtonClick(object? sender, RoutedEventArgs e)
        {
            await AddNewGroupAsync();
        }

        /// <summary>
        /// 新分组名输入框按键
        /// </summary>
        private async void OnNewGroupNameKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await AddNewGroupAsync();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 添加新分组
        /// </summary>
        private async Task AddNewGroupAsync()
        {
            if (_newGroupNameTextBox == null || _plugin == null) return;

            string? newGroupName = _newGroupNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(newGroupName)) return;

            // 检查是否已存在
            if (UnityProjectData.Groups.Any(g => g.Name == newGroupName))
            {
                return;
            }

            // 添加新分组
            await _plugin.AddGroupAsync(newGroupName);
            _newGroupNameTextBox.Text = string.Empty;

            // 重新加载分组列表
            LoadGroupsData();
            GroupsRefreshed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 删除分组点击
        /// </summary>
        private async void OnDeleteGroupClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ProjectGroup group)
            {
                var tabView = this.FindAncestorOfType<UnityProjectTabView>();
                if (tabView != null)
                {
                    await tabView.ViewModel.DeleteGroupAsync(group);
                    LoadGroupsData();
                    GroupsRefreshed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// 设置当前选中的分组（不触发事件）
        /// </summary>
        public void SetSelectedGroupSilently(string? groupName)
        {
            if (_groupComboBox == null) return;

            _groupComboBox.SelectionChanged -= OnGroupSelectionChanged;
            SelectedGroup = groupName;

            if (!string.IsNullOrEmpty(groupName))
            {
                _groupComboBox.SelectedItem = groupName;
            }
            else
            {
                _groupComboBox.SelectedItem = null;
            }

            _groupComboBox.SelectionChanged += OnGroupSelectionChanged;
        }
    }
}
