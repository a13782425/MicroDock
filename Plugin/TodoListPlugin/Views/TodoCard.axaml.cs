using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using System;
using System.Linq;
using TodoListPlugin.Models;

namespace TodoListPlugin.Views
{
    /// <summary>
    /// 待办事项卡片
    /// </summary>
    public partial class TodoCard : UserControl
    {
        private readonly TodoListPlugin _plugin;
        private readonly TodoListTabView _parentView;
        private readonly TodoItem _item;

        private Border? _cardBorder;
        private TextBlock? _titleText;
        private TextBlock? _descriptionText;
        private TextBlock? _reminderIcon;
        private TextBlock? _createdTimeText;
        private WrapPanel? _customFieldsPanel;
        private MenuItem? _editMenuItem;
        private MenuItem? _deleteMenuItem;

        // 优先级相关控件
        private Button? _priorityButton;
        private TextBlock? _priorityButtonText;
        private ComboBox? _priorityComboBox;
        private ItemsControl? _prioritiesListControl;
        private TextBox? _newPriorityNameTextBox;
        private Button? _addPriorityButton;

        // 标签相关控件
        private Button? _tagButton;
        private TextBlock? _tagButtonText;
        private ComboBox? _tagComboBox;
        private WrapPanel? _currentTagsPanel;
        private ItemsControl? _tagsListControl;

        // 拖拽相关字段
        private Point _pressedPoint;
        private bool _isDragging = false;

        public TodoCard(TodoListPlugin plugin, TodoListTabView parentView, TodoItem item)
        {
            _plugin = plugin;
            _parentView = parentView;
            _item = item;

            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
            UpdateContent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _cardBorder = this.FindControl<Border>("CardBorder");
            _titleText = this.FindControl<TextBlock>("TitleText");
            _descriptionText = this.FindControl<TextBlock>("DescriptionText");
            _reminderIcon = this.FindControl<TextBlock>("ReminderIcon");
            _createdTimeText = this.FindControl<TextBlock>("CreatedTimeText");
            _customFieldsPanel = this.FindControl<WrapPanel>("CustomFieldsPanel");
            _editMenuItem = this.FindControl<MenuItem>("EditMenuItem");
            _deleteMenuItem = this.FindControl<MenuItem>("DeleteMenuItem");

            // 优先级控件
            _priorityButton = this.FindControl<Button>("PriorityButton");
            _priorityButtonText = this.FindControl<TextBlock>("PriorityButtonText");
            _priorityComboBox = this.FindControl<ComboBox>("PriorityComboBox");
            _prioritiesListControl = this.FindControl<ItemsControl>("PrioritiesListControl");
            _newPriorityNameTextBox = this.FindControl<TextBox>("NewPriorityNameTextBox");
            _addPriorityButton = this.FindControl<Button>("AddPriorityButton");

            // 标签控件
            _tagButton = this.FindControl<Button>("TagButton");
            _tagButtonText = this.FindControl<TextBlock>("TagButtonText");
            _tagComboBox = this.FindControl<ComboBox>("TagComboBox");
            _currentTagsPanel = this.FindControl<WrapPanel>("CurrentTagsPanel");
            _tagsListControl = this.FindControl<ItemsControl>("TagsListControl");
        }

        private void AttachEventHandlers()
        {
            // 卡片点击事件
            if (_cardBorder != null)
            {
                _cardBorder.PointerPressed += OnCardPointerPressed;
                _cardBorder.PointerMoved += OnCardPointerMoved;
                _cardBorder.PointerReleased += OnCardPointerReleased;
            }

            // 菜单项事件
            if (_editMenuItem != null)
            {
                _editMenuItem.Click += OnEditClick;
            }

            if (_deleteMenuItem != null)
            {
                _deleteMenuItem.Click += OnDeleteClick;
            }

            // 优先级事件
            if (_priorityButton != null)
            {
                _priorityButton.Click += OnPriorityButtonClick;
            }
            if (_priorityComboBox != null)
            {
                _priorityComboBox.SelectionChanged += OnPrioritySelectionChanged;
            }
            if (_addPriorityButton != null)
            {
                _addPriorityButton.Click += OnAddPriorityButtonClick;
            }

            // 标签事件
            if (_tagButton != null)
            {
                _tagButton.Click += OnTagButtonClick;
            }
            if (_tagComboBox != null)
            {
                _tagComboBox.SelectionChanged += OnTagSelectionChanged;
                _tagComboBox.KeyDown += OnTagComboBoxKeyDown;
            }
        }

