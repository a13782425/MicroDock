using MicroNotePlugin.Entities;
using MicroNotePlugin.Database;

namespace MicroNotePlugin.Services;

/// <summary>
/// 全文搜索服务实现
/// </summary>
public class FullTextSearchService : ISearchService
{
    private readonly NoteDbContext _context;

    public FullTextSearchService(NoteDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SearchResult>> SearchAsync(string keyword, SearchOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Enumerable.Empty<SearchResult>();
        }

        options ??= new SearchOptions();
        var maxResults = options.MaxResults;
        var query = keyword.Trim();
        var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        // 获取所有笔记 (简单实现，数据量大时需优化)
        // sqlite-net-pcl 的 Where 表达式不支持复杂的 StringComparison，所以先拉取再过滤
        // 或者使用 SQL LIKE
        
        var sql = "SELECT * FROM notes WHERE name LIKE ? OR content LIKE ?";
        var likeQuery = $"%{query}%";
        var notes = await _context.Connection.QueryAsync<Note>(sql, likeQuery, likeQuery);

        var results = new List<SearchResult>();

        foreach (var note in notes)
        {
            if (results.Count >= maxResults) break;

            var isNameMatch = note.Name.Contains(query, comparison);
            var matchLines = FindMatchLines(note.Content, query, comparison);

            if (isNameMatch || matchLines.Any())
            {
                var result = new SearchResult
                {
                    Note = note,
                    IsNameMatch = isNameMatch,
                    MatchLines = matchLines,
                    Snippet = GenerateSnippet(note.Content, query, comparison, 100)
                };
                results.Add(result);
            }
        }

        return results;
    }

    public Task RebuildIndexAsync()
    {
        // sqlite-net-pcl 没有内置 FTS，无需重建索引
        return Task.CompletedTask;
    }

    private static string GenerateSnippet(string content, string query, StringComparison comparison, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // 查找搜索词位置
        var index = content.IndexOf(query, comparison);
        if (index < 0)
        {
            // 没找到，返回开头部分
            return content.Length <= maxLength 
                ? content 
                : content.Substring(0, maxLength) + "...";
        }

        // 计算摘要范围
        var start = Math.Max(0, index - maxLength / 3);
        var end = Math.Min(content.Length, index + query.Length + maxLength * 2 / 3);

        var snippet = content.Substring(start, end - start);
        if (start > 0) snippet = "..." + snippet;
        if (end < content.Length) snippet = snippet + "...";

        return snippet;
    }

    private static List<SearchMatchLine> FindMatchLines(string content, string query, StringComparison comparison)
    {
        var matches = new List<SearchMatchLine>();
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(query))
            return matches;

        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var matchPositions = new List<int>();
            int index = 0;
            while ((index = line.IndexOf(query, index, comparison)) != -1)
            {
                matchPositions.Add(index);
                index += query.Length;
            }

            if (matchPositions.Any())
            {
                matches.Add(new SearchMatchLine
                {
                    LineNumber = i + 1,
                    LineContent = line.Trim(),
                    MatchPositions = matchPositions
                });

                if (matches.Count >= 5) // 限制匹配行数
                    break;
            }
        }

        return matches;
    }
}
