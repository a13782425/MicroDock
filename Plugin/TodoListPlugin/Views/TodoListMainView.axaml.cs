using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using TodoListPlugin.Helpers;
using TodoListPlugin.Models;
using TodoListPlugin.ViewModels;
using TodoListPlugin.Views.Dialogs;

namespace TodoListPlugin.Views
{
    public partial class TodoListMainView : UserControl
    {
        private TodoListMainViewModel? _viewModel;
        private TodoItemViewModel? _draggedItem;
        private Point _dragStartPoint;
        private bool _isDragging;
        private bool _isDetailPanelOpen;
        private ProjectBoardViewModel? _contextMenuProject; // 当前右键菜单的项目

        public TodoListMainView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            
            // 设置详情面板事件
            DetailPanel.CloseRequested += OnDetailPanelCloseRequested;
            DetailPanel.ItemChanged += OnDetailPanelItemChanged;
            DetailPanel.ItemDeleted += OnDetailPanelItemDeleted;
            DetailPanel.MoveRequested += OnDetailPanelMoveRequested;
            
            // 设置看板拖放事件
            AddHandler(DragDrop.DragOverEvent, OnKanbanDragOver);
            AddHandler(DragDrop.DropEvent, OnKanbanDrop);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            _viewModel = DataContext as TodoListMainViewModel;
        }

        /// <summary>
        /// 添加项目按钮点击
        /// </summary>
        private async void OnAddProjectClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var content = new AddProjectContent();
            var result = await DialogHelper.ShowContentDialogWithValidationAsync(
                "新建项目",
                content,
                () => content.IsValid);

