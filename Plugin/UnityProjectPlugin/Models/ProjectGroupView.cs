using System;

namespace UnityProjectPlugin.Models
{
    /// <summary>
    /// 项目分组模型（用于视图显示）
    /// </summary>
    public class ProjectGroupView
    {
        /// <summary>
        /// 分组名称（空表示"未分组"）
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// 该分组下的项目列表
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<UnityProject> Projects { get; set; } = new();

        /// <summary>
        /// 项目计数描述（用于 SettingsExpander.Description）
        /// </summary>
        public string ProjectCountDescription => $"{Projects.Count} 个项目";
    }
}
