namespace MicroDock.Plugin;

/// <summary>
/// 插件基类，提供插件生命周期管理的基础实现
/// 注意：插件元数据（名称、版本、依赖等）现在通过 plugin.json 配置文件提供
/// </summary>
public abstract class BaseMicroDockPlugin : IMicroDockPlugin
{
    /// <summary>
    /// 插件上下文（由框架通过 Initialize 方法注入）
    /// </summary>
    public IPluginContext? Context { get; private set; }

    /// <summary>
    /// 初始化插件上下文（由框架调用）
    /// </summary>
    public void Initialize(IPluginContext context)
    {
        if (Context != null)
        {
            throw new InvalidOperationException("插件的上下文已经被初始化");
        }
        
        Context = context ?? throw new ArgumentNullException(nameof(context));
        
        // Initialize 之后调用 OnInit
        OnInit();
    }

    public abstract IMicroTab[] Tabs { get; }

    /// <summary>
    /// 获取插件的设置UI控件（可选，返回null表示没有设置）
    /// </summary>
    public virtual object? GetSettingsControl()
    {
        // 默认返回null，表示没有设置
        return null;
    }

    /// <summary>
    /// 插件初始化
    /// </summary>
    public virtual void OnInit() 
    { 
        // 子类可以重写此方法
    }
    /// <summary>
    /// 异步初始化(可选)
    /// 如果插件需要执行异步初始化操作(如网络请求、文件IO等),请重写此方法
    /// 框架会优先调用此方法,如果插件未重写此方法,则回退到调用 OnInit
    /// </summary>
    /// <returns>初始化任务</returns>
    public virtual Task OnInitAsync()
    {
        // 默认实现:同步调用 OnInit 并返回已完成的 Task
        OnInit();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 插件启用
    /// </summary>
    public virtual void OnEnable() 
    { 
        // 子类可以重写此方法
    }
    
    /// <summary>
    /// 插件禁用
    /// </summary>
    public virtual void OnDisable() 
    { 
        // 子类可以重写此方法
    }
    
    /// <summary>
    /// 插件销毁
    /// </summary>
    public virtual void OnDestroy() 
    { 
        // 子类可以重写此方法
    }

    /// <summary>
    /// 所有插件初始化完成后调用（此时可以安全地调用其他插件的工具）
    /// </summary>
    public virtual void OnAllPluginsLoaded()
    {
        // 子类可以重写此方法
    }
    
    #region 便捷方法 - 工具调用
    
    /// <summary>
    /// 调用工具（异步）
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <param name="parameters">参数字典（字符串键值对）</param>
    /// <param name="pluginName">可选的插件名称</param>
    protected async System.Threading.Tasks.Task<string> CallToolAsync(
        string toolName,
        System.Collections.Generic.Dictionary<string, string> parameters,
        string? pluginName = null)
    {
        EnsureContextInitialized();
        return await Context!.CallToolAsync(toolName, parameters, pluginName);
    }
    
    /// <summary>
    /// 获取所有可用工具
    /// </summary>
    protected System.Collections.Generic.List<ToolInfo> GetAvailableTools()
    {
        EnsureContextInitialized();
        return Context!.GetAvailableTools();
    }
    
    /// <summary>
    /// 获取指定插件的工具列表
    /// </summary>
    protected System.Collections.Generic.List<ToolInfo> GetPluginTools(string pluginName)
    {
        EnsureContextInitialized();
        return Context!.GetPluginTools(pluginName);
    }
    
    /// <summary>
    /// 获取工具详细信息
    /// </summary>
    protected ToolInfo? GetToolInfo(string toolName, string? pluginName = null)
    {
        EnsureContextInitialized();
        return Context!.GetToolInfo(toolName, pluginName);
    }
    
    #endregion
    
    #region 便捷方法 - 日志
    
    /// <summary>
    /// 输出调试日志
    /// </summary>
    protected void LogDebug(string message)
    {
        EnsureContextInitialized();
        Context!.LogDebug(message);
    }
    
    /// <summary>
    /// 输出信息日志
    /// </summary>
    protected void LogInfo(string message)
    {
        EnsureContextInitialized();
        Context!.LogInfo(message);
    }
    
    /// <summary>
    /// 输出警告日志
    /// </summary>
    protected void LogWarning(string message)
    {
        EnsureContextInitialized();
        Context!.LogWarning(message);
    }
    
    /// <summary>
    /// 输出错误日志
    /// </summary>
    protected void LogError(string message, Exception? exception = null)
    {
        EnsureContextInitialized();
        Context!.LogError(message, exception);
    }
    
    #endregion
    
    #region 便捷方法 - 键值存储
    
    /// <summary>
    /// 设置键值对
    /// </summary>
    protected void SetValue(string key, string value)
    {
        EnsureContextInitialized();
        Context!.SetValue(key, value);
    }
    
    /// <summary>
    /// 获取键值
    /// </summary>
    protected string? GetValue(string key)
    {
        EnsureContextInitialized();
        return Context!.GetValue(key);
    }
    
    /// <summary>
    /// 删除键值
    /// </summary>
    protected void DeleteValue(string key)
    {
        EnsureContextInitialized();
        Context!.DeleteValue(key);
    }
    
    /// <summary>
    /// 获取所有键
    /// </summary>
    protected List<string> GetAllKeys()
    {
        EnsureContextInitialized();
        return Context!.GetAllKeys();
    }
    
    #endregion
    
    #region 便捷方法 - 设置
    
    /// <summary>
    /// 获取设置
    /// </summary>
    protected string? GetSettings(string key)
    {
        EnsureContextInitialized();
        return Context!.GetSettings(key);
    }
    
    /// <summary>
    /// 设置设置
    /// </summary>
    protected void SetSettings(string key, string value, string? description = null)
    {
        EnsureContextInitialized();
        Context!.SetSettings(key, value, description);
    }
    
    /// <summary>
    /// 删除设置
    /// </summary>
    protected void DeleteSettings(string key)
    {
        EnsureContextInitialized();
        Context!.DeleteSettings(key);
    }
    
    /// <summary>
    /// 获取所有设置键
    /// </summary>
    protected List<string> GetAllSettingsKeys()
    {
        EnsureContextInitialized();
        return Context!.GetAllSettingsKeys();
    }
    
    #endregion
    
    #region 便捷方法 - 依赖访问
    
    /// <summary>
    /// 从依赖插件获取键值（只读）
    /// </summary>
    protected string? GetValueFromDependency(string dependencyPluginName, string key)
    {
        EnsureContextInitialized();
        return Context!.GetValueFromDependency(dependencyPluginName, key);
    }
    
    /// <summary>
    /// 获取依赖插件的所有键（只读）
    /// </summary>
    protected List<string> GetKeysFromDependency(string dependencyPluginName)
    {
        EnsureContextInitialized();
        return Context!.GetKeysFromDependency(dependencyPluginName);
    }
    
    /// <summary>
    /// 获取依赖插件的设置（只读）
    /// </summary>
    protected string? GetSettingsFromDependency(string dependencyPluginName, string key)
    {
        EnsureContextInitialized();
        return Context!.GetSettingsFromDependency(dependencyPluginName, key);
    }
    
    /// <summary>
    /// 获取依赖插件的所有设置键（只读）
    /// </summary>
    protected List<string> GetAllSettingsKeysFromDependency(string dependencyPluginName)
    {
        EnsureContextInitialized();
        return Context!.GetAllSettingsKeysFromDependency(dependencyPluginName);
    }
    
    #endregion
    
    #region 便捷方法 - 图片管理
    
    /// <summary>
    /// 保存图片
    /// </summary>
    protected void SaveImage(string key, byte[] imageData)
    {
        EnsureContextInitialized();
        Context!.SaveImage(key, imageData);
    }
    
    /// <summary>
    /// 加载图片
    /// </summary>
    protected byte[]? LoadImage(string key)
    {
        EnsureContextInitialized();
        return Context!.LoadImage(key);
    }
    
    /// <summary>
    /// 删除图片
    /// </summary>
    protected void DeleteImage(string key)
    {
        EnsureContextInitialized();
        Context!.DeleteImage(key);
    }
    
    #endregion
    
    #region 便捷方法 - 路径
    
    /// <summary>
    /// 获取插件配置目录
    /// </summary>
    protected string GetConfigPath()
    {
        EnsureContextInitialized();
        return Context!.ConfigPath;
    }
    
    /// <summary>
    /// 获取插件数据目录
    /// </summary>
    protected string GetPluginDataPath()
    {
        EnsureContextInitialized();
        return Context!.DataPath;
    }

    /// <summary>
    /// 获取插件的临时目录
    /// </summary>
    /// <returns></returns>
    protected string GetPluginTempDataPath()
    {
        EnsureContextInitialized();
        return Context!.TempDataPath;
    }

    #endregion

    /// <summary>
    /// 确保上下文已初始化
    /// </summary>
    private void EnsureContextInitialized()
    {
        if (Context == null)
        {
            throw new InvalidOperationException(
                "插件上下文未初始化。请确保插件已通过 Initialize 方法正确初始化。");
        }
    }
}