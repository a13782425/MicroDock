using ReactiveUI;

namespace MicroDock.Models
{
    /// <summary>
    /// 导航项模型
    /// </summary>
    public class NavigationItemModel : ReactiveObject
    {
        private string _title = string.Empty;
        private string _icon = string.Empty;
        private object? _content;
        private NavigationType _navType;

        /// <summary>
        /// 导航项标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        /// <summary>
        /// 图标名称（对应Symbol枚举）
        /// </summary>
        public string Icon
        {
            get => _icon;
            set => this.RaiseAndSetIfChanged(ref _icon, value);
        }

        /// <summary>
        /// 导航项内容（View实例）
        /// </summary>
        public object? Content
        {
            get => _content;
            set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        /// <summary>
        /// 导航类型
        /// </summary>
        public NavigationType NavType
        {
            get => _navType;
            set => this.RaiseAndSetIfChanged(ref _navType, value);
        }
    }

    /// <summary>
    /// 导航类型枚举
    /// </summary>
    public enum NavigationType
    {
        /// <summary>
        /// 应用管理
        /// </summary>
        Application,

        /// <summary>
        /// 插件
        /// </summary>
        Plugin,

        /// <summary>
        /// 系统功能（如日志查看器等）
        /// </summary>
        System,

        /// <summary>
        /// 设置
        /// </summary>
        Settings
    }
}

