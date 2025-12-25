using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Database;

/// <summary>
/// 用户偏好
/// </summary>
public class UserPreferenceDB 
{
    /// <summary>
    /// 用户偏好的Key
    /// </summary>
    [PrimaryKey]
    public string PreferenceKey { get; set; } = string.Empty;

    /// <summary>
    /// 用户偏好的值
    /// </summary>
    public string PreferenceValue { get; set; } = string.Empty;

}
