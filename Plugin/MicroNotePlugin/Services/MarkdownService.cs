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
        // 配置 Markdig 管道，启用常用扩展
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()      // 启用高级扩展（表格、任务列表等）
            .UseEmojiAndSmiley()          // 支持表情符号
            .UseAutoLinks()               // 自动识别链接
            .UseSoftlineBreakAsHardlineBreak() // 软换行作为硬换行
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

    /// <summary>
    /// 将 Markdown 转换为带样式的完整 HTML 文档
    /// </summary>
    public string ToStyledHtml(string markdown, bool isDarkTheme = false)
    {
        var html = ToHtml(markdown);
        return WrapWithStyles(html, isDarkTheme);
    }

    /// <summary>
    /// 包装 HTML 内容，添加样式
    /// </summary>
    private string WrapWithStyles(string htmlContent, bool isDarkTheme)
    {
        var backgroundColor = isDarkTheme ? "#1e1e1e" : "#ffffff";
        var textColor = isDarkTheme ? "#d4d4d4" : "#333333";
        var codeBackground = isDarkTheme ? "#2d2d2d" : "#f5f5f5";
        var borderColor = isDarkTheme ? "#3c3c3c" : "#e0e0e0";
        var linkColor = isDarkTheme ? "#569cd6" : "#0066cc";
        var quoteBackground = isDarkTheme ? "#252526" : "#f9f9f9";
        var quoteBorderColor = isDarkTheme ? "#4a4a4a" : "#ddd";

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            font-size: 14px;
            line-height: 1.6;
            color: {textColor};
            background-color: {backgroundColor};
            padding: 16px;
            margin: 0;
        }}
        
        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
        }}
        
        h1 {{ font-size: 2em; border-bottom: 1px solid {borderColor}; padding-bottom: 0.3em; }}
        h2 {{ font-size: 1.5em; border-bottom: 1px solid {borderColor}; padding-bottom: 0.3em; }}
        h3 {{ font-size: 1.25em; }}
        h4 {{ font-size: 1em; }}
        
        p {{ margin-top: 0; margin-bottom: 16px; }}
        
        a {{ color: {linkColor}; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
        
        code {{
            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
            font-size: 85%;
            background-color: {codeBackground};
            padding: 0.2em 0.4em;
            border-radius: 3px;
        }}
        
        pre {{
            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
            font-size: 85%;
            background-color: {codeBackground};
            padding: 16px;
            border-radius: 6px;
            overflow-x: auto;
            line-height: 1.45;
        }}
        
        pre code {{
            background-color: transparent;
            padding: 0;
        }}
        
        blockquote {{
            margin: 0 0 16px 0;
            padding: 0 16px;
            border-left: 4px solid {quoteBorderColor};
            background-color: {quoteBackground};
            color: {textColor};
        }}
        
        ul, ol {{
            margin-top: 0;
            margin-bottom: 16px;
            padding-left: 2em;
        }}
        
        li {{ margin-top: 0.25em; }}
        
        table {{
            border-collapse: collapse;
            width: 100%;
            margin-bottom: 16px;
        }}
        
        table th, table td {{
            border: 1px solid {borderColor};
            padding: 8px 12px;
        }}
        
        table th {{
            background-color: {codeBackground};
            font-weight: 600;
        }}
        
        hr {{
            height: 2px;
            background-color: {borderColor};
            border: none;
            margin: 24px 0;
        }}
        
        img {{
            max-width: 100%;
            height: auto;
        }}
        
        /* 任务列表样式 */
        .task-list-item {{
            list-style-type: none;
            margin-left: -1.5em;
        }}
        
        .task-list-item input[type=""checkbox""] {{
            margin-right: 0.5em;
        }}
    </style>
</head>
<body>
{htmlContent}
</body>
</html>";
    }

    /// <summary>
    /// 获取纯文本（去除 Markdown 语法）
    /// </summary>
    public string ToPlainText(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        var doc = Markdig.Markdown.Parse(markdown, _pipeline);
        return doc.ToString() ?? string.Empty;
    }
}
