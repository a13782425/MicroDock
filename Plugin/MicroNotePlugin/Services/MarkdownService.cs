using Markdig;

namespace MicroNotePlugin.Services;

/// <summary>
/// Markdown 渲染服务
/// </summary>
public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        // 配置 Markdig 管道
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .UseEmojiAndSmiley()
            .UseAutoLinks()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();
    }

    /// <summary>
    /// 将 Markdown 转换为 HTML
    /// </summary>
    public string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }
}
