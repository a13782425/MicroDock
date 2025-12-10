using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;

namespace TodoListPlugin.Views.Dialogs
{
    public partial class AddProjectContent : UserControl
    {
        public AddProjectContent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取项目名称
        /// </summary>
        public string ProjectName => NameTextBox.Text?.Trim() ?? string.Empty;

        /// <summary>
        /// 获取项目描述
        /// </summary>
        public string ProjectDescription => DescriptionTextBox.Text?.Trim() ?? string.Empty;

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
        public bool IsValid => !string.IsNullOrWhiteSpace(ProjectName);

        /// <summary>
        /// 设置选中的颜色
        /// </summary>
        public void SetColor(string color)
        {
            foreach (var radio in this.GetVisualDescendants().OfType<RadioButton>())
            {
                if (radio.Tag is string tagColor && tagColor == color)
                {
                    radio.IsChecked = true;
                    break;
                }
            }
        }

        /// <summary>
        /// 设置项目数据（用于编辑）
        /// </summary>
        public void SetProject(string name, string color, string? description)
        {
            NameTextBox.Text = name;
            SetColor(color);
            DescriptionTextBox.Text = description ?? string.Empty;
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
