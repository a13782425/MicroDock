using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Avalonia.Threading;
using MicroDock.Utils;
using System.Threading.Tasks;

namespace MicroDock.Views.Dialog;

/// <summary>
/// 验证密码对话框 - 用于验证当前密码（单次输入）
/// </summary>
public partial class VerifyPasswordDialog : UserControl, ICustomDialog<string>
{
    /// <summary>
    /// 获取输入的密码
    /// </summary>
    public string? Password => PasswordBox.Text;

    public VerifyPasswordDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 设置提示文本
    /// </summary>
    public void SetPrompt(string prompt)
    {
        PromptText.Text = prompt;
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

        return true;
    }

    /// <summary>
    /// 获取对话框结果
    /// </summary>
    /// <returns>输入的密码</returns>
    public string GetResult() => Password!;

    /// <summary>
    /// 显示错误信息
    /// </summary>
    public void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    /// <summary>
    /// 显示验证密码对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="prompt">提示文本</param>
    /// <returns>输入的密码，如果取消则返回 null</returns>
    public static async Task<string?> ShowAsync(string title = "验证密码", string prompt = "请输入当前密码：")
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialogContent = new VerifyPasswordDialog();
            dialogContent.SetPrompt(prompt);

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