        private void UpdateContent()
        {
            // 更新标题
            if (_titleText != null)
            {
                _titleText.Text = _item.Title;
            }

            // 更新简述
            if (_descriptionText != null)
            {
                _descriptionText.Text = string.IsNullOrWhiteSpace(_item.Description) ? "无描述" : _item.Description;
                _descriptionText.Opacity = string.IsNullOrWhiteSpace(_item.Description) ? 0.4 : 0.7;
            }

            // 更新提醒图标
            if (_reminderIcon != null)
            {
                _reminderIcon.IsVisible = _item.IsReminderEnabled;
            }

            // 更新创建时间
            if (_createdTimeText != null)
            {
                _createdTimeText.Text = $"创建于 {_item.CreatedTime:yyyy-MM-dd HH:mm}";
            }

            // 更新优先级显示
            UpdatePriorityDisplay();

            // 更新标签显示
            UpdateTagDisplay();

            // 更新自定义字段
            UpdateCustomFields();
        }

        private void UpdateCustomFields()
        {
            if (_customFieldsPanel == null) return;

            _customFieldsPanel.Children.Clear();

            // 获取所有字段模板
            System.Collections.Generic.List<CustomFieldTemplate> templates = _plugin.GetFieldTemplates();

            foreach (CustomFieldTemplate template in templates)
            {
                if (_item.CustomFields.TryGetValue(template.Id, out string? value) && !string.IsNullOrWhiteSpace(value))
                {
                    // 检查是否为标签类型字段（优先级或标签）
                    if (template.Id == "default-priority" || template.Id == "default-tags")
                    {
                        // 使用标签样式显示
                        Border tagBorder = new Border
                        {
                            Margin = new Avalonia.Thickness(0, 0, 6, 6),
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                        };
                        tagBorder.Classes.Add("tag");

                        TextBlock tagText = new TextBlock
                        {
                            Text = value,
                            FontSize = 10,
                            Foreground = Avalonia.Media.Brushes.White
                        };
                        tagBorder.Child = tagText;

                        _customFieldsPanel.Children.Add(tagBorder);
                    }
                    else
                    {
                        // 普通字段显示
                        StackPanel fieldPanel = new StackPanel
                        {
                            Spacing = 2
                        };

                        // 字段名称
                        TextBlock nameText = new TextBlock
                        {
                            Text = template.Name,
                            FontSize = 10,
                            Opacity = 0.6,
                            FontWeight = Avalonia.Media.FontWeight.Medium
                        };
                        fieldPanel.Children.Add(nameText);

                        // 字段值
                        string displayValue = FormatFieldValue(value, template.FieldType);
                        TextBlock valueText = new TextBlock
                        {
                            Text = displayValue,
                            FontSize = 11,
                            Opacity = 0.8,
                            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                        };
                        fieldPanel.Children.Add(valueText);

                        _customFieldsPanel.Children.Add(fieldPanel);
                    }
                }
            }
        }

        private string FormatFieldValue(string value, FieldType fieldType)
        {
            return fieldType switch
            {
                FieldType.Date => DateTime.TryParse(value, out DateTime date) ? date.ToString("yyyy-MM-dd") : value,
                FieldType.Bool => value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "是" : "否",
                _ => value
            };
        }

        private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            PointerPoint point = e.GetCurrentPoint(this);
            if (point.Properties.IsLeftButtonPressed)
            {
                _pressedPoint = point.Position;
                _isDragging = false;
            }
        }

