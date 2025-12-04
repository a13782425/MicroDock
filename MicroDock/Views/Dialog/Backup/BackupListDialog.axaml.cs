using Avalonia.Controls;
using Avalonia.Interactivity;
using MicroDock.Model;
using MicroDock.Utils;
using System.Collections.ObjectModel;
using System;

namespace MicroDock.Views.Dialog;

/// <summary>
/// 备份列表项
/// </summary>
public class BackupListItem
{
    public int Id { get; set; }
    public string BackupType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? PluginName { get; set; }

    /// <summary>
    /// 格式化的创建时间
    /// </summary>
    public string FormattedCreatedAt
    {
        get
        {
            if (DateTime.TryParse(CreatedAt, out var dt))
            {
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            }
            return CreatedAt;
        }
    }

    /// <summary>
    /// 格式化的文件大小
    /// </summary>
    public string FormattedFileSize
    {
        get
        {
            if (FileSize < 1024)
                return $"{FileSize} B";
            if (FileSize < 1024 * 1024)
                return $"{FileSize / 1024.0:F1} KB";
            return $"{FileSize / (1024.0 * 1024.0):F2} MB";
        }
    }

    /// <summary>
    /// 显示名称（用于列表显示）
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Description) ? $"备份 #{Id}" : Description;
}

/// <summary>
/// 备份列表对话框操作类型
/// </summary>
public enum BackupDialogAction
{
    None,
    Restore,
    Delete
}

/// <summary>
/// 备份列表对话框结果
/// </summary>
public class BackupDialogResult
{
    public BackupDialogAction Action { get; set; }
    public BackupListItem? SelectedItem { get; set; }
}

/// <summary>
/// 备份列表对话框 - 用于选择备份进行恢复或删除
/// </summary>
public partial class BackupListDialog : UserControl, ICustomDialog<BackupDialogResult>
{
    private bool _deleteRequested = false;

    /// <summary>
    /// 获取选中的备份
    /// </summary>
    public BackupListItem? SelectedBackup => BackupListBox.SelectedItem as BackupListItem;

    /// <summary>
    /// 是否请求删除
    /// </summary>
    public bool DeleteRequested => _deleteRequested;

    public BackupListDialog()
    {
        InitializeComponent();
        DeleteButton.Click += OnDeleteButtonClick;

        // 从 GlobalData 读取数据
        if (GlobalData.TempBackupList != null)
        {
            SetBackupList(GlobalData.TempBackupList);
        }
    }

    private void OnDeleteButtonClick(object? sender, RoutedEventArgs e)
    {
        _deleteRequested = true;
    }

    /// <summary>
    /// 设置备份列表
    /// </summary>
    public void SetBackupList(ObservableCollection<BackupListItem> backups)
    {
        BackupListBox.ItemsSource = backups;
        TitleText.Text = $"找到 {backups.Count} 个备份，选择一个进行操作：";
    }

    /// <summary>
    /// 重置删除请求状态
    /// </summary>
    public void ResetDeleteRequest()
    {
        _deleteRequested = false;
    }

    /// <summary>
    /// 验证选择
    /// </summary>
    public bool Validate()
    {
        return SelectedBackup != null;
    }

    /// <summary>
    /// 获取对话框结果
    /// </summary>
    /// <returns>备份对话框结果</returns>
    public BackupDialogResult GetResult()
    {
        if (_deleteRequested && SelectedBackup != null)
        {
            return new BackupDialogResult
            {
                Action = BackupDialogAction.Delete,
                SelectedItem = SelectedBackup
            };
        }

        return new BackupDialogResult
        {
            Action = BackupDialogAction.Restore,
            SelectedItem = SelectedBackup
        };
    }

}
