using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroDock.Procedure;

/// <summary>
/// 启动过程基类
/// </summary>
internal abstract class BaseLaunchProcedure
{
    /// <summary>
    /// 流程名称
    /// </summary>
    public abstract string ProcedureName { get; }

    /// <summary>
    /// 流程描述
    /// </summary>
    public virtual string Description => string.Empty;

    /// <summary>
    /// 下一个流程
    /// </summary>
    public virtual BaseLaunchProcedure NextProcedure => null;

    /// <summary>
    /// 当前流程对应的进度值
    /// </summary>
    public abstract int Progress { get; }

    public abstract Task ExecuteAsync();
}
