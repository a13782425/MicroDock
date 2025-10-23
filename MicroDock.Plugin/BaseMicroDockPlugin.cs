namespace MicroDock.Plugin;
public abstract class BaseMicroDockPlugin : IMicroDockPlugin
{
    [Obsolete("不要手动创建插件",true)]
    public BaseMicroDockPlugin()
    {
    }
    /// <summary>
    /// 获取当前实例
    /// </summary>
    public BaseMicroDockPlugin Instance { get; private set; }
    /// <summary>
    /// 插件工具类
    /// </summary>
    private IMicroPluginUtils _utils;
    public abstract string UniqueName { get; }
    public abstract string[] Dependencies { get; }
    public abstract Version PluginVersion { get; }
    public abstract IMicroTab[] Tabs { get; }
    public virtual void OnInit() { }
    public virtual void OnEnable() { }
    public virtual void OnDisable() { } 
    public virtual void OnDestroy() { }
    
    public string GetConfigPath()
    {
        return _utils.GetConfigPath();
    }
    public string GetConfig(string key)
    {
        //获取config
        return "";
    }
}