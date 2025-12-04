using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using TodoListPlugin.Helpers;
using TodoListPlugin.Models;
using TodoListPlugin.ViewModels;
using TodoListPlugin.Views.Dialogs;

namespace TodoListPlugin.Views
{
    public partial class TodoDetailPanel : UserControl
    {
        private TodoItemViewModel? _currentItem;
        private IList<StatusColumn>? _statusColumns;
        private IList<PriorityGroup>? _priorities;
        private IList<TagGroup>? _allTags;
        private IList<CustomFieldTemplate>? _fieldTemplates;
        private ObservableCollection<TagGroup> _itemTags = new();
        private ObservableCollection<EditableCustomField> _editableFields = new();
        
        private bool _isLoading = false;

        /// <summary>
        /// 关闭面板事件
        /// </summary>
        public event EventHandler? CloseRequested;

        /// <summary>
        /// 待办变更事件
        /// </summary>
        public event EventHandler<TodoItemViewModel>? ItemChanged;

        /// <summary>
        /// 删除待办事件
        /// </summary>
        public event EventHandler<TodoItemViewModel>? ItemDeleted;

        /// <summary>
        /// 移动待办事件
        /// </summary>
        public event EventHandler<TodoItemViewModel>? MoveRequested;

        public TodoDetailPanel()
        {
            InitializeComponent();
            TagsItemsControl.ItemsSource = _itemTags;
            CustomFieldsItemsControl.ItemsSource = _editableFields;
        }

        /// <summary>
        /// 设置可用的状态列
        /// </summary>
        public void SetStatusColumns(IEnumerable<StatusColumn> columns)
        {
            _statusColumns = columns.ToList();
            StatusComboBox.ItemsSource = _statusColumns;
            StatusComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
        }

        /// <summary>
        /// 设置字段模板
        /// </summary>
        public void SetFieldTemplates(IEnumerable<CustomFieldTemplate> templates)
        {
            _fieldTemplates = templates.ToList();
        }

        /// <summary>
        /// 设置可用的优先级
        /// </summary>
        public void SetPriorities(IEnumerable<PriorityGroup> priorities)
        {
            _priorities = priorities.ToList();
            // 添加一个"无"选项
            var items = new List<PriorityGroup> { new PriorityGroup { Id = "", Name = "无", Color = "#808080" } };
            items.AddRange(_priorities);
            PriorityComboBox.ItemsSource = items;
        }

        /// <summary>
        /// 设置可用的标签
        /// </summary>
        public void SetTags(IEnumerable<TagGroup> tags)
        {
            _allTags = tags.ToList();
        }

