using MicroDock.Plugin;
namespace Test;
public class TestPlugin : BaseMicroDockPlugin
{    [Obsolete("不要手动创建插件",true)]
    public TestPlugin()
    {
    }
    public override string UniqueName { get; }
    public override string[] Dependencies { get; }
    public override Version PluginVersion { get; }
    public override IMicroTab[] Tabs { get; }
}