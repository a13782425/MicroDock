using ClaudeSwitchPlugin.Views;
using MicroDock.Plugin;
using System;

namespace ClaudeSwitchPlugin
{
    /// <summary>
    /// Claude 配置切换插件
    /// </summary>
    public class ClaudeSwitchPlugin : BaseMicroDockPlugin
    {
        private ClaudeSwitchTabView? _mainTabView;

        public override IMicroTab[] Tabs
        {
            get
            {
                if (_mainTabView == null)
                {
                    _mainTabView = new ClaudeSwitchTabView();
                }
                return new IMicroTab[] { _mainTabView };
            }
        }

        public override void OnInit()
        {
            base.OnInit();
            LogInfo("Claude 配置切换插件初始化中...");
        }

        public override void OnEnable()
        {
            base.OnEnable();
            LogInfo("Claude 配置切换插件已启用");
        }

        public override void OnDisable()
        {
            base.OnDisable();
            LogInfo("Claude 配置切换插件已禁用");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogInfo("Claude 配置切换插件已销毁");
        }
    }
}