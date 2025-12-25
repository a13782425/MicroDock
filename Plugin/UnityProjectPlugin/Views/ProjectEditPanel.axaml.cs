using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Linq;
using System.Threading.Tasks;
using UnityProjectPlugin.ViewModels;

namespace UnityProjectPlugin.Views
{
    /// <summary>
    /// 项目编辑面板
    /// </summary>
    public partial class ProjectEditPanel : UserControl
    {
        private GroupSelector? _groupSelector;
        private Button? _groupButton;
        private TextBlock? _groupButtonText;

        public ProjectEditPanel()
        {
            InitializeComponent();

            // 获取控件引用
            _groupButton = this.FindControl<Button>("GroupButton");
            _groupButtonText = this.FindControl<TextBlock>("GroupButtonText");

            // 绑定 Loaded 事件
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // 绑定分组按钮点击事件
            if (_groupButton != null)
            {
                _groupButton.Click += OnGroupButtonClick;
            }
        }

        private void OnGroupButtonClick(object? sender, RoutedEventArgs e)
        {
            // 延迟绑定 GroupSelector 事件
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(100);
                BindGroupSelectorEvents();
            });
        }

        /// <summary>
        /// 绑定 GroupSelector 的事件
        /// </summary>
        private void BindGroupSelectorEvents()
        {
            // 从视觉树中查找 GroupSelector
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                _groupSelector = topLevel.GetVisualDescendants()
                    .OfType<GroupSelector>()
                    .FirstOrDefault();
            }

            if (_groupSelector != null)
            {
                // 设置当前项目的分组
                if (DataContext is ProjectEditPanelViewModel viewModel && viewModel.Project != null)
                {
                    _groupSelector.SetSelectedGroupSilently(viewModel.Project.GroupName);
                }

                // 解绑旧事件（避免重复绑定）
                _groupSelector.GroupChanged -= OnGroupSelectorGroupChanged;
                _groupSelector.GroupChanged += OnGroupSelectorGroupChanged;
            }
        }

        /// <summary>
        /// GroupSelector 分组变化事件处理
        /// </summary>
        private void OnGroupSelectorGroupChanged(object? sender, string? newGroupName)
        {
            if (DataContext is ProjectEditPanelViewModel viewModel)
            {
                // 更新 ViewModel 中的 SelectedGroup
                viewModel.SelectedGroup = newGroupName;

                // 更新按钮显示文本
                if (_groupButtonText != null)
                {
                    _groupButtonText.Text = string.IsNullOrEmpty(newGroupName) ? "未分组" : newGroupName;
                }
            }
        }
    }
}
