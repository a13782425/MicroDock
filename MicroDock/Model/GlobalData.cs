using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicroDock.Views.Dialog;

namespace MicroDock.Model;

/// <summary>
/// 全局数据
/// </summary>
internal static class GlobalData
{
    /// <summary>
    /// 临时备份列表 - 用于对话框数据传递
    /// </summary>
    public static ObservableCollection<BackupListItem>? TempBackupList { get; set; }

    /// <summary>
    /// 临时插件列表 - 用于对话框数据传递
    /// </summary>
    public static ObservableCollection<RemotePluginListItem>? TempPluginList { get; set; }

    static GlobalData()
    {
    }
}
