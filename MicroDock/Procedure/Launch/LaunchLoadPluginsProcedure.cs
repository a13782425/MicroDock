using MicroDock.Service;
using Serilog;
using System;
using System.Threading.Tasks;

namespace MicroDock.Procedure;

/// <summary>
/// 插件加载启动程序
/// </summary>
internal class LaunchLoadPluginsProcedure : BaseLaunchProcedure
{
    public override string ProcedureName => "加载插件";

    public override string Description => "正在加载插件...";

    public override int Progress => 60;

    public override BaseLaunchProcedure NextProcedure => null; // 最后一个启动步骤

    public override async Task ExecuteAsync()
    {
        try
        {
            LogService.LogInformation("开始异步加载所有插件", DEFAULT_LOG_TAG);

            var pluginService = ServiceLocator.Get<PluginService>();
            if (pluginService == null)
            {
                LogInformation($"没有找到插件服务", DEFAULT_LOG_TAG);
                return;
            }
            await pluginService.LoadPluginsAsync();
            LogService.LogInformation($"插件加载完成,共加载 {pluginService.LoadedPlugins.Count} 个插件", DEFAULT_LOG_TAG);
        }
        catch (Exception ex)
        {
            LogService.LogError("插件加载过程中发生错误", DEFAULT_LOG_TAG, ex: ex);
            // 即使失败也继续启动,允许用户使用基础功能
        }
    }
}
