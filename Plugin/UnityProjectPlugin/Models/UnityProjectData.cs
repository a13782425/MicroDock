using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityProjectPlugin.Models;

internal static class UnityProjectData
{
    /// <summary>
    /// Unity项目
    /// </summary>
    public readonly static List<UnityProject> Projects = new();
    /// <summary>
    /// Unity版本
    /// </summary>
    public readonly static List<UnityVersion> Versions = new();
    /// <summary>
    /// 项目分组
    /// </summary>
    public readonly static List<ProjectGroup> Groups = new();
    /// <summary>
    /// 插件配置
    /// </summary>
    public static PluginSettings Settings = new();
}