        /// <summary>
        /// 加载待办详情
        /// </summary>
        public void LoadItem(TodoItemViewModel item)
        {
            _isLoading = true;
            try
            {
                // 取消订阅旧项目的事件
                if (_currentItem != null)
                {
                    _currentItem.PropertyChanged -= OnCurrentItemPropertyChanged;
                }

                _currentItem = item;
                
                // 订阅新项目的属性变化事件
                _currentItem.PropertyChanged += OnCurrentItemPropertyChanged;

                // 标题和描述
                TitleTextBox.Text = item.Title;
                DescriptionTextBox.Text = item.Description;

                // 状态
                if (_statusColumns != null)
                {
                    StatusComboBox.SelectedItem = _statusColumns.FirstOrDefault(s => s.Id == item.StatusColumnId);
                }

                // 优先级
                if (PriorityComboBox.ItemsSource is IList<PriorityGroup> priorityItems)
                {
                    PriorityComboBox.SelectedItem = priorityItems.FirstOrDefault(p => p.Id == item.PriorityId);
                }

                // 标签
                _itemTags.Clear();
                if (_allTags != null && item.Tags != null)
                {
                    foreach (var tagId in item.Tags)
                    {
                        var tag = _allTags.FirstOrDefault(t => t.Id == tagId);
                        if (tag != null)
                        {
                            _itemTags.Add(tag);
                        }
                    }
                }

                // 截止日期
                DueDatePicker.SelectedDate = item.DueDate;

                // 自定义字段
                LoadCustomFields(item.CustomFields);

                // 时间信息
                CreatedTimeText.Text = $"创建于 {item.Model.CreatedTime:yyyy-MM-dd HH:mm}";
                UpdatedTimeText.Text = item.Model.UpdatedTime.HasValue 
                    ? $"更新于 {item.Model.UpdatedTime:yyyy-MM-dd HH:mm}" 
                    : "";
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// 加载自定义字段（基于字段模板）
        /// </summary>
        private void LoadCustomFields(Dictionary<string, string>? customFields)
        {
            _editableFields.Clear();
            
            if (_fieldTemplates == null || _fieldTemplates.Count == 0)
            {
                NoCustomFieldsHint.IsVisible = true;
                return;
            }

            NoCustomFieldsHint.IsVisible = false;
            
            // 基于字段模板创建可编辑字段
            foreach (var template in _fieldTemplates.OrderBy(t => t.Order))
            {
                var value = "";
                if (customFields != null && customFields.TryGetValue(template.Name, out var v))
                {
                    value = v;
                }
                else if (!string.IsNullOrEmpty(template.DefaultValue))
                {
                    value = template.DefaultValue;
                }

                _editableFields.Add(new EditableCustomField
                {
                    TemplateId = template.Id,
                    Name = template.Name,
                    Value = value,
                    Required = template.Required,
                    FieldType = template.FieldType
                });
            }
        }

        /// <summary>
        /// 当前待办属性变化时更新UI
        /// </summary>
        private void OnCurrentItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isLoading || _currentItem == null) return;

            switch (e.PropertyName)
            {
                case nameof(TodoItemViewModel.StatusColumnId):
                    // 拖拽后状态改变，更新ComboBox
                    if (_statusColumns != null)
                    {
                        _isLoading = true;
                        StatusComboBox.SelectedItem = _statusColumns.FirstOrDefault(s => s.Id == _currentItem.StatusColumnId);
                        _isLoading = false;
                    }
                    break;
                case nameof(TodoItemViewModel.Title):
                    _isLoading = true;
                    TitleTextBox.Text = _currentItem.Title;
                    _isLoading = false;
                    break;
                case nameof(TodoItemViewModel.Description):
                    _isLoading = true;
                    DescriptionTextBox.Text = _currentItem.Description;
                    _isLoading = false;
                    break;
            }
        }

        /// <summary>
        /// 清空面板
        /// </summary>
        public void Clear()
        {
            // 取消订阅旧项目的事件
            if (_currentItem != null)
            {
                _currentItem.PropertyChanged -= OnCurrentItemPropertyChanged;
            }
            
            _currentItem = null;
            TitleTextBox.Text = "";
            DescriptionTextBox.Text = "";
            StatusComboBox.SelectedItem = null;
            PriorityComboBox.SelectedItem = null;
            _itemTags.Clear();
            DueDatePicker.SelectedDate = null;
            _editableFields.Clear();
            CreatedTimeText.Text = "";
            UpdatedTimeText.Text = "";
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnTitleLostFocus(object? sender, RoutedEventArgs e)
        {
            if (_isLoading || _currentItem == null) return;

            var newTitle = TitleTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(newTitle) && newTitle != _currentItem.Title)
            {
                _currentItem.Title = newTitle;
                ItemChanged?.Invoke(this, _currentItem);
            }
        }

        private void OnDescriptionLostFocus(object? sender, RoutedEventArgs e)
        {
            if (_isLoading || _currentItem == null) return;

            var newDesc = DescriptionTextBox.Text?.Trim();
            if (newDesc != _currentItem.Description)
            {
                _currentItem.Description = newDesc ?? "";
                ItemChanged?.Invoke(this, _currentItem);
            }
        }

        private void OnStatusChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || _currentItem == null) return;