        private async void OnCardPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isDragging && _pressedPoint != default)
            {
                PointerPoint point = e.GetCurrentPoint(this);
                double distance = Math.Sqrt(
                    Math.Pow(point.Position.X - _pressedPoint.X, 2) + 
                    Math.Pow(point.Position.Y - _pressedPoint.Y, 2));
                
                if (distance > 5) // 移动超过5像素才算拖拽
                {
                    _isDragging = true;
                    
                    // 创建拖放数据并开始拖拽
#pragma warning disable CS0618 // 类型或成员已过时
                    var dataObject = new DataObject();
                    dataObject.Set("TodoItem", _item);
                    
                    var args = new PointerPressedEventArgs(
                        e.Source!,
                        e.Pointer,
                        (Visual)e.Source!,
                        point.Position,
                        e.Timestamp,
                        new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
                        e.KeyModifiers);
                    
                    await DragDrop.DoDragDrop(args, dataObject, DragDropEffects.Move);
#pragma warning restore CS0618
                }
            }
        }

        private async void OnCardPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging && _pressedPoint != default)
            {
                // 这是点击，不是拖拽 - 打开编辑对话框
                await EditItem();
            }
            _pressedPoint = default;
            _isDragging = false;
        }

        private async void OnEditClick(object? sender, RoutedEventArgs e)
        {
            await EditItem();
        }

        private async System.Threading.Tasks.Task EditItem()
        {
            var content = new TodoEditDialog(_plugin, _item);
            var dialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = "编辑待办",
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
                _parentView.RefreshView();
            }
        }

        private async void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            var confirmDialog = new FluentAvalonia.UI.Controls.ContentDialog
            {
                Title = "确认删除",
                Content = $"确定要删除待办 \"{_item.Title}\" 吗？此操作无法撤销。",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Close
            };

            var result = await confirmDialog.ShowAsync();
            if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
            {
                try
                {
                    _plugin.DeleteTodo(_item.Id);
                    _parentView.RefreshView();
                }
                catch (Exception ex)
                {
                    var errorDialog = new FluentAvalonia.UI.Controls.ContentDialog
                    {
                        Title = "删除失败",
                        Content = $"删除待办时发生错误：{ex.Message}",
                        CloseButtonText = "确定"
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        #region 优先级管理

        private void UpdatePriorityDisplay()
        {
            if (_priorityButtonText == null) return;

            if (!string.IsNullOrEmpty(_item.PriorityName))
            {
                _priorityButtonText.Text = _item.PriorityName;
            }
            else
            {
                _priorityButtonText.Text = "无优先级";
            }
        }

        private void OnPriorityButtonClick(object? sender, RoutedEventArgs e)
        {
            LoadPrioritiesData();
        }

        private void LoadPrioritiesData()
        {
            if (_plugin == null || _priorityComboBox == null || _prioritiesListControl == null) return;

            List<PriorityGroup> priorities = _plugin.GetPriorities();

            // 设置 ComboBox 数据源
            List<string> priorityNames = priorities.Select(p => p.Name).ToList();
            priorityNames.Insert(0, string.Empty); // 添加"无优先级"选项
            _priorityComboBox.ItemsSource = priorityNames;

            // 设置当前选择
            if (!string.IsNullOrEmpty(_item.PriorityName))
            {
                _priorityComboBox.SelectedItem = _item.PriorityName;
            }
            else
            {
                _priorityComboBox.SelectedIndex = 0;
            }

            // 设置优先级列表
            _prioritiesListControl.ItemsSource = priorities;
        }

        private void OnPrioritySelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_priorityComboBox == null || _plugin == null) return;

            string? selectedPriority = _priorityComboBox.SelectedItem as string;
            string? newPriorityName = string.IsNullOrEmpty(selectedPriority) ? null : selectedPriority;

            // 如果优先级变化了，保存
            if (newPriorityName != _item.PriorityName)
            {
                _plugin.UpdateTodo(_item);
                _item.PriorityName = newPriorityName;
                UpdatePriorityDisplay();
                _parentView.RefreshView();
            }
        }

        private void OnAddPriorityButtonClick(object? sender, RoutedEventArgs e)
        {
            AddNewPriority();
        }

        private void AddNewPriority()
        {
            if (_newPriorityNameTextBox == null || _plugin == null) return;

            string? newPriorityName = _newPriorityNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(newPriorityName)) return;

            // 检查是否已存在
            if (_plugin.GetPriorities().Any(p => p.Name == newPriorityName))
            {
                return; // 已存在，不添加
            }

            try
            {
                // 添加新优先级
                _plugin.AddPriority(newPriorityName);
                _newPriorityNameTextBox.Text = string.Empty;

                // 重新加载优先级列表
                LoadPrioritiesData();
            }
            catch
            {
                // 错误处理
            }
        }

        public void DeletePriorityButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PriorityGroup priority && _plugin != null)
            {
                try
                {
                    // 检查是否有待办事项使用该优先级
                    int usageCount = _plugin.GetPriorityUsageCount(priority.Name);
                    if (usageCount > 0)
                    {
                        return; // 有使用，不能删除
                    }

                    // 删除优先级
                    _plugin.DeletePriority(priority.Id);

                    // 重新加载优先级列表
                    LoadPrioritiesData();
                }
                catch
                {
                    // 错误处理
                }
            }
        }

        #endregion

        #region 标签管理

        private void UpdateTagDisplay()
        {
            if (_tagButtonText == null) return;

            if (_item.Tags != null && _item.Tags.Count > 0)
            {
                _tagButtonText.Text = $"{_item.Tags.Count} 个标签";
            }
            else
            {
                _tagButtonText.Text = "无标签";
            }
        }

        private void OnTagButtonClick(object? sender, RoutedEventArgs e)
        {
            LoadTagsData();
        }

        private void LoadTagsData()
        {
            if (_plugin == null || _tagComboBox == null || _tagsListControl == null || _currentTagsPanel == null) return;

            List<TagGroup> tags = _plugin.GetTags();

            // 设置 ComboBox 数据源
            List<string> tagNames = tags.Select(t => t.Name).ToList();
            _tagComboBox.ItemsSource = tagNames;

            // 更新当前标签显示
            _currentTagsPanel.Children.Clear();
            if (_item.Tags != null)
            {
                foreach (string tagName in _item.Tags)
                {
                    Border tagBorder = new Border
                    {
                        Margin = new Avalonia.Thickness(0, 0, 4, 4),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };
                    tagBorder.Classes.Add("tag");

                    StackPanel tagPanel = new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 4
                    };

                    TextBlock tagText = new TextBlock
                    {
                        Text = tagName,
                        FontSize = 10,
                        Foreground = Avalonia.Media.Brushes.White,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };
                    tagPanel.Children.Add(tagText);

                    Button removeButton = new Button
                    {
                        Content = "×",
                        Padding = new Avalonia.Thickness(4, 0),
                        FontSize = 12,
                        Background = Avalonia.Media.Brushes.Transparent,
                        Foreground = Avalonia.Media.Brushes.White,
                        Tag = tagName
                    };
                    removeButton.Click += OnRemoveTagClick;
                    tagPanel.Children.Add(removeButton);

                    tagBorder.Child = tagPanel;
                    _currentTagsPanel.Children.Add(tagBorder);
                }
            }

            // 设置标签列表
            _tagsListControl.ItemsSource = tags;
        }

        private void OnTagSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_tagComboBox == null || _plugin == null) return;

            string? selectedTag = _tagComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedTag)) return;

            AddTagToItem(selectedTag);
        }

        private void OnTagComboBoxKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter && _tagComboBox != null)
            {
                string? newTagName = _tagComboBox.Text?.Trim();
                if (!string.IsNullOrEmpty(newTagName))
                {
                    AddTagToItem(newTagName);
                    _tagComboBox.Text = string.Empty;
                }
                e.Handled = true;
            }
        }

        private void AddTagToItem(string tagName)
        {
            if (_plugin == null) return;

            // 检查是否已存在该标签组，如果不存在则创建
            if (!_plugin.GetTags().Any(t => t.Name == tagName))
            {
                try
                {
                    _plugin.AddTag(tagName);
                }
                catch
                {
                    // 标签已存在或其他错误，继续处理
                }
            }

            // 检查是否已存在该标签
            if (_item.Tags == null)
            {
                _item.Tags = new List<string>();
            }

            if (!_item.Tags.Contains(tagName))
            {
                _item.Tags.Add(tagName);
                _plugin.UpdateTodo(_item);
                LoadTagsData();
                UpdateTagDisplay();
                _parentView.RefreshView();
            }

            // 清空选择
            if (_tagComboBox != null)
            {
                _tagComboBox.SelectedIndex = -1;
                _tagComboBox.Text = string.Empty;
            }
        }

        private void OnRemoveTagClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagName && _item.Tags != null)
            {
                _item.Tags.Remove(tagName);
                _plugin?.UpdateTodo(_item);
                LoadTagsData();
                UpdateTagDisplay();
                _parentView.RefreshView();
            }
        }

        public void DeleteTagButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TagGroup tag && _plugin != null)
            {
                try
                {
                    // 检查是否有待办事项使用该标签
                    int usageCount = _plugin.GetTagUsageCount(tag.Name);
                    if (usageCount > 0)
                    {
                        return; // 有使用，不能删除
                    }

                    // 删除标签
                    _plugin.DeleteTag(tag.Id);

                    // 重新加载标签列表
                    LoadTagsData();
                }
                catch
                {
                    // 错误处理
                }
            }
        }

        #endregion
    }
}

