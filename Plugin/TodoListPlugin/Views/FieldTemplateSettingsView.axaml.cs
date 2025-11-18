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
        }

        private void LoadFields()
        {
            if (_fieldsListControl == null) return;

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

                _plugin.AddFieldTemplate(name, fieldType, required, defaultValue, filterable);

                // 清空输入
                _newFieldNameTextBox.Text = string.Empty;
                _fieldTypeComboBox.SelectedIndex = -1;
                if (_defaultValueTextBox != null) _defaultValueTextBox.Text = string.Empty;
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
                    DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Primary
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

                        _plugin.UpdateFieldTemplate(field.Id, newName, fieldType, required, defaultValue, filterable);
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

