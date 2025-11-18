using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TodoListPlugin.Models;

namespace TodoListPlugin.Views
{
    /// <summary>
    /// 待办事项编辑对话框
    /// </summary>
    public partial class TodoEditDialog : UserControl
    {
        private readonly TodoListPlugin _plugin;
        private readonly TodoItem? _editingItem;
        private readonly string? _defaultColumnId;
        private readonly Dictionary<string, Control> _fieldControls = new Dictionary<string, Control>();

        private TextBox? _titleTextBox;
        private TextBox? _descriptionTextBox;
        private ComboBox? _columnComboBox;
        private ComboBox? _priorityComboBox;
        private ComboBox? _tagComboBox;
        private WrapPanel? _selectedTagsPanel;
        private StackPanel? _customFieldsContainer;
        private ToggleSwitch? _reminderToggle;
        private ComboBox? _reminderIntervalComboBox;

        public TodoEditDialog(TodoListPlugin plugin, TodoItem? item = null, string? defaultColumnId = null)
        {
            _plugin = plugin;
            _editingItem = item;
            _defaultColumnId = defaultColumnId;

            InitializeComponent();
            InitializeControls();
            LoadData();
            GenerateCustomFields();

            if (_editingItem != null)
            {
                LoadItemData();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _titleTextBox = this.FindControl<TextBox>("TitleTextBox");
            _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
            _columnComboBox = this.FindControl<ComboBox>("ColumnComboBox");
            _priorityComboBox = this.FindControl<ComboBox>("PriorityComboBox");
            _tagComboBox = this.FindControl<ComboBox>("TagComboBox");
            _selectedTagsPanel = this.FindControl<WrapPanel>("SelectedTagsPanel");
            _customFieldsContainer = this.FindControl<StackPanel>("CustomFieldsContainer");
            _reminderToggle = this.FindControl<ToggleSwitch>("ReminderToggle");
            _reminderIntervalComboBox = this.FindControl<ComboBox>("ReminderIntervalComboBox");

            // 设置优先级和标签事件
            if (_priorityComboBox != null)
            {
                _priorityComboBox.SelectionChanged += OnPrioritySelectionChanged;
            }
            if (_tagComboBox != null)
            {
                _tagComboBox.SelectionChanged += OnTagSelectionChanged;
                _tagComboBox.KeyDown += OnTagComboBoxKeyDown;
            }
        }

        private List<string> _selectedTags = new List<string>();

        private void LoadData()
        {
            // 加载页签列表
            if (_columnComboBox != null)
            {
                List<TodoColumn> columns = _plugin.GetColumns();
                _columnComboBox.ItemsSource = columns;
                _columnComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Name");

                // 设置默认选中
                if (_defaultColumnId != null)
                {
                    TodoColumn? defaultColumn = columns.FirstOrDefault(c => c.Id == _defaultColumnId);
                    if (defaultColumn != null)
                    {
                        _columnComboBox.SelectedItem = defaultColumn;
                    }
                }
                else if (_editingItem != null)
                {
                    TodoColumn? editingColumn = columns.FirstOrDefault(c => c.Id == _editingItem.ColumnId);
                    if (editingColumn != null)
                    {
                        _columnComboBox.SelectedItem = editingColumn;
                    }
                }
            }

            // 加载优先级列表
            if (_priorityComboBox != null)
            {
                List<PriorityGroup> priorities = _plugin.GetPriorities();
                List<string> priorityNames = priorities.Select(p => p.Name).ToList();
                priorityNames.Insert(0, string.Empty); // 添加"无优先级"选项
                _priorityComboBox.ItemsSource = priorityNames;
            }

            // 加载标签列表
            if (_tagComboBox != null)
            {
                List<TagGroup> tags = _plugin.GetTags();
                List<string> tagNames = tags.Select(t => t.Name).ToList();
                _tagComboBox.ItemsSource = tagNames;
            }
        }

        private void GenerateCustomFields()
        {
            if (_customFieldsContainer == null) return;

            // 只获取用户自定义字段，排除默认字段（标题、简述、优先级、标签已有专门控件）
            List<CustomFieldTemplate> templates = _plugin.GetUserFieldTemplates();
            
            foreach (CustomFieldTemplate template in templates.OrderBy(t => t.Order))
            {
                // 创建字段标签
                TextBlock label = new TextBlock
                {
                    Text = template.Name,
                    FontWeight = Avalonia.Media.FontWeight.Medium,
                    FontSize = 14
                };
                _customFieldsContainer.Children.Add(label);

                // 根据类型创建输入控件
                Control inputControl = CreateInputFieldForType(template);
                _fieldControls[template.Id] = inputControl;
                _customFieldsContainer.Children.Add(inputControl);
            }
        }

        private Control CreateInputFieldForType(CustomFieldTemplate template)
        {
            return template.FieldType switch
            {
                FieldType.Text => new TextBox
                {
                    Watermark = $"输入{template.Name}...",
                    AcceptsReturn = true,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    MinHeight = 60,
                    MaxHeight = 120
                },
                FieldType.Bool => new ToggleSwitch
                {
                    OnContent = "是",
                    OffContent = "否"
                },
                FieldType.Date => new DatePicker
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
                },
                FieldType.Number => new TextBox
                {
                    Watermark = $"输入数字..."
                },
                FieldType.Select => new ComboBox
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    PlaceholderText = $"选择{template.Name}...",
                    ItemsSource = template.Options ?? new List<string>(),
                    IsEditable = false
                },
                _ => new TextBox { Watermark = "未知类型" }
            };
        }

