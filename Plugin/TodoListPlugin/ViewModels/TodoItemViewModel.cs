using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TodoListPlugin.Models;

namespace TodoListPlugin.ViewModels
{
    /// <summary>
    /// 待办事项 ViewModel - 单个待办项的视图模型
    /// </summary>
    public class TodoItemViewModel : INotifyPropertyChanged
    {
        private readonly TodoItem _item;
        private readonly Action<TodoItem>? _onChanged;
        private IReadOnlyList<CustomFieldTemplate>? _fieldTemplates;

        public TodoItemViewModel(TodoItem item, Action<TodoItem>? onChanged = null)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _onChanged = onChanged;
        }

        /// <summary>
        /// 设置字段模板（用于决定哪些自定义字段显示在卡片上）
        /// </summary>
        public void SetFieldTemplates(IReadOnlyList<CustomFieldTemplate>? templates)
        {
            _fieldTemplates = templates;
            OnPropertyChanged(nameof(DisplayCustomFields));
            OnPropertyChanged(nameof(HasDisplayCustomFields));
            OnPropertyChanged(nameof(ShowDescription));
            OnPropertyChanged(nameof(ShowPriority));
            OnPropertyChanged(nameof(ShowDueDate));
            OnPropertyChanged(nameof(ShowTags));
            OnPropertyChanged(nameof(ShouldShowDescriptionOnCard));
            OnPropertyChanged(nameof(ShouldShowPriorityOnCard));
            OnPropertyChanged(nameof(ShouldShowDueDateOnCard));
        }

        /// <summary>
        /// 是否显示描述（根据内置字段模板设置）
        /// </summary>
        public bool ShowDescription => GetBuiltinFieldShowOnCard("builtin_description");

        /// <summary>
        /// 是否显示优先级（根据内置字段模板设置）
        /// </summary>
        public bool ShowPriority => GetBuiltinFieldShowOnCard("builtin_priority");

        /// <summary>
        /// 是否显示截止日期（根据内置字段模板设置）
        /// </summary>
        public bool ShowDueDate => GetBuiltinFieldShowOnCard("builtin_duedate");

        /// <summary>
        /// 是否显示标签（根据内置字段模板设置）
        /// </summary>
        public bool ShowTags => GetBuiltinFieldShowOnCard("builtin_tags");

        /// <summary>
        /// 获取内置字段是否显示在卡片上
        /// </summary>
        private bool GetBuiltinFieldShowOnCard(string fieldId)
        {
            if (_fieldTemplates == null) return true; // 默认显示
            var field = _fieldTemplates.FirstOrDefault(t => t.Id == fieldId);
            return field?.ShowOnCard ?? true;
        }

        /// <summary>
        /// 获取需要显示在卡片上的自定义字段
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> DisplayCustomFields
        {
            get
            {
                if (_fieldTemplates == null || CustomFields == null || CustomFields.Count == 0)
                    return Enumerable.Empty<KeyValuePair<string, string>>();

                // 获取设置了 ShowOnCard 的字段模板名称
                var showOnCardFields = _fieldTemplates
                    .Where(t => t.ShowOnCard)
                    .Select(t => t.Name)
                    .ToHashSet();

                // 返回匹配的自定义字段
                return CustomFields
                    .Where(kvp => showOnCardFields.Contains(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value));
            }
        }

        /// <summary>
        /// 是否有需要显示的自定义字段
        /// </summary>
        public bool HasDisplayCustomFields => DisplayCustomFields.Any();

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 获取原始数据模型
        /// </summary>
        public TodoItem Model => _item;

        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string Id => _item.Id;

