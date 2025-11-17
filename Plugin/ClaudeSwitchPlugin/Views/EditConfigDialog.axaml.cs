using Avalonia.Controls;
using Avalonia.Interactivity;
using ClaudeSwitchPlugin.Services;
using ClaudeSwitchPlugin.ViewModels;

namespace ClaudeSwitchPlugin.Views
{
    public partial class EditConfigDialog : UserControl
    {
        public EditConfigDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 显示编辑对话框（新增模式）
        /// </summary>
        public static EditConfigDialog CreateAddDialog(ConfigurationService configService)
        {
            var dialog = new EditConfigDialog();
            dialog.DataContext = new EditConfigDialogViewModel(configService, dialog);
            return dialog;
        }

        /// <summary>
        /// 显示编辑对话框（编辑模式）
        /// </summary>
        public static EditConfigDialog CreateEditDialog(ConfigurationService configService, Models.AIConfiguration config)
        {
            var dialog = new EditConfigDialog();
            dialog.DataContext = new EditConfigDialogViewModel(configService, dialog, config);
            return dialog;
        }
    }
}