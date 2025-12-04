using Avalonia.Controls;
using System;
using System.Linq;
using TodoListPlugin.Models;

namespace TodoListPlugin.Views.Dialogs
{
    public partial class EditFieldContent : UserControl
    {
        public EditFieldContent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取字段名称
        /// </summary>
        public string FieldName => NameTextBox.Text?.Trim() ?? string.Empty;

        /// <summary>
        /// 获取字段类型
        /// </summary>
        public FieldType FieldType
        {
            get
            {
                var selectedItem = TypeComboBox.SelectedItem as ComboBoxItem;
                var tag = selectedItem?.Tag as string ?? "Text";
                return tag switch
                {
                    "Number" => FieldType.Number,
                    "Date" => FieldType.Date,
                    "Select" => FieldType.Select,
                    "MultiSelect" => FieldType.MultiSelect,
                    "Boolean" => FieldType.Boolean,
                    _ => FieldType.Text
                };
            }
        }

        /// <summary>
        /// 获取默认值
        /// </summary>
        public string? DefaultValue => string.IsNullOrWhiteSpace(DefaultValueTextBox.Text) ? null : DefaultValueTextBox.Text.Trim();

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool Required => RequiredCheckBox.IsChecked == true;

        /// <summary>
        /// 是否在卡片上显示
        /// </summary>
        public bool ShowOnCard => ShowOnCardCheckBox.IsChecked == true;

        /// <summary>
        /// 验证输入是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(FieldName);

        /// <summary>
        /// 加载现有字段数据
        /// </summary>
        public void LoadField(CustomFieldTemplate field)
        {
            NameTextBox.Text = field.Name;
            DefaultValueTextBox.Text = field.DefaultValue;
            RequiredCheckBox.IsChecked = field.Required;
            ShowOnCardCheckBox.IsChecked = field.ShowOnCard;
            SelectType(field.FieldType);
        }

        private void SelectType(FieldType type)
        {
            var tag = type switch
            {
                FieldType.Number => "Number",
                FieldType.Date => "Date",
                FieldType.Select => "Select",
                FieldType.MultiSelect => "MultiSelect",
                FieldType.Boolean => "Boolean",
                _ => "Text"
            };

            var items = TypeComboBox.Items.OfType<ComboBoxItem>();
            int index = 0;
            foreach (var item in items)
            {
                if (item.Tag is string t && t == tag)
                {
                    TypeComboBox.SelectedIndex = index;
                    break;
                }
                index++;
            }
        }

        /// <summary>
        /// 聚焦到名称输入框
        /// </summary>
        public void FocusName()
        {
            NameTextBox.Focus();
        }
    }
}