        private void LoadItemData()
        {
            if (_editingItem == null) return;

            // 加载基础信息
            if (_titleTextBox != null)
            {
                _titleTextBox.Text = _editingItem.Title;
            }

            if (_descriptionTextBox != null)
            {
                _descriptionTextBox.Text = _editingItem.Description;
            }

            // 加载自定义字段值
            foreach (KeyValuePair<string, Control> kvp in _fieldControls)
            {
                if (_editingItem.CustomFields.TryGetValue(kvp.Key, out string? value))
                {
                    SetControlValue(kvp.Value, value);
                }
            }

            // 加载优先级
            if (_priorityComboBox != null && !string.IsNullOrEmpty(_editingItem.PriorityName))
            {
                _priorityComboBox.SelectedItem = _editingItem.PriorityName;
            }

            // 加载标签
            if (_editingItem.Tags != null && _editingItem.Tags.Count > 0)
            {
                _selectedTags = new List<string>(_editingItem.Tags);
                UpdateSelectedTagsDisplay();
            }

            // 加载提醒设置
            if (_editingItem != null && _reminderToggle != null && _reminderIntervalComboBox != null)
            {
                _reminderToggle.IsChecked = _editingItem.IsReminderEnabled;
                
                if (_editingItem.IsReminderEnabled && _editingItem.ReminderIntervalType != ReminderInterval.None)
                {
                    string intervalTag = _editingItem.ReminderIntervalType.ToString();
                    foreach (object? item in _reminderIntervalComboBox.Items)
                    {
                        if (item is ComboBoxItem cbItem && cbItem.Tag?.ToString() == intervalTag)
                        {
                            _reminderIntervalComboBox.SelectedItem = cbItem;
                            break;
                        }
                    }
                }
            }
        }

        private void SetControlValue(Control control, string value)
        {
            switch (control)
            {
                case TextBox textBox:
                    textBox.Text = value;
                    break;
                case ToggleSwitch toggle:
                    toggle.IsChecked = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case DatePicker datePicker:
                    if (DateTime.TryParse(value, out DateTime date))
                    {
                        datePicker.SelectedDate = date;
                    }
                    break;
                case ComboBox comboBox:
                    comboBox.SelectedItem = value;
                    break;
            }
        }

        private string? GetControlValue(Control control)
        {
            return control switch
            {
                TextBox textBox => textBox.Text,
                ToggleSwitch toggle => toggle.IsChecked == true ? "true" : "false",
                DatePicker datePicker => datePicker.SelectedDate?.ToString("yyyy-MM-dd"),
                ComboBox comboBox => comboBox.SelectedItem?.ToString(),
                _ => null
            };
        }

