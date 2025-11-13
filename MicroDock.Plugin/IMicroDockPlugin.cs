namespace MicroDock.Plugin;

/// <summary>
/// 插件接口
/// 注意：插件元数据（名称、版本、依赖等）现在通过 plugin.json 配置文件提供
/// </summary>
public interface IMicroDockPlugin
{
    /// <summary>
    /// 初始化插件上下文（由框架调用，之后会调用 OnInit）
    /// </summary>
    void Initialize(IPluginContext context);

    /// <summary>
    /// 所有标签页
    /// </summary>
    IMicroTab[] Tabs { get; }

    /// <summary>
    /// 获取插件的设置UI控件（可选，返回null表示没有设置）
    /// </summary>
    /// <returns>设置UI控件，如果没有设置则返回null</returns>
    object? GetSettingsControl();

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