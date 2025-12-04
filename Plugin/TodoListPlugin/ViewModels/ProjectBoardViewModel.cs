using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TodoListPlugin.Models;

namespace TodoListPlugin.ViewModels
{
    /// <summary>
    /// 项目 ViewModel v2 - 包含多个状态列的看板视图
    /// </summary>
    public class ProjectBoardViewModel : INotifyPropertyChanged
    {
        private readonly Project _project;
        private readonly ObservableCollection<StatusColumnViewModel> _statusColumns;
        private int _totalItemCount;

        public ProjectBoardViewModel(Project project, IEnumerable<StatusColumn> statusColumns)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _statusColumns = new ObservableCollection<StatusColumnViewModel>(
                statusColumns.OrderBy(s => s.Order).Select(s => new StatusColumnViewModel(s))
            );
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 获取原始数据模型
        /// </summary>
        public Project Model => _project;

        /// <summary>
        /// 项目 ID
        /// </summary>
        public string Id => _project.Id;

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name
        {
            get => _project.Name;
            set
            {
                if (_project.Name != value)
                {
                    _project.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 项目颜色
        /// </summary>
        public string Color
        {
            get => _project.Color;
            set
            {
                if (_project.Color != value)
                {
                    _project.Color = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 排序
        /// </summary>
        public int Order
        {
            get => _project.Order;
            set
            {
                if (_project.Order != value)
                {
                    _project.Order = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 项目图标
        /// </summary>
        public string? Icon
        {
            get => _project.Icon;
            set
            {
                if (_project.Icon != value)
                {
                    _project.Icon = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 项目描述
        /// </summary>
        public string? Description
        {
            get => _project.Description;
            set
            {
                if (_project.Description != value)
                {
                    _project.Description = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime => _project.CreatedTime;

        /// <summary>
        /// 状态列列表
        /// </summary>
        public ObservableCollection<StatusColumnViewModel> StatusColumns => _statusColumns;

        /// <summary>
        /// 项目内待办总数
        /// </summary>
        public int TotalItemCount
        {
            get => _totalItemCount;
            private set
            {
                if (_totalItemCount != value)
                {
                    _totalItemCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalItemCountText));
                }
            }
        }

        /// <summary>
        /// 待办总数显示文本
        /// </summary>
        public string TotalItemCountText => $"{TotalItemCount} 项";

        /// <summary>
        /// 获取状态列
        /// </summary>
        public StatusColumnViewModel? GetStatusColumn(string statusId)
        {
            return _statusColumns.FirstOrDefault(s => s.Id == statusId);
        }

        /// <summary>
        /// 加载待办事项到对应的状态列
        /// </summary>
        public void LoadItems(IEnumerable<TodoItem> items, Action<TodoItem>? onItemChanged = null, IReadOnlyList<CustomFieldTemplate>? fieldTemplates = null)
        {
            // 先清空所有状态列
            foreach (var column in _statusColumns)
            {
                column.ClearItems();
            }

            // 按状态分组添加
            foreach (var item in items.OrderBy(i => i.Order))
            {
                var column = GetStatusColumn(item.StatusColumnId);
                if (column != null)
                {
                    var itemVm = new TodoItemViewModel(item, onItemChanged);
                    itemVm.SetFieldTemplates(fieldTemplates);
                    column.AddItem(itemVm);
                }
            }

            UpdateTotalCount();
        }

        /// <summary>
        /// 添加待办事项
        /// </summary>
        public void AddItem(TodoItemViewModel item)
        {
            var column = GetStatusColumn(item.StatusColumnId);
            column?.AddItem(item);
            UpdateTotalCount();
        }

        /// <summary>
        /// 移除待办事项
        /// </summary>
        public bool RemoveItem(string itemId)
        {
            foreach (var column in _statusColumns)
            {
                if (column.RemoveItem(itemId))
                {
                    UpdateTotalCount();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取待办事项
        /// </summary>
        public TodoItemViewModel? GetItem(string itemId)
        {
            foreach (var column in _statusColumns)
            {
                var item = column.GetItem(itemId);
                if (item != null) return item;
            }
            return null;
        }

        /// <summary>
        /// 移动待办事项到其他状态列
        /// </summary>
        public void MoveItemToStatus(string itemId, string targetStatusId)
        {
            TodoItemViewModel? itemVm = null;
            StatusColumnViewModel? sourceColumn = null;

            // 找到待办事项和源状态列
            foreach (var column in _statusColumns)
            {
                itemVm = column.GetItem(itemId);
                if (itemVm != null)
                {
                    sourceColumn = column;
                    break;
                }
            }

            if (itemVm == null || sourceColumn == null) return;

            var targetColumn = GetStatusColumn(targetStatusId);
            if (targetColumn == null || targetColumn == sourceColumn) return;

            // 从源列移除
            sourceColumn.RemoveItem(itemId);

            // 更新状态ID
            itemVm.StatusColumnId = targetStatusId;

            // 添加到目标列
            targetColumn.AddItem(itemVm);
        }

        /// <summary>
        /// 更新总数
        /// </summary>
        public void UpdateTotalCount()
        {
            TotalItemCount = _statusColumns.Sum(c => c.ItemCount);
        }

        /// <summary>
        /// 刷新所有属性通知
        /// </summary>
        public void Refresh()
        {
            OnPropertyChanged(string.Empty);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