            if (StatusComboBox.SelectedItem is StatusColumn status && status.Id != _currentItem.StatusColumnId)
            {
                _currentItem.StatusColumnId = status.Id;
                ItemChanged?.Invoke(this, _currentItem);
            }
        }

        private void OnPriorityChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || _currentItem == null) return;

            if (PriorityComboBox.SelectedItem is PriorityGroup priority)
            {
                var newPriorityId = string.IsNullOrEmpty(priority.Id) ? null : priority.Id;
                if (newPriorityId != _currentItem.PriorityId)
                {
                    _currentItem.PriorityId = newPriorityId;
                    _currentItem.PriorityColor = string.IsNullOrEmpty(priority.Id) ? null : priority.Color;
                    ItemChanged?.Invoke(this, _currentItem);
                }
            }
        }

        private async void OnAddTagClick(object? sender, RoutedEventArgs e)
        {
            if (_currentItem == null || _allTags == null) return;

            // 获取未添加的标签
            var existingTagIds = _itemTags.Select(t => t.Id).ToHashSet();
            var availableTags = _allTags.Where(t => !existingTagIds.Contains(t.Id)).ToList();

            if (availableTags.Count == 0)
            {
                await DialogHelper.ShowMessageAsync("提示", "没有更多可添加的标签");
                return;
            }

            // 创建选择标签的内容
            var listBox = new ListBox
            {
                ItemsSource = availableTags,
                SelectionMode = SelectionMode.Multiple,
                MaxHeight = 300
            };
            listBox.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<TagGroup>((tag, _) =>
            {
                var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
                panel.Children.Add(new Border 
                { 
                    Width = 16, Height = 16, 
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(tag.Color))
                });
                panel.Children.Add(new TextBlock { Text = tag.Name });
                return panel;
            });

            var dialog = new ContentDialog
            {
                Title = "选择标签",
                Content = listBox,
                PrimaryButtonText = "添加",
                CloseButtonText = "取消"
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var selectedTags = listBox.SelectedItems?.Cast<TagGroup>().ToList();
                if (selectedTags != null && selectedTags.Count > 0)
                {
                    foreach (var tag in selectedTags)
                    {
                        _itemTags.Add(tag);
                    }
                    UpdateItemTags();
                }
            }
        }

        private void OnRemoveTagClick(object? sender, RoutedEventArgs e)
        {
            if (_currentItem == null) return;

            if (sender is Button btn && btn.Tag is string tagId)
            {
                var tag = _itemTags.FirstOrDefault(t => t.Id == tagId);
                if (tag != null)
                {
                    _itemTags.Remove(tag);
                    UpdateItemTags();
                }
            }
        }

        private void UpdateItemTags()
        {
            if (_currentItem == null) return;

            _currentItem.Model.Tags = _itemTags.Select(t => t.Id).ToList();
            ItemChanged?.Invoke(this, _currentItem);
        }

        private void OnDueDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || _currentItem == null) return;

            var selectedDate = DueDatePicker.SelectedDate;
            DateTime? newDate = selectedDate.HasValue ? selectedDate.Value.Date : null;
            if (newDate != _currentItem.DueDate)
            {
                _currentItem.DueDate = newDate;
                ItemChanged?.Invoke(this, _currentItem);
            }
        }

        private void OnClearDueDateClick(object? sender, RoutedEventArgs e)
        {
            if (_currentItem == null) return;

            DueDatePicker.SelectedDate = null;
            if (_currentItem.DueDate != null)
            {
                _currentItem.DueDate = null;
                ItemChanged?.Invoke(this, _currentItem);
            }
        }

        private void OnMoveClick(object? sender, RoutedEventArgs e)
        {
            if (_currentItem != null)
            {
                MoveRequested?.Invoke(this, _currentItem);
            }
        }

        private async void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            if (_currentItem == null) return;

            var confirm = await DialogHelper.ShowDeleteConfirmAsync(_currentItem.Title);
            if (confirm)
            {
                ItemDeleted?.Invoke(this, _currentItem);
            }
        }

        /// <summary>
        /// 自定义字段值变更
        /// </summary>
        private void OnCustomFieldValueLostFocus(object? sender, RoutedEventArgs e)
        {
            if (_isLoading || _currentItem == null) return;
            if (sender is not TextBox textBox || textBox.Tag is not EditableCustomField field) return;

            var newValue = textBox.Text?.Trim() ?? "";
            
            // 更新到待办项的 CustomFields
            if (string.IsNullOrEmpty(newValue))
            {
                _currentItem.CustomFields.Remove(field.Name);
            }
            else
            {
                _currentItem.CustomFields[field.Name] = newValue;
            }
            
            // 更新本地缓存
            field.Value = newValue;
            
            // 通知变更
            ItemChanged?.Invoke(this, _currentItem);
        }
    }

    /// <summary>
    /// 可编辑的自定义字段
    /// </summary>
    public class EditableCustomField
    {
        public string TemplateId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public bool Required { get; set; }
        public FieldType FieldType { get; set; }
    }

    /// <summary>
    /// 自定义字段项（已弃用，保留兼容）
    /// </summary>
    public class CustomFieldItem
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
