using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroDock.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using TodoListPlugin.Models;
using TodoListPlugin.ViewModels;

namespace TodoListPlugin.Views
{
    /// <summary>
    /// 待办清单主视图
    /// </summary>
    public partial class TodoListTabView : UserControl, IMicroTab
    {
        private readonly TodoListPlugin _plugin;
        private readonly TodoListTabViewModel _viewModel;

        // UI 控件引用
        private Button? _addTodoButton;
        private TextBox? _searchTextBox;
        private Button? _filterButton;
        private Button? _manageColumnsButton;
        private ItemsControl? _columnsControl;
        private StackPanel? _emptyState;
        private ComboBox? _filterFieldComboBox;
        private ComboBox? _filterOperatorComboBox;
        private TextBox? _filterValueTextBox;
        private Button? _addFilterButton;
        private ItemsControl? _activeFiltersControl;
        private Button? _clearFiltersButton;
        private Button? _applyFiltersButton;

        public TodoListTabView(TodoListPlugin plugin)
        {
            _plugin = plugin;
            _viewModel = new TodoListTabViewModel(plugin);

            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
            UpdateView();
        }

        public string TabName => "待办清单";

        public IconSymbolEnum IconSymbol => IconSymbolEnum.List;

        /// <summary>
        /// 公开插件实例供子控件使用
        /// </summary>
        public TodoListPlugin Plugin => _plugin;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _addTodoButton = this.FindControl<Button>("AddTodoButton");
            _searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            _filterButton = this.FindControl<Button>("FilterButton");
            _manageColumnsButton = this.FindControl<Button>("ManageColumnsButton");
            _columnsControl = this.FindControl<ItemsControl>("ColumnsControl");
            _emptyState = this.FindControl<StackPanel>("EmptyState");
            _filterFieldComboBox = this.FindControl<ComboBox>("FilterFieldComboBox");
            _filterOperatorComboBox = this.FindControl<ComboBox>("FilterOperatorComboBox");
            _filterValueTextBox = this.FindControl<TextBox>("FilterValueTextBox");
            _addFilterButton = this.FindControl<Button>("AddFilterButton");
            _activeFiltersControl = this.FindControl<ItemsControl>("ActiveFiltersControl");
            _clearFiltersButton = this.FindControl<Button>("ClearFiltersButton");
            _applyFiltersButton = this.FindControl<Button>("ApplyFiltersButton");

            // 设置 DataContext
            DataContext = _viewModel;

            // 初始化筛选字段下拉框
            if (_filterFieldComboBox != null)
            {
                _filterFieldComboBox.ItemsSource = _plugin.GetFilterableTemplates();
                _filterFieldComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
            }
        }

