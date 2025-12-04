using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TodoListPlugin.Models;

namespace TodoListPlugin.ViewModels
{
    /// <summary>
    /// 状态列 ViewModel - 管理单个状态列及其下的待办事项
    /// </summary>
    public class StatusColumnViewModel : INotifyPropertyChanged
    {
        private readonly StatusColumn _model;
        private readonly ObservableCollection<TodoItemViewModel> _items;

        public StatusColumnViewModel(StatusColumn model)
        {
            _model = model;
            _items = new ObservableCollection<TodoItemViewModel>();
            _items.CollectionChanged += OnItemsCollectionChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 获取原始数据模型
        /// </summary>
        public StatusColumn Model => _model;

        /// <summary>
        /// 状态列 ID
        /// </summary>
        public string Id => _model.Id;

        /// <summary>
        /// 状态名称
        /// </summary>
        public string Name
        {
            get => _model.Name;
            set
            {
                if (_model.Name != value)
                {
                    _model.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 状态颜色
        /// </summary>
        public string Color
        {
            get => _model.Color;
            set
            {
                if (_model.Color != value)
                {
                    _model.Color = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 图标
        /// </summary>
        public string? Icon
        {
            get => _model.Icon;
            set
            {
                if (_model.Icon != value)
                {
                    _model.Icon = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 排序
        /// </summary>
        public int Order
        {
            get => _model.Order;
            set
            {
                if (_model.Order != value)
                {
                    _model.Order = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否为默认状态
        /// </summary>
        public bool IsDefault
        {
            get => _model.IsDefault;
            set
            {
                if (_model.IsDefault != value)
                {
                    _model.IsDefault = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 待办事项列表
        /// </summary>
        public ObservableCollection<TodoItemViewModel> Items => _items;

        /// <summary>
        /// 待办数量
        /// </summary>
        public int ItemCount => _items.Count;

        /// <summary>
        /// 是否有待办事项
        /// </summary>
        public bool HasItems => _items.Count > 0;

        /// <summary>
        /// 待办数量显示文本
        /// </summary>
        public string ItemCountText => $"{ItemCount}";

        /// <summary>
        /// 添加待办事项
        /// </summary>
        public void AddItem(TodoItemViewModel item)
        {
            int insertIndex = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Order > item.Order)
                {
                    break;
                }
                insertIndex = i + 1;
            }
            _items.Insert(insertIndex, item);
        }

        /// <summary>
        /// 移除待办事项
        /// </summary>
        public bool RemoveItem(string itemId)
        {
            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                return _items.Remove(item);
            }
            return false;
        }

        /// <summary>
        /// 获取待办事项
        /// </summary>
        public TodoItemViewModel? GetItem(string itemId)
        {
            return _items.FirstOrDefault(i => i.Id == itemId);
        }

        /// <summary>
        /// 清空所有待办事项
        /// </summary>
        public void ClearItems()
        {
            _items.Clear();
        }

        /// <summary>
        /// 重新排序待办事项
        /// </summary>
        public void ReorderItems()
        {
            var sorted = _items.OrderBy(i => i.Order).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int currentIndex = _items.IndexOf(sorted[i]);
                if (currentIndex != i)
                {
                    _items.Move(currentIndex, i);
                }
            }
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ItemCount));
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(ItemCountText));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
