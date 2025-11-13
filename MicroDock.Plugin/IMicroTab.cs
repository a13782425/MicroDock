namespace MicroDock.Plugin;
/// <summary>
/// 插件页签
/// </summary>
public interface IMicroTab
{
    /// <summary>
    /// 页签名字
    /// </summary>
    string TabName { get; }
    /// <summary>
    /// 页签图标
    /// </summary>
    IconSymbolEnum IconSymbol { get; }
    /// <summary>
    /// 是否使用父级的 ScrollViewer（默认 true）
    /// 设置为 false 时，标签页需要自己管理内部滚动
    /// </summary>
    bool UseParentScrollViewer => true;
}