using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Avalonia.Threading;
using MicroDock.Utils;
using System.Threading.Tasks;

namespace MicroDock.Views.Dialog;

/// <summary>
/// 修改密码对话框结果
/// </summary>
public class ChangePasswordResult
{
    /// <summary>
    /// 当前密码
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 修改密码对话框 - 用于修改密码（旧密码+两次新密码）
/// </summary>
public partial class ChangePasswordDialog : UserControl, ICustomDialog<ChangePasswordResult>
{
    /// <summary>
    /// 获取当前密码
    /// </summary>
    public string? CurrentPassword => CurrentPasswordBox.Text;

    /// <summary>
    /// 获取新密码
    /// </summary>
    public string? NewPassword => NewPasswordBox.Text;

    /// <summary>
    /// 获取确认密码
    /// </summary>
    public string? ConfirmPassword => ConfirmPasswordBox.Text;

    public ChangePasswordDialog()
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

        if (string.IsNullOrEmpty(CurrentPassword))
        {
            ShowError("请输入当前密码");
            return false;
        }

        //if (string.IsNullOrEmpty(NewPassword))
        //{
        //    ShowError("请输入新密码");
        //    return false;
        //}

        //if (string.IsNullOrEmpty(ConfirmPassword))
        //{
        //    ShowError("请确认新密码");
        //    return false;
        //}

        if (NewPassword != ConfirmPassword)
        {
            ShowError("两次输入的新密码不一致");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取对话框结果
    /// </summary>
    /// <returns>修改密码结果</returns>
    public ChangePasswordResult GetResult() => new ChangePasswordResult
    {
        CurrentPassword = CurrentPassword!,
        NewPassword = NewPassword!
    };

    /// <summary>
    /// 显示错误信息
    /// </summary>
    public void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    ///// <summary>
    ///// 显示修改密码对话框
    ///// </summary>
    ///// <param name="title">对话框标题</param>
    ///// <returns>成功时返回 (当前密码, 新密码)，取消或失败返回 null</returns>
    //public static async Task<(string currentPassword, string newPassword)?> ShowAsync(string title = "修改密码")
    //{
    //    return await Dispatcher.UIThread.InvokeAsync(async () =>
    //    {
    //        var dialogContent = new ChangePasswordDialog();

    //        var dialog = new ContentDialog
    //        {
    //            Title = title,
    //            Content = dialogContent,
    //            PrimaryButtonText = "确定",
    //            CloseButtonText = "取消",
    //            DefaultButton = ContentDialogButton.Primary
    //        };

    //        // 自定义主按钮点击验证
    //        dialog.PrimaryButtonClick += (sender, args) =>
    //        {
    //            if (!dialogContent.Validate())
    //            {
    //                args.Cancel = true; // 阻止对话框关闭
    //            }
    //        };

    //        var result = await dialog.ShowAsync();

    //        if (result == ContentDialogResult.Primary)
    //        {
    //            return (dialogContent.CurrentPassword!, dialogContent.NewPassword!);
    //        }

    //        return (false, "");
    //    });
    //}
}