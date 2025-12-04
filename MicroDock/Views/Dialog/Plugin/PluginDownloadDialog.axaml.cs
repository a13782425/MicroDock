using Avalonia.Controls;
using MicroDock.Model;
using MicroDock.Utils;
using System.Collections.ObjectModel;

namespace MicroDock.Views.Dialog;

/// <summary>
/// 远程插件列表项
/// </summary>
public class RemotePluginListItem
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public bool NeedsUpdate { get; set; }
    public string? InstalledVersion { get; set; }
}

/// <summary>
/// 插件下载对话框 - 用于从服务器选择插件进行安装
/// </summary>
public partial class PluginDownloadDialog : UserControl, ICustomDialog<RemotePluginListItem>
{
    /// <summary>
    /// 获取选中的插件
    /// </summary>
    public RemotePluginListItem? SelectedPlugin => PluginListBox.SelectedItem as RemotePluginListItem;

    public PluginDownloadDialog()
    {
        InitializeComponent();

        // 从 GlobalData 读取数据
        if (GlobalData.TempPluginList != null)
        {
            SetPluginList(GlobalData.TempPluginList);
        }
    }

    /// <summary>
    /// 设置插件列表
    /// </summary>
    public void SetPluginList(ObservableCollection<RemotePluginListItem> plugins)
    {
        PluginListBox.ItemsSource = plugins;
        TitleText.Text = $"找到 {plugins.Count} 个可用插件，选择一个进行安装：";
    }

    /// <summary>
    /// 验证选择
    /// </summary>
    public bool Validate()
    {
        return SelectedPlugin != null;
    }

    /// <summary>
    /// 获取对话框结果
    /// </summary>
    /// <returns>选中的插件</returns>
    public RemotePluginListItem GetResult() => SelectedPlugin!;

}
