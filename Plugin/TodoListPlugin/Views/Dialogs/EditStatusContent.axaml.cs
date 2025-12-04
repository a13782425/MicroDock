using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;
using TodoListPlugin.Models;

namespace TodoListPlugin.Views.Dialogs
{
    public partial class EditStatusContent : UserControl
    {
        public EditStatusContent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取状态名称
        /// </summary>
        public string StatusName => NameTextBox.Text?.Trim() ?? string.Empty;

        /// <summary>
        /// 获取选中的颜色
        /// </summary>
        public string SelectedColor
        {
            get
            {
                var colorRadios = this.GetVisualDescendants()
                    .OfType<RadioButton>()
                    .Where(r => r.GroupName == "Color");

                foreach (var radio in colorRadios)
                {
                    if (radio.IsChecked == true && radio.Tag is string color)
                    {
                        return color;
                    }
                }
                return "#FF6B6B";
            }
        }

        /// <summary>
        /// 是否为默认状态
        /// </summary>
        public bool IsDefault => IsDefaultCheckBox.IsChecked == true;

        /// <summary>
        /// 验证输入是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(StatusName);

        /// <summary>
        /// 加载现有状态数据
        /// </summary>
        public void LoadStatus(StatusColumn status)
        {
            NameTextBox.Text = status.Name;
            IsDefaultCheckBox.IsChecked = status.IsDefault;
            SelectColor(status.Color);
        }

        private void SelectColor(string color)
        {
            var colorRadios = this.GetVisualDescendants()
                .OfType<RadioButton>()
                .Where(r => r.GroupName == "Color");

            foreach (var radio in colorRadios)
            {
                if (radio.Tag is string c && c == color)
                {
                    radio.IsChecked = true;
                    break;
                }
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