        private void AttachEventHandlers()
        {
            if (_addTodoButton != null)
            {
                _addTodoButton.Click += OnAddTodoClick;
            }

            if (_searchTextBox != null)
            {
                _searchTextBox.TextChanged += OnSearchTextChanged;
            }

            if (_manageColumnsButton != null)
            {
                _manageColumnsButton.Click += OnManageColumnsClick;
            }

            if (_addFilterButton != null)
            {
                _addFilterButton.Click += OnAddFilterClick;
            }

            if (_clearFiltersButton != null)
            {
                _clearFiltersButton.Click += OnClearFiltersClick;
            }

            if (_applyFiltersButton != null)
            {
                _applyFiltersButton.Click += OnApplyFiltersClick;
            }

            if (_filterFieldComboBox != null)
            {
                _filterFieldComboBox.SelectionChanged += OnFilterFieldChanged;
            }

            // 监听列变化
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.Columns))
                {
                    UpdateView();
                }
            };

            // 使用Loaded事件进行初始更新
            if (_columnsControl != null)
            {
                _columnsControl.Loaded += OnColumnsControlLoaded;
            }
        }

        private bool _isInitialized = false;

        private void OnColumnsControlLoaded(object? sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                // 延迟更新，确保容器已生成
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    UpdateColumnContents();
                }, Avalonia.Threading.DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// 刷新视图
        /// </summary>
        public void RefreshView()
        {
            _viewModel.LoadData();
            UpdateView();
        }

        private void UpdateView()
        {
            if (_emptyState != null && _columnsControl != null)
            {
                bool hasColumns = _viewModel.Columns.Count > 0;
                _emptyState.IsVisible = !hasColumns;
                _columnsControl.IsVisible = hasColumns;
            }

            // 延迟更新，避免死循环
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateColumnContents();
            }, Avalonia.Threading.DispatcherPriority.Background);
        }

        private bool _isUpdating = false;

        private void UpdateColumnContents()
        {
            if (_columnsControl == null || _isUpdating) return;

            _isUpdating = true;
            try
            {
                // 遍历所有列的视觉容器
                foreach (object? item in _columnsControl.Items)
                {
                    if (item is TodoColumn column)
                    {
                        int index = _columnsControl.Items.IndexOf(item);
                        Control? container = _columnsControl.ContainerFromIndex(index) as Control;
                        
                        if (container != null)
                        {
                            // 整个列容器都支持拖放，扩大检测范围
                            DragDrop.SetAllowDrop(container, true);
                            container.AddHandler(DragDrop.DragOverEvent, OnColumnDragOver);
                            container.AddHandler(DragDrop.DropEvent, OnColumnDrop);
                            container.DataContext = column;

                            // 找到 ItemsPanel
                            StackPanel? itemsPanel = container.FindDescendantOfType<StackPanel>("ItemsPanel");
                            
                            if (itemsPanel != null)
                            {
                                // 清空现有内容
                                itemsPanel.Children.Clear();

                                // 获取筛选后的待办事项
                                List<TodoItem> items = _viewModel.GetFilteredItemsForColumn(column.Id);

                                // 添加待办事项卡片
                                foreach (TodoItem todoItem in items)
                                {
                                    TodoCard card = new TodoCard(_plugin, this, todoItem);
                                    itemsPanel.Children.Add(card);
                                }

                                // 更新计数
                                TextBlock? countText = container.FindDescendantOfType<TextBlock>("ItemCountText");
                                if (countText != null)
                                {
                                    countText.Text = $"{items.Count} 项";
                                }
                            }

                            // 设置列颜色和标题背景色
                            if (!string.IsNullOrEmpty(column.Color))
                            {
                                try
                                {
                                    var colorBrush = new Avalonia.Media.SolidColorBrush(
                                        Avalonia.Media.Color.Parse(column.Color));
                                    
                                    // 设置标题背景色
                                    StackPanel? headerPanel = container.FindDescendantOfType<StackPanel>("ColumnHeaderPanel");
                                    if (headerPanel != null)
                                    {
                                        headerPanel.Background = colorBrush;
                                    }
                                    
                                    // 设置底部颜色条
                                    Avalonia.Controls.Border? colorBar = container.FindDescendantOfType<Avalonia.Controls.Border>();
                                    if (colorBar != null && colorBar.Height == 2)
                                    {
                                        colorBar.Background = colorBrush;
                                    }
                                }
                                catch
                                {
                                    // 颜色解析失败，忽略
                                }
                            }
                            else
                            {
                                // 如果没有颜色，清除标题背景色
                                StackPanel? headerPanel = container.FindDescendantOfType<StackPanel>("ColumnHeaderPanel");
                                if (headerPanel != null)
                                {
                                    headerPanel.Background = null;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private async void OnAddTodoClick(object? sender, RoutedEventArgs e)
        {
            // 如果没有列，提示用户先创建列
            if (_viewModel.Columns.Count == 0)
            {
                await ShowErrorDialogAsync("无法添加待办", "请先在设置中创建页签。");
                return;
            }

            // 默认添加到第一列
            string firstColumnId = _viewModel.Columns[0].Id;
            await ShowTodoEditDialog(null, firstColumnId);
        }


        private async System.Threading.Tasks.Task ShowTodoEditDialog(TodoItem? item, string? defaultColumnId = null)
        {
            var content = new TodoEditDialog(_plugin, item, defaultColumnId);
            var dialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = item == null ? "添加待办" : "编辑待办",
                Content = content,
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Primary
            };
            
            dialog.PrimaryButtonClick += (s, args) =>
            {
                args.Cancel = !content.Validate();
            };
            
            var result = await dialog.ShowAsync();
            if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
            {
                content.SaveTodo();
                RefreshView();
            }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_searchTextBox != null)
            {
                _viewModel.SearchText = _searchTextBox.Text ?? string.Empty;
                UpdateView();
            }
        }

        private async void OnManageColumnsClick(object? sender, RoutedEventArgs e)
        {
            var dialog = new FluentAvalonia.UI.Controls.ContentDialog
            {                
                Width = 400,
                Content = new ColumnSettingsView(_plugin),
                CloseButtonText = "关闭"
            };
            await dialog.ShowAsync();
            RefreshView(); // 关闭后刷新视图
        }

        private void OnFilterFieldChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_filterFieldComboBox?.SelectedItem is CustomFieldTemplate template && _filterOperatorComboBox != null)
            {
                // 根据字段类型更新可用的操作符
                List<FilterOperator> operators = template.FieldType switch
                {
                    FieldType.Text => new List<FilterOperator> { FilterOperator.Contains, FilterOperator.Equals },
                    FieldType.Number => new List<FilterOperator> 
                    { 
                        FilterOperator.Equals, 
                        FilterOperator.GreaterThan, 
                        FilterOperator.LessThan, 
                        FilterOperator.GreaterOrEqual, 
                        FilterOperator.LessOrEqual 
                    },
                    FieldType.Date => new List<FilterOperator> 
                    { 
                        FilterOperator.Equals, 
                        FilterOperator.GreaterThan, 
                        FilterOperator.LessThan 
                    },
                    FieldType.Bool => new List<FilterOperator> { FilterOperator.Equals },
                    _ => new List<FilterOperator>()
                };

                _filterOperatorComboBox.ItemsSource = operators;
            }
        }

        private void OnAddFilterClick(object? sender, RoutedEventArgs e)
        {
            if (_filterFieldComboBox?.SelectedItem is CustomFieldTemplate template &&
                _filterOperatorComboBox?.SelectedItem is FilterOperator op &&
                !string.IsNullOrWhiteSpace(_filterValueTextBox?.Text))
            {
                FilterCondition condition = new FilterCondition
                {
                    FieldId = template.Id,
                    FieldName = template.Name,
                    FieldType = template.FieldType,
                    Value = _filterValueTextBox.Text,
                    Operator = op
                };

                _viewModel.AddFilter(condition);

                // 清空输入
                if (_filterFieldComboBox != null) _filterFieldComboBox.SelectedIndex = -1;
                if (_filterOperatorComboBox != null) _filterOperatorComboBox.SelectedIndex = -1;
                if (_filterValueTextBox != null) _filterValueTextBox.Text = string.Empty;
            }
        }

        private void OnClearFiltersClick(object? sender, RoutedEventArgs e)
        {
            _viewModel.ClearFilters();
        }

        private void OnApplyFiltersClick(object? sender, RoutedEventArgs e)
        {
            UpdateView();
            
            // 关闭 Flyout
            if (_filterButton?.Flyout is Flyout flyout)
            {
                flyout.Hide();
            }
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        private async System.Threading.Tasks.Task ShowErrorDialogAsync(string title, string message)
        {
            var errorDialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定"
            };
            await errorDialog.ShowAsync();
        }

        /// <summary>
        /// 处理列容器的拖拽悬停事件
        /// </summary>
        private void OnColumnDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.Move;
        }

        /// <summary>
        /// 处理列容器的拖拽放置事件
        /// </summary>
        private void OnColumnDrop(object? sender, DragEventArgs e)
        {
            if (sender is Control container && container.DataContext is TodoColumn targetColumn)
            {
                // 从拖拽数据中获取待办事项ID
                if (e.Data.Contains("TodoItemId") && e.Data.Get("TodoItemId") is string todoId)
                {
                    TodoItem? item = _plugin.GetAllTodos().FirstOrDefault(t => t.Id == todoId);
                    if (item != null && item.ColumnId != targetColumn.Id)
                    {
                        // 更新待办事项的列ID
                        item.ColumnId = targetColumn.Id;
                        _plugin.UpdateTodo(item);
                        RefreshView();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 扩展方法：查找视觉树中的子元素
    /// </summary>
    public static class VisualExtensions
    {
        public static T? FindDescendantOfType<T>(this Control control, string? name = null) where T : Control
        {
            System.Diagnostics.Debug.WriteLine($"FindDescendantOfType: 检查 {control.GetType().Name} (Name={control.Name})");
            
            if (control is T result && (name == null || control.Name == name))
            {
                System.Diagnostics.Debug.WriteLine($"  -> 找到匹配: {control.Name}");
                return result;
            }

            // 尝试使用 Visual Tree Helper 进行深度搜索
            int childCount = Avalonia.VisualTree.VisualExtensions.GetVisualChildren(control).Count();
            System.Diagnostics.Debug.WriteLine($"  -> 子元素数量: {childCount}");
            
            foreach (var child in Avalonia.VisualTree.VisualExtensions.GetVisualChildren(control))
            {
                if (child is Control childControl)
                {
                    T? found = FindDescendantOfType<T>(childControl, name);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        public static T? FindAncestorOfType<T>(this Control control) where T : class
        {
            Control? parent = control.Parent as Control;
            while (parent != null)
            {
                if (parent is T result)
                {
                    return result;
                }
                parent = parent.Parent as Control;
            }
            return null;
        }
    }
}

