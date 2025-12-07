using MicroNotePlugin.Core.Entities;

namespace MicroNotePlugin.Core.Interfaces;

/// <summary>
/// 搜索选项
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// 是否搜索内容 (否则只搜索文件名)
    /// </summary>
    public bool SearchContent { get; set; } = true;

    /// <summary>
    /// 是否区分大小写
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// 最大结果数量
    /// </summary>
    public int MaxResults { get; set; } = 50;
}

/// <summary>
/// 搜索匹配行
/// </summary>
public class SearchMatchLine
{
    /// <summary>
    /// 行号 (从1开始)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// 行内容
    /// </summary>
    public string LineContent { get; set; } = string.Empty;

    /// <summary>
    /// 匹配位置列表
    /// </summary>
    public List<int> MatchPositions { get; set; } = new();
}

/// <summary>
/// 搜索结果
/// </summary>
public class SearchResult
{
    /// <summary>
    /// 匹配的笔记
    /// </summary>
    public Note Note { get; set; } = null!;

    /// <summary>
    /// 是否文件名匹配
    /// </summary>
    public bool IsNameMatch { get; set; }

    /// <summary>
    /// 匹配的行列表
    /// </summary>
    public List<SearchMatchLine> MatchLines { get; set; } = new();

    /// <summary>
    /// 预览文本片段
    /// </summary>
    public string Snippet { get; set; } = string.Empty;

    /// <summary>
    /// 总匹配数
    /// </summary>
    public int TotalMatches => MatchLines.Sum(m => m.MatchPositions.Count) + (IsNameMatch ? 1 : 0);
}

/// <summary>
/// 搜索服务接口
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// 搜索笔记
    /// </summary>
    Task<IEnumerable<SearchResult>> SearchAsync(string keyword, SearchOptions? options = null);

    /// <summary>
    /// 重建搜索索引
    /// </summary>
    Task RebuildIndexAsync();
}
