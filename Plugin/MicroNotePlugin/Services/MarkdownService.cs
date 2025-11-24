using Markdig;

namespace MicroNotePlugin.Services;

/// <summary>
/// 提供 Markdown 渲染为 HTML 的功能。
/// </summary>
public static class MarkdownService
{
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// 将 Markdown 文本渲染为 HTML。
    /// </summary>
    /// <param name="markdown">Markdown 内容</param>
    /// <returns>HTML 字符串</returns>
    public static string RenderHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;
        return Markdown.ToHtml(markdown, _pipeline);
    }
}
