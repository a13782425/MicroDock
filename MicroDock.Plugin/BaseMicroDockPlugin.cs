namespace MicroDock.Plugin;

/// <summary>
/// 插件基类，提供插件生命周期管理的基础实现
/// </summary>
public abstract class BaseMicroDockPlugin : IMicroDockPlugin
{
    /// <summary>
    /// 插件工具类
    /// </summary>
    private IMicroPluginUtils? _utils;

    protected BaseMicroDockPlugin()
    {
        // 子类可以正常构造
    }

    public abstract string UniqueName { get; }
    public abstract string[] Dependencies { get; }
    public abstract Version PluginVersion { get; }
    public abstract IMicroTab[] Tabs { get; }
    
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
    
    /// <summary>
    /// 获取插件配置路径
    /// </summary>
    public string GetConfigPath()
    {
        if (_utils == null)
        {
            // 如果工具类未初始化，返回默认路径
            return System.IO.Path.Combine(System.AppContext.BaseDirectory, "PluginConfigs", UniqueName);
        }
        return _utils.GetConfigPath();
    }
    
    /// <summary>
    /// 获取配置值
    /// </summary>
    public string GetConfig(string key)
    {
        // TODO: 实现配置读取逻辑
        return string.Empty;
    }
    
    /// <summary>
    /// 设置插件工具类（由框架调用）
    /// </summary>
    internal void SetPluginUtils(IMicroPluginUtils utils)
    {
        _utils = utils;
    }
}