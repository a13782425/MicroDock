using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using System;
using System.Threading.Tasks;

namespace TodoListPlugin.Helpers
{
    /// <summary>
    /// 弹窗帮助类 - 封装 FluentAvalonia ContentDialog
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// 显示内容弹窗
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="title">弹窗标题</param>
        /// <param name="content">弹窗内容（UserControl）</param>
        /// <param name="primaryButtonText">主按钮文本（确认）</param>
        /// <param name="closeButtonText">关闭按钮文本（取消）</param>
        /// <param name="getResult">获取结果的委托，在点击主按钮时调用</param>
        /// <returns>结果，如果取消则返回 default</returns>
        public static async Task<TResult?> ShowContentDialogAsync<TResult>(
            string title,
            Control content,
            string primaryButtonText = "确定",
            string closeButtonText = "取消",
            Func<TResult?>? getResult = null)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && getResult != null)
            {
                return getResult();
            }

            return default;
        }

        /// <summary>
        /// 显示内容弹窗 - 简化版本，只返回 ContentDialogResult
        /// </summary>
        public static async Task<ContentDialogResult> ShowContentDialogAsync(
            string title,
            Control content,
            string primaryButtonText = "确定",
            string closeButtonText = "取消")
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                DefaultButton = ContentDialogButton.Primary
            };

            return await dialog.ShowAsync();
        }

        /// <summary>
        /// 显示内容弹窗（带验证）- 简化版本，只返回 ContentDialogResult
        /// </summary>
        public static async Task<ContentDialogResult> ShowContentDialogWithValidationAsync(
            string title,
            Control content,
            Func<bool> validate)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            // 阻止关闭直到验证通过
            dialog.PrimaryButtonClick += (s, e) =>
            {
                if (!validate())
                {
                    e.Cancel = true;
                }
            };

            return await dialog.ShowAsync();
        }

        /// <summary>
        /// 显示内容弹窗（带验证）
        /// </summary>
        public static async Task<TResult?> ShowContentDialogWithValidationAsync<TResult>(
            string title,
            Control content,
            string primaryButtonText,
            string closeButtonText,
            Func<TResult?> getResult,
            Func<bool> validate)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                DefaultButton = ContentDialogButton.Primary
            };

            // 阻止关闭直到验证通过
            dialog.PrimaryButtonClick += (s, e) =>
            {
                if (!validate())
                {
                    e.Cancel = true;
                }
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                return getResult();
            }

            return default;
        }

        /// <summary>
        /// 显示确认弹窗
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="confirmText">确认按钮文本</param>
        /// <param name="cancelText">取消按钮文本</param>
        /// <returns>是否确认</returns>
        public static async Task<bool> ShowConfirmAsync(
            string title,
            string message,
            string confirmText = "确定",
            string cancelText = "取消")
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message },
                PrimaryButtonText = confirmText,
                CloseButtonText = cancelText,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示危险操作确认弹窗（删除等）
        /// </summary>
        public static async Task<bool> ShowDeleteConfirmAsync(string itemName)
        {
            var dialog = new ContentDialog
            {
                Title = "确认删除",
                Content = new TextBlock { Text = $"确定要删除 \"{itemName}\" 吗？此操作不可撤销。" },
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            // 可以设置主按钮为危险样式
            // dialog.PrimaryButtonStyle = ...

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示消息弹窗
        /// </summary>
        public static async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message },
                CloseButtonText = "确定",
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// 显示带三个按钮的弹窗
        /// </summary>
        public static async Task<ContentDialogResult> ShowThreeButtonDialogAsync(
            string title,
            Control content,
            string primaryButtonText,
            string secondaryButtonText,
            string closeButtonText)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText,
                CloseButtonText = closeButtonText,
                DefaultButton = ContentDialogButton.Primary
            };

            return await dialog.ShowAsync();
        }

        /// <summary>
        /// 显示自定义字段编辑对话框
        /// </summary>
        /// <param name="key">字段名（编辑时传入）</param>
        /// <param name="value">字段值（编辑时传入）</param>
        /// <returns>结果元组，如果取消返回 null</returns>
        public static async Task<(string Key, string Value)?> ShowCustomFieldDialogAsync(string? key, string? value)
        {
            var keyTextBox = new TextBox
            {
                Watermark = "字段名称",
                Text = key ?? "",
                Margin = new Avalonia.Thickness(0, 0, 0, 8)
            };

            var valueTextBox = new TextBox
            {
                Watermark = "字段值",
                Text = value ?? "",
                AcceptsReturn = true,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                MinHeight = 60
            };

            var content = new StackPanel
            {
                Spacing = 8,
                Children = { keyTextBox, valueTextBox }
            };

            var dialog = new ContentDialog
            {
                Title = string.IsNullOrEmpty(key) ? "添加自定义字段" : "编辑自定义字段",
                Content = content,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            // 验证字段名不能为空
            dialog.PrimaryButtonClick += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(keyTextBox.Text))
                {
                    e.Cancel = true;
                }
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                return (keyTextBox.Text?.Trim() ?? "", valueTextBox.Text?.Trim() ?? "");
            }

            return null;
        }
    }
}
