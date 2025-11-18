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
    /// 待办清单主视图的 ViewModel
    /// </summary>
    public class TodoListTabViewModel : INotifyPropertyChanged
    {
        private readonly TodoListPlugin _plugin;
        private string _searchText = string.Empty;
        private List<FilterCondition> _activeFilters = new List<FilterCondition>();

        public TodoListTabViewModel(TodoListPlugin plugin)
        {
            _plugin = plugin;
            Columns = new ObservableCollection<TodoColumn>();
            LoadData();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 所有页签
        /// </summary>
        public ObservableCollection<TodoColumn> Columns { get; }

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 活动的筛选条件
        /// </summary>
        public List<FilterCondition> ActiveFilters
        {
            get => _activeFilters;
            set
            {
                _activeFilters = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        public void LoadData()
        {
            Columns.Clear();
            List<TodoColumn> columns = _plugin.GetColumns();
            foreach (TodoColumn column in columns)
            {
                Columns.Add(column);
            }
        }

        /// <summary>
        /// 获取指定页签的待办事项（已筛选）
        /// </summary>
        public List<TodoItem> GetFilteredItemsForColumn(string columnId)
        {
            List<TodoItem> items = _plugin.GetTodosByColumn(columnId);

            // 应用搜索
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                items = items.Where(i => 
                    i.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    i.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 应用筛选条件
            if (_activeFilters.Count > 0)
            {
                items = items.Where(item => _activeFilters.All(filter => _plugin.MatchesFilter(item, filter))).ToList();
            }

            return items;
        }

        /// <summary>
        /// 添加筛选条件
        /// </summary>
        public void AddFilter(FilterCondition filter)
        {
            _activeFilters.Add(filter);
            OnPropertyChanged(nameof(ActiveFilters));
        }

        /// <summary>
        /// 移除筛选条件
        /// </summary>
        public void RemoveFilter(FilterCondition filter)
        {
            _activeFilters.Remove(filter);
            OnPropertyChanged(nameof(ActiveFilters));
        }

        /// <summary>
        /// 清除所有筛选条件
        /// </summary>
        public void ClearFilters()
        {
            _activeFilters.Clear();
            OnPropertyChanged(nameof(ActiveFilters));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

