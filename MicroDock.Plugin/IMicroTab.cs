﻿namespace MicroDock.Plugin;
/// <summary>
/// 插件页签
/// </summary>
public interface IMicroTab
{
    /// <summary>
    /// 页签名字
    /// </summary>
    string TabName { get; }
}