namespace MicroNotePlugin.Models;

/// <summary>
/// 虚拟文件夹模型（运行时使用）
/// </summary>
public class NoteFolder
{
    /// <summary>
    /// 文件夹名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 虚拟路径（如 "/工作/项目"）
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// 父文件夹路径
    /// </summary>
    public string ParentPath { get; set; } = string.Empty;

    /// <summary>
    /// 子文件夹列表
    /// </summary>
    public List<NoteFolder> SubFolders { get; set; } = new();

    /// <summary>
    /// 子文件列表
    /// </summary>
    public List<NoteFile> Files { get; set; } = new();

    /// <summary>
    /// 是否是根文件夹
    /// </summary>
    public bool IsRoot => Path == "/";

    /// <summary>
    /// 从路径创建 NoteFolder
    /// </summary>
    public static NoteFolder FromPath(string path)
    {
        var name = path == "/" ? "根目录" : System.IO.Path.GetFileName(path.TrimEnd('/'));
        var parentPath = path == "/" ? "" : GetParentPath(path);
        
        return new NoteFolder
        {
            Name = name,
            Path = path,
            ParentPath = parentPath
        };
    }

    /// <summary>
    /// 获取父路径
    /// </summary>
    private static string GetParentPath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
            return "";
        
        var trimmed = path.TrimEnd('/');
        var lastSlash = trimmed.LastIndexOf('/');
        
        if (lastSlash <= 0)
            return "/";
        
        return trimmed.Substring(0, lastSlash);
    }
}