            if (result == ContentDialogResult.Primary)
            {
                await _viewModel.AddProjectAsync(content.ProjectName, content.SelectedColor, null);
            }
        }

        /// <summary>
        /// 设置按钮点击
        /// </summary>
        private void OnSettingsClick(object? sender, RoutedEventArgs e)
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        #region 项目右键菜单

        /// <summary>
        /// 项目右键菜单打开时获取数据上下文
        /// </summary>
        private void OnProjectContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _contextMenuProject = null;
            
            if (sender is ContextMenu contextMenu)
            {
                // 方法1：通过 PlacementTarget 的 Tag 属性
                if (contextMenu.PlacementTarget is Control { Tag: ProjectBoardViewModel project1 })
                {
                    _contextMenuProject = project1;
                    return;
                }
                
                // 方法2：通过 PlacementTarget 的 DataContext
                if (contextMenu.PlacementTarget is Control { DataContext: ProjectBoardViewModel project2 })
                {
                    _contextMenuProject = project2;
                    return;
                }
                
                // 方法3：向上查找
                var current = contextMenu.PlacementTarget as Visual;
                while (current != null)
                {
                    if (current is Control c && c.DataContext is ProjectBoardViewModel project3)
                    {
                        _contextMenuProject = project3;
                        return;
                    }
                    current = current.GetVisualParent();
                }
            }
        }

        /// <summary>
        /// 重命名项目
        /// </summary>
        private async void OnRenameProjectClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            
            // 优先从menuItem.Tag获取，否则使用_contextMenuProject
            var project = (sender as MenuItem)?.Tag as ProjectBoardViewModel ?? _contextMenuProject;
            if (project == null) return;

            var textBox = new TextBox { Text = project.Name, Width = 300 };
            var result = await DialogHelper.ShowContentDialogAsync(
                "重命名项目",
                textBox,
                "确定",
                "取消");

            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                await _viewModel.RenameProjectAsync(project.Id, textBox.Text.Trim());
            }
        }

        /// <summary>
        /// 修改项目颜色
        /// </summary>
        private async void OnChangeProjectColorClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var project = (sender as MenuItem)?.Tag as ProjectBoardViewModel ?? _contextMenuProject;
            if (project == null) return;

            var content = new AddProjectContent();
            content.SetColor(project.Color);
            
            var result = await DialogHelper.ShowContentDialogAsync(
                "修改颜色",
                content,
                "确定",
                "取消");

            if (result == ContentDialogResult.Primary)
            {
                await _viewModel.ChangeProjectColorAsync(project.Id, content.SelectedColor);
            }
        }

        /// <summary>
        /// 删除项目
        /// </summary>
        private async void OnDeleteProjectClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var project = (sender as MenuItem)?.Tag as ProjectBoardViewModel ?? _contextMenuProject;
            if (project == null) return;

            var confirm = await DialogHelper.ShowDeleteConfirmAsync(project.Name);
            if (confirm)
            {
                await _viewModel.DeleteProjectAsync(project.Id);
            }
        }

        #endregion

        #region 待办右键菜单

        /// <summary>
        /// 编辑待办（打开详情面板）
        /// </summary>
        private void OnEditTodoClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not TodoItemViewModel item) return;
            OpenTodoDetail(item);
        }

        /// <summary>
        /// 移动待办到其他项目
        /// </summary>
        private async void OnMoveTodoClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.SelectedProject == null) return;
            if (sender is not MenuItem menuItem || menuItem.Tag is not TodoItemViewModel item) return;

            // 调用移动逻辑（与详情面板相同）
            var content = new StackPanel { Spacing = 8 };
            var projectComboBox = new ComboBox
            {
                ItemsSource = _viewModel.Projects.Where(p => p.Id != _viewModel.SelectedProject.Id).ToList(),
                DisplayMemberBinding = new Avalonia.Data.Binding("Name"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                MinWidth = 200
            };
            content.Children.Add(new TextBlock { Text = "选择目标项目:" });
            content.Children.Add(projectComboBox);

            var result = await DialogHelper.ShowContentDialogAsync(
                "移动待办",
                content,
                "移动",
                "取消");

            if (result == ContentDialogResult.Primary && projectComboBox.SelectedItem is ProjectBoardViewModel targetProject)
            {
                await _viewModel.MoveTodoItemToProjectAsync(item, targetProject.Id);
            }
        }

        /// <summary>
        /// 删除待办
        /// </summary>
        private async void OnDeleteTodoClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is not MenuItem menuItem || menuItem.Tag is not TodoItemViewModel item) return;

            var confirm = await DialogHelper.ShowDeleteConfirmAsync(item.Title);
            if (confirm)
            {
                await _viewModel.DeleteTodoItemAsync(item);
            }
        }

        #endregion

        /// <summary>
        /// 添加待办按钮点击
        /// </summary>
        private async void OnAddTodoClick(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.SelectedProject == null) return;

            var button = sender as Button;
            var statusColumnId = button?.Tag as string;

            var content = new AddTodoContent();
            content.SetStatusColumns(_viewModel.SelectedProject.StatusColumns.Select(s => s.Model));
            content.SetRequiredFields(_viewModel.FieldTemplates);
            if (!string.IsNullOrEmpty(statusColumnId))
            {
                content.SetSelectedStatus(statusColumnId);
            }

            var result = await DialogHelper.ShowContentDialogWithValidationAsync(
                "添加待办",
                content,
                () => content.IsValid);

            if (result == ContentDialogResult.Primary)
            {
                var itemVm = await _viewModel.AddTodoItemAsync(content.TodoTitle, content.SelectedStatusId);
                
                // 设置必填字段值到自定义字段
                if (content.RequiredFieldValues.Count > 0)
                {
                    foreach (var field in _viewModel.FieldTemplates.Where(f => f.Required))
                    {
                        if (content.RequiredFieldValues.TryGetValue(field.Id, out var value) && !string.IsNullOrEmpty(value))
                        {
                            itemVm.CustomFields[field.Name] = value;
                        }
                    }
                    await _viewModel.UpdateTodoItemAsync(itemVm);
                }
            }
        }

        /// <summary>
        /// 待办卡片鼠标按下 - 开始拖拽
        /// </summary>
        private void OnTodoCardPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.Tag is not TodoItemViewModel itemVm) return;

            var point = e.GetCurrentPoint(border);
            if (point.Properties.IsLeftButtonPressed)
            {
                _draggedItem = itemVm;
                _dragStartPoint = e.GetPosition(this);
                _isDragging = false;

                border.PointerMoved += OnTodoCardPointerMoved;
                border.PointerReleased += OnTodoCardPointerReleased;
            }
        }

        /// <summary>
        /// 待办卡片鼠标移动 - 检测拖拽
        /// </summary>
        private async void OnTodoCardPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_draggedItem == null || sender is not Border border) return;

            var currentPoint = e.GetPosition(this);
            var diff = currentPoint - _dragStartPoint;

            // 检测是否开始拖拽（移动超过一定距离）
            if (!_isDragging && (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5))
            {
                _isDragging = true;

                // 开始拖放操作
                var data = new DataObject();
                data.Set("TodoItem", _draggedItem);

                var result = await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);

                // 拖放完成，清理状态
                CleanupDrag(border);
            }
        }

        /// <summary>
        /// 待办卡片鼠标释放
        /// </summary>
        private void OnTodoCardPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (sender is Border border)
            {
                CleanupDrag(border);

                // 如果没有进入拖拽模式，视为点击，打开详情
                if (!_isDragging && _draggedItem != null)
                {
                    OpenTodoDetail(_draggedItem);
                }
            }
        }

        /// <summary>
        /// 清理拖拽状态
        /// </summary>
        private void CleanupDrag(Border border)
        {
            border.PointerMoved -= OnTodoCardPointerMoved;
            border.PointerReleased -= OnTodoCardPointerReleased;
            _draggedItem = null;
            _isDragging = false;
        }

        /// <summary>
        /// 看板拖拽经过
        /// </summary>
        private void OnKanbanDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.None;
            
            if (!e.Data.Contains("TodoItem")) return;
            
            // 查找目标状态列
            var targetColumn = FindStatusColumnFromEvent(e);
            if (targetColumn != null)
            {
                e.DragEffects = DragDropEffects.Move;
            }
        }

        /// <summary>
        /// 看板放置
        /// </summary>
        private async void OnKanbanDrop(object? sender, DragEventArgs e)
        {
            if (!e.Data.Contains("TodoItem")) return;
            if (_viewModel == null) return;

            var todoItem = e.Data.Get("TodoItem") as TodoItemViewModel;
            if (todoItem == null) return;

            // 查找目标状态列
            var targetColumn = FindStatusColumnFromEvent(e);
            if (targetColumn != null && targetColumn.Id != todoItem.StatusColumnId)
            {
                await _viewModel.MoveItemToStatusAsync(todoItem.Id, targetColumn.Id);
            }
        }

        /// <summary>
        /// 从拖放事件中查找目标状态列
        /// </summary>
        private StatusColumnViewModel? FindStatusColumnFromEvent(DragEventArgs e)
        {
            // 从事件源向上查找状态列 Border
            if (e.Source is Control control)
            {
                var current = control as Visual;
                while (current != null)
                {
                    if (current is Border border && 
                        border.Classes.Contains("status-column") && 
                        border.Tag is StatusColumnViewModel column)
                    {
                        return column;
                    }
                    current = current.GetVisualParent();
                }
            }
            return null;
        }

        /// <summary>
        /// 打开待办详情
        /// </summary>
        private void OpenTodoDetail(TodoItemViewModel item)
        {
            if (_viewModel?.SelectedProject == null) return;

            // 设置详情面板数据
            DetailPanel.SetStatusColumns(_viewModel.SelectedProject.StatusColumns.Select(s => s.Model));
            DetailPanel.SetPriorities(_viewModel.Settings.Priorities ?? new List<PriorityGroup>());
            DetailPanel.SetTags(_viewModel.Settings.Tags ?? new List<TagGroup>());
            DetailPanel.SetFieldTemplates(_viewModel.FieldTemplates);
            DetailPanel.LoadItem(item);

            // 打开详情面板
            ShowDetailPanel();
        }

        /// <summary>
        /// 显示详情面板
        /// </summary>
        private void ShowDetailPanel()
        {
            if (!_isDetailPanelOpen)
            {
                _isDetailPanelOpen = true;
                DetailPanelOverlay.IsVisible = true;
            }
        }

        /// <summary>
        /// 隐藏详情面板
        /// </summary>
        private void HideDetailPanel()
        {
            if (_isDetailPanelOpen)
            {
                _isDetailPanelOpen = false;
                DetailPanelOverlay.IsVisible = false;
                DetailPanel.Clear();
            }
        }

        /// <summary>
        /// 点击遮罩层关闭详情面板
        /// </summary>
        private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // 检查是否点击在遮罩层上（而不是详情面板）
            if (e.Source == DetailPanelOverlay)
            {
                HideDetailPanel();
            }
        }

        /// <summary>
        /// 详情面板关闭请求
        /// </summary>
        private void OnDetailPanelCloseRequested(object? sender, EventArgs e)
        {
            HideDetailPanel();
        }

        /// <summary>
        /// 详情面板项目变更
        /// </summary>
        private async void OnDetailPanelItemChanged(object? sender, TodoItemViewModel item)
        {
            if (_viewModel == null) return;
            await _viewModel.UpdateTodoItemAsync(item);
        }

        /// <summary>
        /// 详情面板删除项目
        /// </summary>
        private async void OnDetailPanelItemDeleted(object? sender, TodoItemViewModel item)
        {
            if (_viewModel == null) return;
            await _viewModel.DeleteTodoItemAsync(item);
            HideDetailPanel();
        }

        /// <summary>
        /// 详情面板移动请求
        /// </summary>
        private async void OnDetailPanelMoveRequested(object? sender, TodoItemViewModel item)
        {
            if (_viewModel?.SelectedProject == null) return;

            // 创建项目选择内容
            var content = new StackPanel { Spacing = 8 };
            var projectComboBox = new ComboBox
            {
                ItemsSource = _viewModel.Projects.Where(p => p.Id != _viewModel.SelectedProject.Id).ToList(),
                DisplayMemberBinding = new Avalonia.Data.Binding("Name"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                MinWidth = 200
            };
            content.Children.Add(new TextBlock { Text = "选择目标项目:" });
            content.Children.Add(projectComboBox);

            var result = await DialogHelper.ShowContentDialogAsync(
                "移动待办",
                content,
                "移动",
                "取消");

            if (result == ContentDialogResult.Primary && projectComboBox.SelectedItem is ProjectBoardViewModel targetProject)
            {
                await _viewModel.MoveTodoItemToProjectAsync(item, targetProject.Id);
                HideDetailPanel();
            }
        }

        /// <summary>
        /// 获取窗口
        /// </summary>
        private Window? GetWindow()
        {
            return this.FindAncestorOfType<Window>();
        }

        /// <summary>
        /// 设置请求事件
        /// </summary>
        public event EventHandler? SettingsRequested;

        /// <summary>
        /// 待办详情请求事件（保留用于外部处理）
        /// </summary>
        public event EventHandler<TodoItemViewModel>? TodoDetailRequested;
    }

    /// <summary>
    /// 项目对话框结果
    /// </summary>
    public class ProjectDialogResult
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#2196F3";
        public string? Icon { get; set; }
    }
}
