using MicroDock.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Procedure;

internal class LaunchInitializeProcedure : BaseLaunchProcedure
{
    public override string ProcedureName => "初始化";

    public override string Description => "正在初始化应用...";

    public override BaseLaunchProcedure NextProcedure => new LaunchLoadPluginsProcedure();
    
    public override int Progress => 20;

    public override async Task ExecuteAsync()
    {
        // 初始化基础配置(如需要可以在这里添加数据库初始化等)
        await Task.Delay(new Random().Next(50, 150));
    }
}
