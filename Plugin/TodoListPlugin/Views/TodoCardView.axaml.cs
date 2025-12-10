using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using TodoListPlugin.ViewModels;

namespace TodoListPlugin.Views
{
    /// <summary>
    /// 待办卡片视图 - 独立的卡片控件
    /// </summary>
    public partial class TodoCardView : UserControl
    {
        public TodoCardView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取当前绑定的待办项
        /// </summary>
        private TodoItemViewModel? CurrentItem => DataContext as TodoItemViewModel;

        #region 事件定义

        /// <summary>
        /// 卡片点击事件
        /// </summary>
        public event EventHandler<TodoItemViewModel>? CardClicked;

        /// <summary>
        /// 卡片拖拽开始事件
        /// </summary>
        public event EventHandler<(TodoItemViewModel Item, PointerEventArgs Args)>? DragStarted;

        /// <summary>
        /// 编辑请求事件
        /// </summary>
        public event EventHandler<TodoItemViewModel>? EditRequested;

        /// <summary>
        /// 移动请求事件
        /// </summary>
        public event EventHandler<TodoItemViewModel>? MoveRequested;

        /// <summary>
        /// 删除请求事件
        /// </summary>
        public event EventHandler<TodoItemViewModel>? DeleteRequested;

        #endregion

        #region 事件处理

        private Point _dragStartPoint;
        private bool _isDragging;

        /// <summary>
        /// 卡片鼠标按下
        /// </summary>
        private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (CurrentItem == null) return;

            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsLeftButtonPressed)
            {
                _dragStartPoint = e.GetPosition(this);
                _isDragging = false;

                // 订阅移动和释放事件
                PointerMoved += OnCardPointerMoved;
                PointerReleased += OnCardPointerReleased;
            }
        }

        /// <summary>
        /// 卡片鼠标移动
        /// </summary>
        private void OnCardPointerMoved(object? sender, PointerEventArgs e)
        {
            if (CurrentItem == null) return;

            var currentPoint = e.GetPosition(this);
            var diff = currentPoint - _dragStartPoint;

            // 检测是否开始拖拽（移动超过一定距离）
            if (!_isDragging && (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5))
            {
                _isDragging = true;
                CleanupPointerEvents();
                
                // 触发拖拽事件
                DragStarted?.Invoke(this, (CurrentItem, e));
            }
        }

        /// <summary>
        /// 卡片鼠标释放
        /// </summary>
        private void OnCardPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            CleanupPointerEvents();

            // 如果没有进入拖拽模式，视为点击
            if (!_isDragging && CurrentItem != null)
            {
                CardClicked?.Invoke(this, CurrentItem);
            }
        }

        /// <summary>
        /// 清理事件订阅
        /// </summary>
        private void CleanupPointerEvents()
        {
            PointerMoved -= OnCardPointerMoved;
            PointerReleased -= OnCardPointerReleased;
        }

        /// <summary>
        /// 编辑点击
        /// </summary>
        private void OnEditClick(object? sender, RoutedEventArgs e)
        {
            if (CurrentItem != null)
            {
                EditRequested?.Invoke(this, CurrentItem);
            }
        }

        /// <summary>
        /// 移动点击
        /// </summary>
        private void OnMoveClick(object? sender, RoutedEventArgs e)
        {
            if (CurrentItem != null)
            {
                MoveRequested?.Invoke(this, CurrentItem);
            }
        }

        /// <summary>
        /// 删除点击
        /// </summary>
        private void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            if (CurrentItem != null)
            {
                DeleteRequested?.Invoke(this, CurrentItem);
            }
        }

        #endregion
    }
}
