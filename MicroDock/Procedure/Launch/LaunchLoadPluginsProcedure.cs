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
            LogService.LogInformation("开始异步加载所有插件");

            var pluginService = ServiceLocator.Get<PluginService>();
            var loadedPlugins = await pluginService.LoadPluginsAsync();
            LogService.LogInformation($"插件加载完成,共加载 {loadedPlugins.Count} 个插件");
        }
        catch (Exception ex)
        {
            LogService.LogError("插件加载过程中发生错误", ex: ex);
            // 即使失败也继续启动,允许用户使用基础功能
        }
    }
}
