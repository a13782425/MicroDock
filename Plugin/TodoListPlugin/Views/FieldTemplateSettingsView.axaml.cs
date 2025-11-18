using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using TodoListPlugin.Models;

namespace TodoListPlugin.Views
{
    /// <summary>
    /// 字段模板管理设置视图
    /// </summary>
    public partial class FieldTemplateSettingsView : UserControl
    {
        private readonly TodoListPlugin _plugin;

        private TextBox? _newFieldNameTextBox;
        private ComboBox? _fieldTypeComboBox;
        private TextBox? _defaultValueTextBox;
        private TextBox? _optionsTextBox;
        private StackPanel? _optionsPanel;
        private CheckBox? _requiredCheckBox;
        private CheckBox? _filterableCheckBox;
        private Button? _addFieldButton;
        private ItemsControl? _fieldsListControl;

        public FieldTemplateSettingsView(TodoListPlugin plugin)
        {
            _plugin = plugin;

            InitializeComponent();
            InitializeControls();
            AttachEventHandlers();
            LoadFields();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _newFieldNameTextBox = this.FindControl<TextBox>("NewFieldNameTextBox");
            _fieldTypeComboBox = this.FindControl<ComboBox>("FieldTypeComboBox");
            _defaultValueTextBox = this.FindControl<TextBox>("DefaultValueTextBox");
            _optionsTextBox = this.FindControl<TextBox>("OptionsTextBox");
            _optionsPanel = this.FindControl<StackPanel>("OptionsPanel");
            _requiredCheckBox = this.FindControl<CheckBox>("RequiredCheckBox");
            _filterableCheckBox = this.FindControl<CheckBox>("FilterableCheckBox");
            _addFieldButton = this.FindControl<Button>("AddFieldButton");
            _fieldsListControl = this.FindControl<ItemsControl>("FieldsListControl");
        }