        /// <summary>
        /// 验证表单数据
        /// </summary>
        public bool Validate()
        {
            if (_titleTextBox == null || string.IsNullOrWhiteSpace(_titleTextBox.Text))
            {
                return false;
            }

            if (_columnComboBox?.SelectedItem is not TodoColumn)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取当前选择的列ID
        /// </summary>
        public string? GetColumnId()
        {
            if (_columnComboBox?.SelectedItem is TodoColumn selectedColumn)
            {
                return selectedColumn.Id;
            }
            return null;
        }

        /// <summary>
        /// 保存待办事项
        /// </summary>
        public void SaveTodo()
        {
            if (!Validate()) return;

            TodoColumn? selectedColumn = _columnComboBox!.SelectedItem as TodoColumn;
            if (selectedColumn == null) return;

            if (_editingItem != null)
            {
                // 更新现有项
                _editingItem.Title = _titleTextBox!.Text!;
                _editingItem.Description = _descriptionTextBox?.Text ?? string.Empty;
                _editingItem.ColumnId = selectedColumn.Id;

                // 更新自定义字段
                foreach (KeyValuePair<string, Control> kvp in _fieldControls)
                {
                    string? value = GetControlValue(kvp.Value);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _editingItem.CustomFields[kvp.Key] = value;
                    }
                }

                // 更新优先级
                string? selectedPriority = _priorityComboBox?.SelectedItem as string;
                _editingItem.PriorityName = string.IsNullOrEmpty(selectedPriority) ? null : selectedPriority;

                // 更新标签
                _editingItem.Tags = new List<string>(_selectedTags);

                // 更新提醒设置
                _editingItem.IsReminderEnabled = _reminderToggle?.IsChecked == true;
                if (_editingItem.IsReminderEnabled &&
                    _reminderIntervalComboBox?.SelectedItem is ComboBoxItem intervalItem)
                {
                    string intervalStr = intervalItem.Tag?.ToString() ?? "None";
                    _editingItem.ReminderIntervalType = Enum.Parse<ReminderInterval>(intervalStr);
                    // 重置上次提醒时间，使其立即生效
                    _editingItem.LastReminderTime = null;
                }
                else
                {
                    _editingItem.ReminderIntervalType = ReminderInterval.None;
                    _editingItem.LastReminderTime = null;
                }

                _plugin.UpdateTodo(_editingItem);
            }
            else
            {
                // 创建新项
                TodoItem newItem = _plugin.AddTodo(_titleTextBox!.Text!, selectedColumn.Id, _descriptionTextBox?.Text ?? string.Empty);

                // 设置自定义字段
                foreach (KeyValuePair<string, Control> kvp in _fieldControls)
                {
                    string? value = GetControlValue(kvp.Value);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        newItem.CustomFields[kvp.Key] = value!; // 已检查非 null
                    }
                }

                // 设置优先级
                string? newPriority = _priorityComboBox?.SelectedItem as string;
                newItem.PriorityName = string.IsNullOrEmpty(newPriority) ? null : newPriority;

                // 设置标签
                newItem.Tags = new List<string>(_selectedTags);

                // 设置提醒
                newItem.IsReminderEnabled = _reminderToggle?.IsChecked == true;
                if (newItem.IsReminderEnabled &&
                    _reminderIntervalComboBox?.SelectedItem is ComboBoxItem newIntervalItem)
                {
                    string intervalStr = newIntervalItem.Tag?.ToString() ?? "None";
                    newItem.ReminderIntervalType = Enum.Parse<ReminderInterval>(intervalStr);
                    newItem.LastReminderTime = null; // 首次启用，立即提醒
                }
                else
                {
                    newItem.ReminderIntervalType = ReminderInterval.None;
                }

                _plugin.UpdateTodo(newItem);
            }
        }

        #region 优先级和标签管理

        private void OnPrioritySelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // 优先级选择变化，已在SaveTodo中处理
        }

        private void OnTagSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_tagComboBox == null || _plugin == null) return;

            string? selectedTag = _tagComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedTag)) return;

            AddTagToSelected(selectedTag);
        }

        private void OnTagComboBoxKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter && _tagComboBox != null)
            {
                string? newTagName = _tagComboBox.Text?.Trim();
                if (!string.IsNullOrEmpty(newTagName))
                {
                    AddTagToSelected(newTagName);
                    _tagComboBox.Text = string.Empty;
                }
                e.Handled = true;
            }
        }

        private void AddTagToSelected(string tagName)
        {
            if (_plugin == null) return;

            // 检查是否已存在该标签组，如果不存在则创建
            if (!_plugin.GetTags().Any(t => t.Name == tagName))
            {
                try
                {
                    _plugin.AddTag(tagName);
                    // 重新加载标签列表
                    if (_tagComboBox != null)
                    {
                        List<TagGroup> tags = _plugin.GetTags();
                        List<string> tagNames = tags.Select(t => t.Name).ToList();
                        _tagComboBox.ItemsSource = tagNames;
                    }
                }
                catch
                {
                    // 标签已存在或其他错误，继续处理
                }
            }

            // 添加到已选标签列表
            if (!_selectedTags.Contains(tagName))
            {
                _selectedTags.Add(tagName);
                UpdateSelectedTagsDisplay();
            }

            // 清空选择
            if (_tagComboBox != null)
            {
                _tagComboBox.SelectedIndex = -1;
                _tagComboBox.Text = string.Empty;
            }
        }

        private void UpdateSelectedTagsDisplay()
        {
            if (_selectedTagsPanel == null) return;

            _selectedTagsPanel.Children.Clear();

            foreach (string tagName in _selectedTags)
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
                removeButton.Click += (s, e) =>
                {
                    _selectedTags.Remove(tagName);
                    UpdateSelectedTagsDisplay();
                };
                tagPanel.Children.Add(removeButton);

                tagBorder.Child = tagPanel;
                _selectedTagsPanel.Children.Add(tagBorder);
            }
        }

        #endregion
    }
}
