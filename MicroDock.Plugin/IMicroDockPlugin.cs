namespace MicroDock.Plugin;
/// <summary>
/// 插件相关配置
/// </summary>
internal interface IMicroDockPlugin
{
    /// <summary>
    /// 唯一名字
    /// 用于确保插件的唯一性和依赖
    /// </summary>
    string UniqueName { get; }
    /// <summary>
    /// 依赖列表
    /// 冒号后面没有版本号则默认为全版本
    /// 依赖插件唯一名:版本号
    /// 支持>,=,<
    /// </summary>
    string[] Dependencies { get; }
    /// <summary>
    /// 插件版本
    /// </summary>
    Version PluginVersion { get; }
    /// <summary>
    /// 所有页签
    /// </summary>
    IMicroTab[] Tabs { get; }
    /// <summary>
    /// 初始化
    /// </summary>
    void OnInit();
    /// <summary>
    /// 插件启用
    /// </summary>
    void OnEnable();
    /// <summary>
    /// 插件禁用
    /// </summary>
    void OnDisable();
    /// <summary>
    /// 释放
    /// </summary>
    void OnDestroy();
    
}