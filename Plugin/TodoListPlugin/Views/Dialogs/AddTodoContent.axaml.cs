using Avalonia.Controls;
using System.Collections.Generic;
using System.Linq;
using TodoListPlugin.Models;

namespace TodoListPlugin.Views.Dialogs
{
    public partial class AddTodoContent : UserControl
    {
        private readonly Dictionary<string, string> _requiredFieldValues = new();
        private IReadOnlyList<CustomFieldTemplate>? _requiredFields;

        public AddTodoContent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取待办标题
        /// </summary>
        public string TodoTitle => TitleTextBox.Text?.Trim() ?? string.Empty;

        /// <summary>
        /// 获取选中的状态列 ID
        /// </summary>
        public string? SelectedStatusId => (StatusComboBox.SelectedItem as StatusColumn)?.Id;

        /// <summary>
        /// 获取必填字段值
        /// </summary>
        public Dictionary<string, string> RequiredFieldValues => new(_requiredFieldValues);

        /// <summary>
        /// 验证输入是否有效
        /// </summary>
        public bool IsValid
        {
            get
            {
                // 标题不能为空
                if (string.IsNullOrWhiteSpace(TodoTitle))
                    return false;

                // 验证所有必填字段
                if (_requiredFields != null)
                {
                    foreach (var field in _requiredFields)
                    {
                        if (!_requiredFieldValues.TryGetValue(field.Id, out var value) || string.IsNullOrWhiteSpace(value))
                            return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 设置状态列列表
        /// </summary>
        public void SetStatusColumns(IEnumerable<StatusColumn> columns)
        {
            StatusComboBox.ItemsSource = columns;
            StatusComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
        }

        /// <summary>
        /// 设置必填字段模板
        /// </summary>
        public void SetRequiredFields(IEnumerable<CustomFieldTemplate> fields)
        {
            var requiredList = fields.Where(f => f.Required).ToList();
            _requiredFields = requiredList;

            if (requiredList.Count > 0)
            {
                RequiredFieldsItemsControl.ItemsSource = requiredList;
                RequiredFieldsPanel.IsVisible = true;

                // 初始化默认值
                foreach (var field in requiredList)
                {
                    _requiredFieldValues[field.Id] = field.DefaultValue ?? string.Empty;
                }
            }
            else
            {
                RequiredFieldsPanel.IsVisible = false;
            }
        }

        /// <summary>
        /// 设置默认选中的状态列
        /// </summary>
        public void SetSelectedStatus(StatusColumn? column)
        {
            StatusComboBox.SelectedItem = column;
        }

        /// <summary>
        /// 设置默认选中的状态列（通过ID）
        /// </summary>
        public void SetSelectedStatus(string? statusColumnId)
        {
            if (string.IsNullOrEmpty(statusColumnId)) return;
            
            if (StatusComboBox.ItemsSource is IEnumerable<StatusColumn> columns)
            {
                foreach (var column in columns)
                {
                    if (column.Id == statusColumnId)
                    {
                        StatusComboBox.SelectedItem = column;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 必填字段文本变更
        /// </summary>
        private void OnRequiredFieldTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string fieldId)
            {
                _requiredFieldValues[fieldId] = textBox.Text ?? string.Empty;
            }
        }

        /// <summary>
        /// 聚焦到标题输入框
        /// </summary>
        public void FocusTitle()
        {
            TitleTextBox.Focus();
        }
    }
}
