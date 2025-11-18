using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using TodoListPlugin.Models;

namespace TodoListPlugin.Views
{
    /// <summary>
    /// 页签管理设置视图
    /// </summary>
    public partial class ColumnSettingsView : UserControl
    {
        private readonly TodoListPlugin _plugin;

        private TextBox? _newColumnNameTextBox;
        private ComboBox? _colorComboBox;
        private Button? _addColumnButton;
        private ItemsControl? _columnsListControl;

        public ColumnSettingsView(TodoListPlugin plugin)
        {
            _plugin = plugin;

            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
            LoadColumns();
            
            // 加载字段模板视图
            if (this.FindControl<ContentControl>("FieldTemplateContent") is ContentControl content)
            {
                content.Content = new FieldTemplateSettingsView(_plugin);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _newColumnNameTextBox = this.FindControl<TextBox>("NewColumnNameTextBox");
            _colorComboBox = this.FindControl<ComboBox>("ColorComboBox");
            _addColumnButton = this.FindControl<Button>("AddColumnButton");
            _columnsListControl = this.FindControl<ItemsControl>("ColumnsListControl");
        }

        private void AttachEventHandlers()
        {
            if (_addColumnButton != null)
            {
                _addColumnButton.Click += OnAddColumnClick;
            }
        }

        private void LoadColumns()
        {
            if (_columnsListControl == null) return;

            List<TodoColumn> columns = _plugin.GetColumns();
            _columnsListControl.ItemsSource = null;
            _columnsListControl.ItemsSource = columns;

            // 延迟更新容器属性，确保容器已生成
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateColumnContainers();
            }, Avalonia.Threading.DispatcherPriority.Background);
        }

        private void UpdateColumnContainers()
        {
            if (_columnsListControl == null) return;

            List<TodoColumn> columns = _plugin.GetColumns();
            for (int i = 0; i < columns.Count; i++)
            {
                TodoColumn column = columns[i];
                Control? container = _columnsListControl.ContainerFromIndex(i) as Control;
                
                if (container != null)
                {
                    // 查找颜色指示器
                    if (container.FindDescendantOfType<Border>("ColorIndicator") is Border colorIndicator)
                    {
                        if (!string.IsNullOrEmpty(column.Color))
                        {
                            colorIndicator.Background = new Avalonia.Media.SolidColorBrush(
                                Avalonia.Media.Color.Parse(column.Color));
                        }
                        else
                        {
                            colorIndicator.Background = Avalonia.Media.Brushes.Transparent;
                        }
                    }

                    // 查找计数文本
                    if (container.FindDescendantOfType<TextBlock>("ItemCountText") is TextBlock countText)
                    {
                        int itemCount = _plugin.GetAllTodos().Count(item => item.ColumnId == column.Id);
                        countText.Text = $"{itemCount} 个待办";
                    }
                }
            }
        }

        private async void OnAddColumnClick(object? sender, RoutedEventArgs e)
        {
            if (_newColumnNameTextBox == null) return;

            string name = _newColumnNameTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                await ShowErrorDialogAsync("输入错误", "页签名称不能为空。");
                return;
            }

            try
            {
                string? color = null;
                if (_colorComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string colorValue)
                {
                    color = colorValue;
                }

                _plugin.AddColumn(name, color);

                // 清空输入
                _newColumnNameTextBox.Text = string.Empty;
                if (_colorComboBox != null)
                {
                    _colorComboBox.SelectedIndex = -1;
                }

                // 刷新列表
                LoadColumns();
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("添加失败", ex.Message);
            }
        }

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

        private async void OnEditColumnClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TodoColumn column)
            {
                // 创建编辑对话框内容
                StackPanel panel = new StackPanel { Spacing = 16 };

                // 名称输入
                StackPanel namePanel = new StackPanel { Spacing = 6 };
                namePanel.Children.Add(new TextBlock { Text = "页签名称", FontWeight = Avalonia.Media.FontWeight.Medium });
                TextBox nameTextBox = new TextBox { Text = column.Name, MaxLength = 20 };
                namePanel.Children.Add(nameTextBox);
                panel.Children.Add(namePanel);

                // 颜色选择
                StackPanel colorPanel = new StackPanel { Spacing = 6 };
                colorPanel.Children.Add(new TextBlock { Text = "页签颜色", FontWeight = Avalonia.Media.FontWeight.Medium });
                ComboBox colorCombo = new ComboBox
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    PlaceholderText = "选择颜色..."
                };
                colorCombo.Items.Add(new ComboBoxItem { Content = "蓝色", Tag = "#FF6B9BD1" });
                colorCombo.Items.Add(new ComboBoxItem { Content = "橙色", Tag = "#FFFFA500" });
                colorCombo.Items.Add(new ComboBoxItem { Content = "绿色", Tag = "#FF90EE90" });
                colorCombo.Items.Add(new ComboBoxItem { Content = "紫色", Tag = "#FFB19CD9" });
                colorCombo.Items.Add(new ComboBoxItem { Content = "红色", Tag = "#FFFF6B6B" });
                colorCombo.Items.Add(new ComboBoxItem { Content = "黄色", Tag = "#FFFFD93D" });
                colorCombo.Items.Add(new ComboBoxItem { Content = "青色", Tag = "#FF6BCB77" });
                
                // 设置当前颜色
                if (!string.IsNullOrEmpty(column.Color))
                {
                    foreach (object? item in colorCombo.Items)
                    {
                        if (item is ComboBoxItem cbItem && cbItem.Tag?.ToString() == column.Color)
                        {
                            colorCombo.SelectedItem = cbItem;
                            break;
                        }
                    }
                }
                
                colorPanel.Children.Add(colorCombo);
                panel.Children.Add(colorPanel);

                var dialog = new FluentAvalonia.UI.Controls.ContentDialog
                {
                    Title = "编辑页签",
                    Content = panel,
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消",
                    DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Primary
                };

                dialog.PrimaryButtonClick += (s, args) =>
                {
                    string newName = nameTextBox.Text?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        args.Cancel = true;
                        return;
                    }

                    try
                    {
                        string? newColor = null;
                        if (colorCombo.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string colorValue)
                        {
                            newColor = colorValue;
                        }

                        _plugin.UpdateColumn(column.Id, newName, newColor);
                    }
                catch (Exception ex)
                {
                    args.Cancel = true;
                    _ = ShowErrorDialogAsync("保存失败", ex.Message);
                }
                };

                var result = await dialog.ShowAsync();
                if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
                {
                    LoadColumns();
                }
            }
        }

        private async void OnDeleteColumnClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TodoColumn column)
            {
                var confirmDialog = new FluentAvalonia.UI.Controls.ContentDialog
                {
                    Title = "确认删除",
                    Content = $"确定要删除页签 \"{column.Name}\" 吗？该页签中的所有待办事项也将被删除。",
                    PrimaryButtonText = "删除",
                    CloseButtonText = "取消",
                    DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Close
                };

                var result = await confirmDialog.ShowAsync();
                if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
                {
                    try
                    {
                        _plugin.DeleteColumn(column.Id);
                        LoadColumns();
                    }
                    catch (Exception ex)
                    {
                        var errorDialog = new FluentAvalonia.UI.Controls.ContentDialog
                        {
                            Title = "删除失败",
                            Content = ex.Message,
                            CloseButtonText = "确定"
                        };
                        await errorDialog.ShowAsync();
                    }
                }
            }
        }
    }
}

