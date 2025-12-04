using Avalonia.Controls;
using MicroDock.Plugin;
using System;
using System.IO;
using TodoListPlugin.Views;

namespace TodoListPlugin
{
    /// <summary>
    /// 待办清单插件 - 使用看板模式
    /// </summary>
    public class TodoListPlugin : BaseMicroDockPlugin
    {
        private string _dataFolder = string.Empty;
        private TodoListTabView? _tabView;

        public override IMicroTab[] Tabs
        {
            get
            {
                if (_tabView == null && !string.IsNullOrEmpty(_dataFolder))
                {
                    _tabView = new TodoListTabView(_dataFolder);
                }
                return _tabView != null ? new IMicroTab[] { _tabView } : Array.Empty<IMicroTab>();
            }
        }

        public override object? GetSettingsControl()
        {
            // 设置已集成到主视图中
            return null;
        }

        public override void OnInit()
        {
            base.OnInit();

            LogInfo("待办清单插件初始化中...");

            // 初始化数据文件夹路径
            _dataFolder = Context?.DataPath ?? string.Empty;
            if (string.IsNullOrEmpty(_dataFolder))
            {
                LogError("无法获取插件数据文件夹路径");
                return;
            }

            // 确保数据文件夹存在
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
                LogInfo($"创建数据文件夹: {_dataFolder}");
            }

            LogInfo("待办清单插件初始化完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogInfo("待办清单插件销毁");
        }
    }
}
