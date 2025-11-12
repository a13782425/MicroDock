namespace MicroDock.Plugin;

/// <summary>
/// 插件基类，提供插件生命周期管理的基础实现
/// </summary>
public abstract class BaseMicroDockPlugin : IMicroDockPlugin
{
    /// <summary>
    /// 插件上下文（由框架通过 Initialize 方法注入）
    /// </summary>
    protected IPluginContext? Context { get; private set; }

    /// <summary>
    /// 插件唯一名称
    /// 格式com.xxxx.xxx
    /// </summary>
    public abstract string UniqueName { get; }
    /// <summary>
    /// 显示名称
    /// </summary>
    public virtual string DisplayName => UniqueName;

    /// <summary>
    /// 依赖的插件列表
    /// </summary>
    public abstract string[] Dependencies { get; }
    
    /// <summary>
    /// 插件版本
    /// </summary>
    public abstract Version PluginVersion { get; }

    /// <summary>
    /// 初始化插件上下文（由框架调用）
    /// </summary>
    public void Initialize(IPluginContext context)
    {
        if (Context != null)
        {
            throw new InvalidOperationException($"插件 '{UniqueName}' 的上下文已经被初始化");
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
    
    #endregion
    
    /// <summary>
    /// 确保上下文已初始化
    /// </summary>
    private void EnsureContextInitialized()
    {
        if (Context == null)
        {
            throw new InvalidOperationException(
                $"插件上下文未初始化。请确保插件 '{UniqueName}' 已通过 Initialize 方法正确初始化。");
        }
    }
}