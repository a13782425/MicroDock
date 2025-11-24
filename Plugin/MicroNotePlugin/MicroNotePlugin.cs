using MicroDock.Plugin;
using Avalonia.Controls;
using MicroNotePlugin.Views;

namespace MicroNotePlugin
{
    public class MicroNotePlugin : BaseMicroDockPlugin
    {
        private MicroNoteTab? _tab;

        public override IMicroTab[] Tabs => _tab != null ? new IMicroTab[] { _tab } : Array.Empty<IMicroTab>();

        public override void OnInit()
        {
            // 创建插件主页面
            _tab = new MicroNoteTab(this);
            LogInfo("MicroNote 插件已初始化");
        }
    }
}
