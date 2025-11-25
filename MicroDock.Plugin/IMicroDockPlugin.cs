namespace MicroDock.Plugin;

/// <summary>
/// 插件接口
/// 注意:插件元数据(名称、版本、依赖等)现在通过 plugin.json 配置文件提供
/// </summary>
public interface IMicroDockPlugin
{
    /// <summary>
    /// 初始化插件上下文(由框架调用,之后会调用 OnInit 或 OnInitAsync)
    /// </summary>
    void Initialize(IPluginContext context);

    /// <summary>
    /// 所有标签页
    /// </summary>
    IMicroTab[] Tabs { get; }

    /// <summary>
    /// 获取插件的设置UI控件(可选,返回null表示没有设置)
    /// </summary>
    /// <returns>设置UI控件,如果没有设置则返回null</returns>
    object? GetSettingsControl();

    /// <summary>
    /// 同步初始化(保留以维持向后兼容)
    /// 如果插件实现了 OnInitAsync,框架会优先调用 OnInitAsync 而不是此方法
    /// </summary>
    void OnInit();

    /// <summary>
    /// 异步初始化(可选)
    /// 如果插件需要执行异步初始化操作(如网络请求、文件IO等),请重写此方法
    /// 框架会优先调用此方法,如果插件未重写此方法,则回退到调用 OnInit
    /// </summary>
    /// <returns>初始化任务</returns>
    Task OnInitAsync()
    {
        // 默认实现:同步调用 OnInit 并返回已完成的 Task
        OnInit();
        return Task.CompletedTask;
    }

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

    /// <summary>
    /// 所有插件初始化完成后调用(此时可以安全地调用其他插件的工具)
    /// </summary>
    void OnAllPluginsLoaded();
}