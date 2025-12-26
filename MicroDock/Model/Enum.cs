using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Model;
/// <summary>
/// 文件类型
/// </summary>
public enum FileType
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknow = 0,
    /// <summary>
    /// 应用程序
    /// </summary>
    Exe,
    /// <summary>
    /// 快捷方式
    /// </summary>
    Lnk,
    /// <summary>
    /// 文件
    /// </summary>
    File,
    /// <summary>
    /// 文件夹
    /// </summary>
    Folder,
    Other
}

/// <summary>
/// 应用列表排序方式
/// </summary>
public enum ApplicationSortMode
{
    /// <summary>
    /// 按添加时间排序
    /// </summary>
    AddTime = 0,
    /// <summary>
    /// 按类型排序
    /// </summary>
    Type,

    /// <summary>
    /// 按名称排序
    /// </summary>
    Name,
    /// <summary>
    /// 按照使用量
    /// </summary>
    Usage,
}