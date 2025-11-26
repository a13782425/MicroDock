using MicroDock.Plugin;
using MicroNotePlugin.Views;

namespace MicroNotePlugin;

/// <summary>
/// 随手记插件 - 提供 Markdown 笔记管理功能
/// </summary>
public class MicroNotePlugin : BaseMicroDockPlugin
{
    private MicroNoteTab? _tab;

    /// <summary>
    /// 插件标签页
    /// </summary>
    public override IMicroTab[] Tabs => _tab != null 
        ? new IMicroTab[] { _tab } 
        : Array.Empty<IMicroTab>();

    /// <summary>
    /// 插件初始化
    /// </summary>
    public override void OnInit()
    {
        base.OnInit();
        
        // 创建主页面
        _tab = new MicroNoteTab(this);
        
        LogInfo("随手记插件已初始化");
    }

    /// <summary>
    /// 插件启用
    /// </summary>
    public override void OnEnable()
    {
        base.OnEnable();
        LogInfo("随手记插件已启用");
    }

    /// <summary>
    /// 插件禁用
    /// </summary>
    public override void OnDisable()
    {
        base.OnDisable();
        LogInfo("随手记插件已禁用");
    }

    /// <summary>
    /// 插件销毁
    /// </summary>
    public override void OnDestroy()
    {
        base.OnDestroy();
        LogInfo("随手记插件已销毁");
    }
}
