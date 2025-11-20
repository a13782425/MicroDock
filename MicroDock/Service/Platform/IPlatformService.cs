using System;
using Avalonia.Controls;

namespace MicroDock.Service.Platform;

/// <summary>
/// 平台特定服务接口
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// 初始化服务（传入主窗口）
    /// </summary>
    void Initialize(Window window);

    /// <summary>
    /// 注册全局热键
    /// </summary>
    /// <param name="uniqueId">唯一ID</param>
    /// <param name="keyCombination">快捷键组合 (e.g. "Ctrl+Alt+T")</param>
    /// <param name="callback">触发回调</param>
    /// <returns>是否成功</returns>
    bool RegisterHotKey(string uniqueId, string keyCombination, Action callback);

    /// <summary>
    /// 注销全局热键
    /// </summary>
    /// <param name="uniqueId">唯一ID</param>
    void UnregisterHotKey(string uniqueId);
}