        /// <summary>
        /// 所属项目ID
        /// </summary>
        public string ProjectId
        {
            get => _item.ProjectId;
            set
            {
                if (_item.ProjectId != value)
                {
                    _item.ProjectId = value;
                    OnPropertyChanged();
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 所属状态列ID
        /// </summary>
        public string StatusColumnId
        {
            get => _item.StatusColumnId;
            set
            {
                if (_item.StatusColumnId != value)
                {
                    _item.StatusColumnId = value;
                    OnPropertyChanged();
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get => _item.Title;
            set
            {
                if (_item.Title != value)
                {
                    _item.Title = value;
                    OnPropertyChanged();
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get => _item.Description;
            set
            {
                if (_item.Description != value)
                {
                    _item.Description = value;
                    OnPropertyChanged();
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 排序
        /// </summary>
        public int Order
        {
            get => _item.Order;
            set
            {
                if (_item.Order != value)
                {
                    _item.Order = value;
                    OnPropertyChanged();
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime => _item.CreatedTime;

        /// <summary>
        /// 提醒间隔类型
        /// </summary>
        public ReminderInterval ReminderIntervalType
        {
            get => _item.ReminderIntervalType;
            set
            {
                if (_item.ReminderIntervalType != value)
                {
                    _item.ReminderIntervalType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasReminder));
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 是否启用提醒
        /// </summary>
        public bool IsReminderEnabled
        {
            get => _item.IsReminderEnabled;
            set
            {
                if (_item.IsReminderEnabled != value)
                {
                    _item.IsReminderEnabled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasReminder));
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 是否有提醒
        /// </summary>
        public bool HasReminder => _item.IsReminderEnabled && _item.ReminderIntervalType != ReminderInterval.None;

        /// <summary>
        /// 优先级名称
        /// </summary>
        public string? PriorityName
        {
            get => _item.PriorityName;
            set
            {
                if (_item.PriorityName != value)
                {
                    _item.PriorityName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasPriority));
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 优先级ID
        /// </summary>
        public string? PriorityId
        {
            get => _item.PriorityId;
            set
            {
                if (_item.PriorityId != value)
                {
                    _item.PriorityId = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasPriority));
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 优先级颜色（用于UI显示）
        /// </summary>
        public string PriorityColor
        {
            get => _item.PriorityColor ?? "#808080";
            set
            {
                if (_item.PriorityColor != value)
                {
                    _item.PriorityColor = value;
                    OnPropertyChanged();
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 是否有优先级
        /// </summary>
        public bool HasPriority => !string.IsNullOrEmpty(_item.PriorityId);

        /// <summary>
        /// 截止日期
        /// </summary>
        public DateTime? DueDate
        {
            get => _item.DueDate;
            set
            {
                if (_item.DueDate != value)
                {
                    _item.DueDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DueDateText));
                    OnPropertyChanged(nameof(HasDueDate));
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 截止日期显示文本
        /// </summary>
        public string? DueDateText
        {
            get
            {
                if (_item.DueDate == null) return null;
                var date = _item.DueDate.Value;
                var today = DateTime.Today;
                if (date.Date == today)
                    return "今天";
                if (date.Date == today.AddDays(1))
                    return "明天";
                if (date.Date == today.AddDays(-1))
                    return "昨天";
                return date.ToString("MM/dd");
            }
        }

        /// <summary>
        /// 是否有截止日期
        /// </summary>
        public bool HasDueDate => _item.DueDate != null;

        /// <summary>
        /// 是否在卡片上显示描述（有内容且设置为显示）
        /// </summary>
        public bool ShouldShowDescriptionOnCard => !string.IsNullOrEmpty(Description) && ShowDescription;

        /// <summary>
        /// 是否在卡片上显示优先级（有值且设置为显示）
        /// </summary>
        public bool ShouldShowPriorityOnCard => HasPriority && ShowPriority;

        /// <summary>
        /// 是否在卡片上显示截止日期（有值且设置为显示）
        /// </summary>
        public bool ShouldShowDueDateOnCard => HasDueDate && ShowDueDate;

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string> Tags
        {
            get => _item.Tags;
            set
            {
                _item.Tags = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTags));
                NotifyChanged();
            }
        }

        /// <summary>
        /// 是否有标签
        /// </summary>
        public bool HasTags => _item.Tags.Count > 0;

        /// <summary>
        /// 自定义字段
        /// </summary>
        public Dictionary<string, string> CustomFields
        {
            get => _item.CustomFields;
            set
            {
                _item.CustomFields = value;
                OnPropertyChanged();
                NotifyChanged();
            }
        }

        /// <summary>
        /// 上次提醒时间
        /// </summary>
        public DateTime? LastReminderTime
        {
            get => _item.LastReminderTime;
            set
            {
                if (_item.LastReminderTime != value)
                {
                    _item.LastReminderTime = value;
                    OnPropertyChanged();
                    NotifyChanged();
                }
            }
        }

        /// <summary>
        /// 刷新所有属性通知
        /// </summary>
        public void Refresh()
        {
            OnPropertyChanged(string.Empty);
        }

        private bool _isVisible = true;

        /// <summary>
        /// 是否可见（用于搜索过滤）
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 检查是否匹配搜索文本
        /// </summary>
        public bool MatchesSearch(string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            var text = searchText.Trim().ToLowerInvariant();
            
            // 搜索标题
            if (Title?.ToLowerInvariant().Contains(text) == true)
                return true;

            // 搜索描述
            if (Description?.ToLowerInvariant().Contains(text) == true)
                return true;

            // 搜索优先级名称
            if (PriorityName?.ToLowerInvariant().Contains(text) == true)
                return true;

            return false;
        }

        /// <summary>
        /// 应用搜索过滤
        /// </summary>
        public void ApplySearchFilter(string? searchText)
        {
            IsVisible = MatchesSearch(searchText);
        }

        private void NotifyChanged()
        {
            _onChanged?.Invoke(_item);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
