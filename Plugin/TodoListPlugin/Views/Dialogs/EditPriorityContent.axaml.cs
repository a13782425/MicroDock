using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;
using TodoListPlugin.Models;

namespace TodoListPlugin.Views.Dialogs
{
    public partial class EditPriorityContent : UserControl
    {
        public EditPriorityContent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取优先级名称
        /// </summary>
        public string PriorityName => NameTextBox.Text?.Trim() ?? string.Empty;

        /// <summary>
        /// 获取优先级级别
        /// </summary>
        public int Level => (int)(LevelNumeric.Value ?? 0);

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
                return "#FF4444";
            }
        }

        /// <summary>
        /// 验证输入是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(PriorityName);

        /// <summary>
        /// 加载现有优先级数据
        /// </summary>
        public void LoadPriority(PriorityGroup priority)
        {
            NameTextBox.Text = priority.Name;
            LevelNumeric.Value = priority.Level;
            SelectColor(priority.Color);
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
