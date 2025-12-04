using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;
using TodoListPlugin.Models;

namespace TodoListPlugin.Views.Dialogs
{
    public partial class EditTagContent : UserControl
    {
        public EditTagContent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取标签名称
        /// </summary>
        public string TagName => NameTextBox.Text?.Trim() ?? string.Empty;

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
                return "#2196F3";
            }
        }

        /// <summary>
        /// 验证输入是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(TagName);

        /// <summary>
        /// 加载现有标签数据
        /// </summary>
        public void LoadTag(TagGroup tag)
        {
            NameTextBox.Text = tag.Name;
            SelectColor(tag.Color);
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
