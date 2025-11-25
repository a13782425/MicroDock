using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using MicroDock.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MicroDock.Utils;

/// <summary>
/// 通用工具类，提供通知、对话框、剪切板等常用功能
/// </summary>
internal static class UniversalUtils
{
    #region 通知方法

    /// <summary>
    /// 显示应用内通知 (Toast)
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    /// <param name="type">通知类型</param>
    /// <param name="seconds">显示时长（秒）</param>
    public static void ShowNotification(
        string title,
        string message,
        AppNotificationType type = AppNotificationType.Information,
        int seconds = 3)
    {
        try
        {
            if (Program.WindowNotificationManager != null)
            {
                // 确保在UI线程上执行
                Dispatcher.UIThread.Post(() =>
                {
                    Program.WindowNotificationManager.Show(new Notification(
                        title,
                        message,
                        type,
                        TimeSpan.FromSeconds(seconds)
                    ));
                });
            }
            else
            {
                LogWarning("WindowNotificationManager 未初始化，无法显示应用内通知");
            }
        }
        catch (Exception ex)
        {
            LogError($"显示应用内通知失败: {title}", DEFAULT_LOG_TAG, ex);
        }
    }

    /// <summary>
    /// 显示系统托盘通知
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    /// <param name="buttons">可选的按钮列表（Key: 按钮ID, Value: 按钮文本）</param>
    /// <param name="seconds">显示时长（秒）</param>
    public static void ShowSystemNotification(
        string title,
        string message,
        Dictionary<string, string>? buttons = null,
        int seconds = 5)
    {
        try
        {
            var notification = new DesktopNotifications.Notification
            {
                Title = title,
                Body = message
            };

            // 添加按钮
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    notification.Buttons.Add((button.Key, button.Value));
                }
            }

            // 确保在UI线程上执行
            Dispatcher.UIThread.Post(() =>
            {
                Program.NotificationManager.ShowNotification(
                    notification,
                    DateTimeOffset.Now + TimeSpan.FromSeconds(seconds)
                );
            });
        }
        catch (Exception ex)
        {
            LogError($"显示系统托盘通知失败: {title}", DEFAULT_LOG_TAG, ex);
        }
    }

    #endregion

    #region ContentDialog 对话框方法

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="message">对话框内容</param>
    /// <param name="warningText">可选的警告文本（显示为橙红色）</param>
    /// <param name="primaryText">确认按钮文本</param>
    /// <param name="closeText">取消按钮文本</param>
    /// <returns>用户是否点击了确认按钮</returns>
    public static async Task<bool> ShowConfirmDialogAsync(
        string title,
        string message,
        string? warningText = null,
        string primaryText = "确认",
        string closeText = "取消")
    {
        try
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                object content;

                if (string.IsNullOrEmpty(warningText))
                {
                    content = new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    };
                }
                else
                {
                    content = new StackPanel
                    {
                        Spacing = 10,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = message,
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap
                            },
                            new TextBlock
                            {
                                Text = warningText,
                                Foreground = Avalonia.Media.Brushes.OrangeRed,
                                FontSize = 12,
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap
                            }
                        }
                    };
                }

                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    PrimaryButtonText = primaryText,
                    CloseButtonText = closeText,
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await dialog.ShowAsync();
                return result == ContentDialogResult.Primary;
            });
        }
        catch (Exception ex)
        {
            LogError($"显示确认对话框失败: {title}", DEFAULT_LOG_TAG, ex);
            return false;
        }
    }

    /// <summary>
    /// 显示错误提示对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="message">错误消息</param>
    public static async Task ShowErrorDialogAsync(string title, string message)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Foreground = Avalonia.Media.Brushes.OrangeRed
                    },
                    CloseButtonText = "确定",
                    DefaultButton = ContentDialogButton.Close
                };

                await dialog.ShowAsync();
            });
        }
        catch (Exception ex)
        {
            LogError($"显示错误对话框失败: {title}", DEFAULT_LOG_TAG, ex);
        }
    }

    /// <summary>
    /// 显示输入对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="placeholder">输入框占位符</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>用户输入的文本，如果取消则返回 null</returns>
    public static async Task<string?> ShowInputDialogAsync(
        string title,
        string placeholder = "",
        string? defaultValue = null)
    {
        try
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var textBox = new TextBox
                {
                    Text = defaultValue ?? string.Empty,
                    Watermark = placeholder,
                    MinWidth = 300
                };

                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = textBox,
                    PrimaryButtonText = "确定",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    return textBox.Text;
                }

                return null;
            });
        }
        catch (Exception ex)
        {
            LogError($"显示输入对话框失败: {title}", DEFAULT_LOG_TAG, ex);
            return null;
        }
    }

    /// <summary>
    /// 显示自定义内容对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="content">自定义内容（Control 或其他可显示对象）</param>
    /// <param name="primaryText">主按钮文本（为空则不显示主按钮）</param>
    /// <param name="closeText">关闭按钮文本</param>
    /// <returns>对话框结果</returns>
    public static async Task<ContentDialogResult> ShowCustomDialogAsync(
        string title,
        object content,
        string? primaryText = null,
        string closeText = "关闭")
    {
        try
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    CloseButtonText = closeText,
                    DefaultButton = string.IsNullOrEmpty(primaryText)
                        ? ContentDialogButton.Close
                        : ContentDialogButton.Primary
                };

                if (!string.IsNullOrEmpty(primaryText))
                {
                    dialog.PrimaryButtonText = primaryText;
                }

                return await dialog.ShowAsync();
            });
        }
        catch (Exception ex)
        {
            LogError($"显示自定义对话框失败: {title}", DEFAULT_LOG_TAG, ex);
            return ContentDialogResult.None;
        }
    }

    #endregion

    #region 剪切板方法

    /// <summary>
    /// 复制文本到剪切板
    /// </summary>
    /// <param name="text">要复制的文本</param>
    /// <param name="typeName">类型名称（用于通知显示，为空则不显示通知）</param>
    /// <param name="showNotification">是否显示通知</param>
    public static async Task CopyToClipboardAsync(
        string text,
        string? typeName = null,
        bool showNotification = true)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // 获取主窗口的剪切板服务
                if (Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow != null)
                {
                    var clipboard = desktop.MainWindow.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(text);

                        if (showNotification)
                        {
                            var title = string.IsNullOrEmpty(typeName)
                                ? "已复制"
                                : $"已复制{typeName}";
                            ShowNotification(title, text, NotificationType.Success, 2);
                        }

                        LogService.LogInformation($"已复制到剪切板: {text}", LogService.DEFAULT_LOG_TAG);
                    }
                    else
                    {
                        LogService.LogWarning("剪切板服务不可用", LogService.DEFAULT_LOG_TAG);
                        if (showNotification)
                        {
                            ShowNotification("复制失败", "剪切板服务不可用", NotificationType.Error, 2);
                        }
                    }
                }
                else
                {
                    LogService.LogWarning("无法获取主窗口，剪切板操作失败", LogService.DEFAULT_LOG_TAG);
                    if (showNotification)
                    {
                        ShowNotification("复制失败", "无法获取主窗口", NotificationType.Error, 2);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            LogService.LogError("复制到剪切板失败", LogService.DEFAULT_LOG_TAG, ex);
            if (showNotification)
            {
                ShowNotification("复制失败", ex.Message, NotificationType.Error, 2);
            }
        }
    }

    #endregion

    public static void RunUIThread(Action action, DispatcherPriority priority = default(DispatcherPriority))
    {
        Dispatcher.UIThread.Post(action, priority);
    }
}
