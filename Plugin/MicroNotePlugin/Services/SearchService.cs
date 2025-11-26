using MicroNotePlugin.Models;

namespace MicroNotePlugin.Services;

/// <summary>
/// 搜索结果项
/// </summary>
public class SearchResultItem
{
    /// <summary>
    /// 文件信息
    /// </summary>
    public NoteFile File { get; set; } = null!;

    /// <summary>
    /// 匹配的行内容
    /// </summary>
    public List<SearchMatchLine> MatchLines { get; set; } = new();

    /// <summary>
    /// 是否文件名匹配
    /// </summary>
    public bool IsFileNameMatch { get; set; }

    /// <summary>
    /// 匹配总数
    /// </summary>
    public int TotalMatches => MatchLines.Sum(m => m.MatchCount) + (IsFileNameMatch ? 1 : 0);
}

/// <summary>
/// 匹配的行
/// </summary>
public class SearchMatchLine
{
    /// <summary>
    /// 行号（从1开始）
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// 行内容
    /// </summary>
    public string LineContent { get; set; } = string.Empty;

    /// <summary>
    /// 该行匹配次数
    /// </summary>
    public int MatchCount { get; set; }

    /// <summary>
    /// 匹配位置列表（起始索引）
    /// </summary>
    public List<int> MatchPositions { get; set; } = new();
}

/// <summary>
/// 搜索服务 - 提供全文搜索功能
/// </summary>
public class SearchService
{
    private readonly NoteFileService _fileService;

    public SearchService(NoteFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// 搜索笔记
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="searchContent">是否搜索内容（否则只搜索文件名）</param>
    /// <param name="caseSensitive">是否区分大小写</param>
    /// <returns>搜索结果列表</returns>
    public List<SearchResultItem> Search(string keyword, bool searchContent = true, bool caseSensitive = false)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return new List<SearchResultItem>();

        var results = new List<SearchResultItem>();
        var allFiles = _fileService.GetAllNoteFiles();
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        foreach (var file in allFiles)
        {
            var result = new SearchResultItem { File = file };

            // 检查文件名匹配
            if (file.Name.Contains(keyword, comparison))
            {
                result.IsFileNameMatch = true;
            }

            // 搜索内容
            if (searchContent)
            {
                // 使用 Hash 读取内容
                var content = _fileService.ReadNoteContent(file.Hash);
                var matchLines = SearchInContent(content, keyword, caseSensitive);
                result.MatchLines = matchLines;
            }

            // 如果有匹配，添加到结果
            if (result.IsFileNameMatch || result.MatchLines.Count > 0)
            {
                results.Add(result);
            }
        }

        // 按匹配数排序
        return results.OrderByDescending(r => r.TotalMatches).ToList();
    }

    /// <summary>
    /// 异步搜索
    /// </summary>
    public async Task<List<SearchResultItem>> SearchAsync(string keyword, bool searchContent = true, bool caseSensitive = false)
    {
        return await Task.Run(() => Search(keyword, searchContent, caseSensitive));
    }

    /// <summary>
    /// 在内容中搜索
    /// </summary>
    private List<SearchMatchLine> SearchInContent(string content, string keyword, bool caseSensitive)
    {
        var results = new List<SearchMatchLine>();
        if (string.IsNullOrEmpty(content))
            return results;

        var lines = content.Split('\n');
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var positions = FindAllOccurrences(line, keyword, caseSensitive);

            if (positions.Count > 0)
            {
                results.Add(new SearchMatchLine
                {
                    LineNumber = i + 1,
                    LineContent = line.TrimEnd('\r'),
                    MatchCount = positions.Count,
                    MatchPositions = positions
                });
            }
        }

        return results;
    }

    /// <summary>
    /// 查找所有匹配位置
    /// </summary>
    private List<int> FindAllOccurrences(string text, string keyword, bool caseSensitive)
    {
        var positions = new List<int>();
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        int index = 0;
        while ((index = text.IndexOf(keyword, index, comparison)) != -1)
        {
            positions.Add(index);
            index += keyword.Length;
        }

        return positions;
    }

    /// <summary>
    /// 获取搜索结果的上下文预览
    /// </summary>
    /// <param name="line">匹配行</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>带省略号的预览文本</returns>
    public static string GetPreviewText(SearchMatchLine line, int maxLength = 100)
    {
        var content = line.LineContent.Trim();
        if (content.Length <= maxLength)
            return content;

        // 尝试围绕第一个匹配位置截取
        if (line.MatchPositions.Count > 0)
        {
            var firstMatch = line.MatchPositions[0];
            var start = Math.Max(0, firstMatch - maxLength / 2);
            var end = Math.Min(content.Length, start + maxLength);

            var preview = content.Substring(start, end - start);
            if (start > 0) preview = "..." + preview;
            if (end < content.Length) preview += "...";

            return preview;
        }

        return content.Substring(0, maxLength) + "...";
    }
}
