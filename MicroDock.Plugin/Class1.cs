namespace MicroDock.Plugin;
public interface IMicroActionsProvider
{
    /// <summary>
    /// 返回供环形菜单使用的动作集合
    /// </summary>
    System.Collections.Generic.IEnumerable<MicroAction> GetActions();
}
public sealed class MicroAction
{
    public string Name { get; set; } = string.Empty;
    public byte[]? IconBytes { get; set; }
    public string Command { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    
}