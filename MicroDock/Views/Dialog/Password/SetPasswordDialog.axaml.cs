using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Avalonia.Threading;
using MicroDock.Utils;
using System.Threading.Tasks;

namespace MicroDock.Views.Dialog;

/// <summary>
/// 设置密码对话框 - 用于首次设置密码（两次输入确认）
/// </summary>
public partial class SetPasswordDialog : UserControl, ICustomDialog<string>
{
    /// <summary>
    /// 获取输入的密码
    /// </summary>
    public string? Password => PasswordBox.Text;

    /// <summary>
    /// 获取确认密码
    /// </summary>
    public string? ConfirmPassword => ConfirmPasswordBox.Text;

    public SetPasswordDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 验证输入
    /// </summary>
    /// <returns>验证是否通过</returns>
    public bool Validate()
    {
        ErrorText.IsVisible = false;

        if (string.IsNullOrEmpty(Password))
        {
            ShowError("请输入密码");
            return false;
        }

        if (string.IsNullOrEmpty(ConfirmPassword))
        {
            ShowError("请确认密码");
            return false;
        }

        if (Password != ConfirmPassword)
        {
            ShowError("两次输入的密码不一致");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取对话框结果
    /// </summary>
    /// <returns>设置的密码</returns>
    public string GetResult() => Password!;

    /// <summary>
    /// 显示错误信息
    /// </summary>
    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    /// <summary>
    /// 显示设置密码对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <returns>设置的密码，如果取消则返回 null</returns>
    public static async Task<string?> ShowAsync(string title = "设置密码")
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialogContent = new SetPasswordDialog();

            var dialog = new ContentDialog
            {
                Title = title,
                Content = dialogContent,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            // 自定义主按钮点击验证
            dialog.PrimaryButtonClick += (sender, args) =>
            {
                if (!dialogContent.Validate())
                {
                    args.Cancel = true; // 阻止对话框关闭
                }
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                return dialogContent.Password;
            }

            return null;
        });
    }
}
