namespace UnityProjectPlugin.Models
{
    /// <summary>
    /// Unity 版本模型
    /// </summary>
    public class UnityVersion
    {
        /// <summary>
        /// 版本号（如 2022.3.10f1）
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Unity Editor 可执行文件路径
        /// </summary>
        public string EditorPath { get; set; } = string.Empty;

        /// <summary>
        /// 版本唯一标识
        /// </summary>
        public string Id => Version;
    }
}