        private void AttachEventHandlers()
        {
            if (_addFieldButton != null)
            {
                _addFieldButton.Click += OnAddFieldClick;
            }
            
            // 类型改变时显示/隐藏选项列表
            if (_fieldTypeComboBox != null)
            {
                _fieldTypeComboBox.SelectionChanged += (s, e) =>
                {
                    if (_optionsPanel != null && _fieldTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                    {
                        string selectedType = selectedItem.Tag?.ToString() ?? "Text";
                        _optionsPanel.IsVisible = selectedType == "Select";
                    }
                };
            }
        }

        private void LoadFields()
        {
            if (_fieldsListControl == null) return;

            // 获取所有字段（包括默认字段），因为设置页面需要显示所有字段以便管理
            List<CustomFieldTemplate> fields = _plugin.GetFieldTemplates();
            // 按Order排序
            fields = fields.OrderBy(f => f.Order).ToList();
            _fieldsListControl.ItemsSource = null;
            _fieldsListControl.ItemsSource = fields;
        }

        private async void OnAddFieldClick(object? sender, RoutedEventArgs e)
        {
            if (_newFieldNameTextBox == null || _fieldTypeComboBox == null) return;

            string name = _newFieldNameTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                await ShowErrorDialogAsync("输入错误", "字段名称不能为空。");
                return;
            }

            if (_fieldTypeComboBox.SelectedItem is not ComboBoxItem selectedItem || selectedItem.Tag == null)
            {
                await ShowErrorDialogAsync("输入错误", "请选择字段类型。");
                return;
            }

            try
            {
                string typeStr = selectedItem.Tag.ToString() ?? "Text";
                FieldType fieldType = Enum.Parse<FieldType>(typeStr);

                bool required = _requiredCheckBox?.IsChecked == true;
                bool filterable = _filterableCheckBox?.IsChecked == true;
                string? defaultValue = _defaultValueTextBox?.Text;

                // 处理Select类型的Options
                List<string> options = new List<string>();
                if (fieldType == FieldType.Select && _optionsTextBox != null && !string.IsNullOrWhiteSpace(_optionsTextBox.Text))
                {
                    options = _optionsTextBox.Text.Split('\n')
                        .Select(o => o.Trim())
                        .Where(o => !string.IsNullOrEmpty(o))
                        .ToList();
                }

                _plugin.AddFieldTemplate(name, fieldType, required, defaultValue, filterable, options);

                // 清空输入
                _newFieldNameTextBox.Text = string.Empty;
                _fieldTypeComboBox.SelectedIndex = -1;
                if (_defaultValueTextBox != null) _defaultValueTextBox.Text = string.Empty;
                if (_optionsTextBox != null) _optionsTextBox.Text = string.Empty;
                if (_optionsPanel != null) _optionsPanel.IsVisible = false;
                if (_requiredCheckBox != null) _requiredCheckBox.IsChecked = false;
                if (_filterableCheckBox != null) _filterableCheckBox.IsChecked = true;

                // 刷新列表
                LoadFields();
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

        private async void OnEditFieldClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CustomFieldTemplate field)
            {
                // 创建编辑对话框内容
                StackPanel panel = new StackPanel { Spacing = 16 };

                // 名称输入
                StackPanel namePanel = new StackPanel { Spacing = 6 };
                namePanel.Children.Add(new TextBlock { Text = "字段名称", FontWeight = Avalonia.Media.FontWeight.Medium });
                TextBox nameTextBox = new TextBox { Text = field.Name, MaxLength = 30 };
                namePanel.Children.Add(nameTextBox);
                panel.Children.Add(namePanel);

                // 类型选择
                StackPanel typePanel = new StackPanel { Spacing = 6 };
                typePanel.Children.Add(new TextBlock { Text = "字段类型", FontWeight = Avalonia.Media.FontWeight.Medium });
                ComboBox typeCombo = new ComboBox
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
                };
                typeCombo.Items.Add(new ComboBoxItem { Content = "文本", Tag = "Text" });
                typeCombo.Items.Add(new ComboBoxItem { Content = "数字", Tag = "Number" });
                typeCombo.Items.Add(new ComboBoxItem { Content = "日期", Tag = "Date" });
                typeCombo.Items.Add(new ComboBoxItem { Content = "布尔", Tag = "Bool" });
                typeCombo.Items.Add(new ComboBoxItem { Content = "单选项", Tag = "Select" });
                
                // 设置当前类型
                string currentType = field.FieldType.ToString();
                foreach (object? item in typeCombo.Items)
                {
                    if (item is ComboBoxItem cbItem && cbItem.Tag?.ToString() == currentType)
                    {
                        typeCombo.SelectedItem = cbItem;
                        break;
                    }
                }
                
                typePanel.Children.Add(typeCombo);
                panel.Children.Add(typePanel);

                // 默认值
                StackPanel defaultPanel = new StackPanel { Spacing = 6 };
                defaultPanel.Children.Add(new TextBlock { Text = "默认值（可选）", FontWeight = Avalonia.Media.FontWeight.Medium });
                TextBox defaultTextBox = new TextBox { Text = field.DefaultValue };
                defaultPanel.Children.Add(defaultTextBox);
                panel.Children.Add(defaultPanel);

                // Select类型的选项列表
                StackPanel optionsPanel = new StackPanel { Spacing = 6 };
                optionsPanel.Children.Add(new TextBlock { Text = "选项列表（每行一个）", FontWeight = Avalonia.Media.FontWeight.Medium });
                TextBox optionsTextBox = new TextBox 
                { 
                    Text = field.FieldType == FieldType.Select ? string.Join("\n", field.Options ?? new List<string>()) : string.Empty,
                    AcceptsReturn = true,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    MinHeight = 100,
                    MaxHeight = 200
                };
                optionsPanel.Children.Add(optionsTextBox);
                optionsPanel.IsVisible = field.FieldType == FieldType.Select;
                panel.Children.Add(optionsPanel);

                // 类型改变时显示/隐藏选项列表
                typeCombo.SelectionChanged += (s, e) =>
                {
                    if (typeCombo.SelectedItem is ComboBoxItem selectedTypeItem)
                    {
                        string selectedType = selectedTypeItem.Tag?.ToString() ?? "Text";
                        optionsPanel.IsVisible = selectedType == "Select";
                    }
                };

                // 必填和可筛选
                CheckBox requiredCheck = new CheckBox { Content = "必填字段", IsChecked = field.Required };
                panel.Children.Add(requiredCheck);
                
                CheckBox filterableCheck = new CheckBox { Content = "支持筛选", IsChecked = field.IsFilterable };
                panel.Children.Add(filterableCheck);

                var dialog = new FluentAvalonia.UI.Controls.ContentDialog
                {
                    Title = "编辑字段",
                    Content = panel,
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消",
                    DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Primary,
                    Width = 500,
                    MaxWidth = 500
                };

                dialog.PrimaryButtonClick += (s, args) =>
                {
                    string newName = nameTextBox.Text?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(newName) || typeCombo.SelectedItem is not ComboBoxItem selectedItem)
                    {
                        args.Cancel = true;
                        return;
                    }

                    try
                    {
                        string typeStr = selectedItem.Tag?.ToString() ?? "Text";
                        FieldType fieldType = Enum.Parse<FieldType>(typeStr);
                        bool required = requiredCheck.IsChecked == true;
                        bool filterable = filterableCheck.IsChecked == true;
                        string? defaultValue = defaultTextBox.Text;

                        // 处理Select类型的Options
                        List<string> options = new List<string>();
                        if (fieldType == FieldType.Select && !string.IsNullOrWhiteSpace(optionsTextBox.Text))
                        {
                            options = optionsTextBox.Text.Split('\n')
                                .Select(o => o.Trim())
                                .Where(o => !string.IsNullOrEmpty(o))
                                .ToList();
                        }

                        _plugin.UpdateFieldTemplate(field.Id, newName, fieldType, required, defaultValue, filterable, options);
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
                    LoadFields();
                }
            }
        }

        private async void OnDeleteFieldClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CustomFieldTemplate field)
            {
                var confirmDialog = new FluentAvalonia.UI.Controls.ContentDialog
                {
                    Title = "确认删除",
                    Content = $"确定要删除字段 \"{field.Name}\" 吗？所有待办事项中的该字段数据将被删除。",
                    PrimaryButtonText = "删除",
                    CloseButtonText = "取消",
                    DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Close
                };

                var result = await confirmDialog.ShowAsync();
                if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
                {
                    try
                    {
                        _plugin.DeleteFieldTemplate(field.Id);
                        LoadFields();
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

