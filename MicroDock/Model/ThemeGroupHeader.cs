namespace MicroDock.Model;

/// <summary>
/// 用于在主题选择下拉列表中表示一个分组标题
/// </summary>
public class ThemeGroupHeader
{
    public string GroupName { get; set; } = string.Empty;
    public bool IsGroupHeader => true; // 标记为分组标题
    
    // 为了在 ComboBox 中与 ThemeModel 兼容，添加空的 DisplayName
    public string DisplayName => string.Empty;
}
