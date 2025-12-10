using System.Reactive.Linq;
using ReactiveUI;
using MicroNotePlugin.Entities;
using MicroNotePlugin.Services;

namespace MicroNotePlugin.ViewModels;

/// <summary>
/// 编辑器视图模式
/// </summary>
public enum EditorViewMode
{
    /// <summary>仅编辑</summary>
    Edit,
    /// <summary>仅预览</summary>
    Preview,
    /// <summary>分屏（左编辑右预览）</summary>
    Split
}

/// <summary>
/// Markdown 编辑器 ViewModel
/// </summary>
public class MarkdownEditorViewModel : ReactiveObject, IDisposable
{
    private readonly INoteRepository _noteRepository;
    private readonly IVersionService _versionService;
    private readonly MarkdownService _markdownService;
    private readonly IDisposable _autoSaveSubscription;

    private string _currentNoteId = string.Empty;
    private string _markdownContent = string.Empty;
    private string _htmlContent = string.Empty;
    private bool _isEditMode = true;
    private bool _isDirty;
    private bool _hasFile;
    private string _fileName = string.Empty;
    private EditorViewMode _viewMode = EditorViewMode.Edit;
    private string _originalContent = string.Empty;

    public MarkdownEditorViewModel(
        INoteRepository noteRepository, 
        IVersionService versionService,
        MarkdownService markdownService)
    {
        _noteRepository = noteRepository;
        _versionService = versionService;
        _markdownService = markdownService;

        // 设置自动保存（防抖 3秒）
        _autoSaveSubscription = this.WhenAnyValue(x => x.MarkdownContent)
            .Throttle(TimeSpan.FromSeconds(3))
            .Where(_ => IsDirty && !string.IsNullOrEmpty(CurrentNoteId))
            .Subscribe(async _ => await SaveAsync());

        // 当内容变化时更新预览
        this.WhenAnyValue(x => x.MarkdownContent)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdatePreview());
    }

    #region Properties

    /// <summary>
    /// 当前笔记 ID
    /// </summary>
    public string CurrentNoteId
    {
        get => _currentNoteId;
        private set => this.RaiseAndSetIfChanged(ref _currentNoteId, value);
    }

    /// <summary>
    /// Markdown 原始内容
    /// </summary>
    public string MarkdownContent
    {
        get => _markdownContent;
        set
        {
            if (_markdownContent != value)
            {
                this.RaiseAndSetIfChanged(ref _markdownContent, value);
                IsDirty = _markdownContent != _originalContent;
            }
        }
    }

    /// <summary>
    /// 渲染后的 HTML 内容
    /// </summary>
    public string HtmlContent
    {
        get => _htmlContent;
        private set
        {
            _htmlContent = value;
            this.RaisePropertyChanged(nameof(HtmlContent));
            //this.RaiseAndSetIfChanged(ref _htmlContent, value);
        }
    }

    /// <summary>
    /// 是否为编辑模式
    /// </summary>
    public bool IsEditMode
    {
        get => _isEditMode;
        set => this.RaiseAndSetIfChanged(ref _isEditMode, value);
    }

    /// <summary>
    /// 当前视图模式
    /// </summary>
    public EditorViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _viewMode, value);
            IsEditMode = value != EditorViewMode.Preview;
            this.RaisePropertyChanged(nameof(ShowEditOnly));
            this.RaisePropertyChanged(nameof(ShowPreviewOnly));
            this.RaisePropertyChanged(nameof(ShowSplitView));
            this.RaisePropertyChanged(nameof(ShowEditor));
            this.RaisePropertyChanged(nameof(ShowPreview));
            this.RaisePropertyChanged(nameof(IsSplitMode));
        }
    }

    /// <summary>
    /// 是否仅显示编辑器
    /// </summary>
    public bool ShowEditOnly => ViewMode == EditorViewMode.Edit && HasFile;

    /// <summary>
    /// 是否仅显示预览
    /// </summary>
    public bool ShowPreviewOnly => ViewMode == EditorViewMode.Preview && HasFile;

    /// <summary>
    /// 是否显示分屏视图
    /// </summary>
    public bool ShowSplitView => ViewMode == EditorViewMode.Split && HasFile;

    /// <summary>
    /// 是否显示编辑器
    /// </summary>
    public bool ShowEditor => ViewMode == EditorViewMode.Edit || ViewMode == EditorViewMode.Split;

    /// <summary>
    /// 是否显示预览
    /// </summary>
    public bool ShowPreview => ViewMode == EditorViewMode.Preview || ViewMode == EditorViewMode.Split;

    /// <summary>
    /// 是否为分屏模式
    /// </summary>
    public bool IsSplitMode => ViewMode == EditorViewMode.Split;

    /// <summary>
    /// 是否有未保存的修改
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        private set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    /// <summary>
    /// 是否已加载文件
    /// </summary>
    public bool HasFile
    {
        get => _hasFile;
        private set
        {
            this.RaiseAndSetIfChanged(ref _hasFile, value);
            this.RaisePropertyChanged(nameof(ShowEditOnly));
            this.RaisePropertyChanged(nameof(ShowPreviewOnly));
            this.RaisePropertyChanged(nameof(ShowSplitView));
        }
    }

    /// <summary>
    /// 当前文件名
    /// </summary>
    public string FileName
    {
        get => _fileName;
        private set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 加载笔记
    /// </summary>
    public async Task LoadNoteAsync(string noteId)
    {
        if (IsDirty)
        {
            await SaveAsync();
        }

        var note = await _noteRepository.GetByIdAsync(noteId);
        if (note == null) return;

        CurrentNoteId = note.Id;
        FileName = note.Name;
        _originalContent = note.Content;
        _markdownContent = note.Content;
        this.RaisePropertyChanged(nameof(MarkdownContent));

        IsDirty = false;
        HasFile = true;

        UpdatePreview();
    }

    /// <summary>
    /// 通过节点加载文件
    /// </summary>
    public async Task LoadNoteAsync(FileNodeViewModel node)
    {
        if (!node.IsFile) return;
        await LoadNoteAsync(node.Id);
    }

    /// <summary>
    /// 保存笔记
    /// </summary>
    public async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(CurrentNoteId) || !IsDirty)
            return;

        var note = await _noteRepository.GetByIdAsync(CurrentNoteId);
        if (note == null) return;

        // 创建版本快照（如果内容有变化）
        if (note.Content != MarkdownContent)
        {
            await _versionService.CreateVersionAsync(note.Id, note.Content);
        }

        // 更新笔记
        note.Content = MarkdownContent;
        await _noteRepository.UpdateAsync(note);

        _originalContent = MarkdownContent;
        IsDirty = false;
    }

    /// <summary>
    /// 关闭当前文件
    /// </summary>
    public async Task CloseFileAsync()
    {
        if (IsDirty)
        {
            await SaveAsync();
        }

        CurrentNoteId = string.Empty;
        FileName = string.Empty;
        _originalContent = string.Empty;
        _markdownContent = string.Empty;
        this.RaisePropertyChanged(nameof(MarkdownContent));
        HtmlContent = string.Empty;
        HasFile = false;
        IsDirty = false;
    }

    /// <summary>
    /// 切换到编辑模式
    /// </summary>
    public void SwitchToEditMode()
    {
        ViewMode = EditorViewMode.Edit;
    }

    /// <summary>
    /// 切换到预览模式
    /// </summary>
    public void SwitchToPreviewMode()
    {
        ViewMode = EditorViewMode.Preview;
        UpdatePreview();
    }

    /// <summary>
    /// 切换到分屏模式
    /// </summary>
    public void SwitchToSplitMode()
    {
        ViewMode = EditorViewMode.Split;
        UpdatePreview();
    }

    /// <summary>
    /// 循环切换视图模式
    /// </summary>
    public void CycleViewMode()
    {
        ViewMode = ViewMode switch
        {
            EditorViewMode.Edit => EditorViewMode.Split,
            EditorViewMode.Split => EditorViewMode.Preview,
            EditorViewMode.Preview => EditorViewMode.Edit,
            _ => EditorViewMode.Edit
        };
    }

    #endregion

    #region Text Operations

    /// <summary>
    /// 在光标位置插入文本
    /// </summary>
    public string InsertText(string text, int caretIndex)
    {
        var newContent = MarkdownContent.Insert(caretIndex, text);
        MarkdownContent = newContent;
        return newContent;
    }

    /// <summary>
    /// 包装选中的文本
    /// </summary>
    public (string newContent, int newCaretIndex) WrapSelection(
        string prefix,
        string suffix,
        int selectionStart,
        int selectionLength)
    {
        var before = MarkdownContent.Substring(0, selectionStart);
        var selected = MarkdownContent.Substring(selectionStart, selectionLength);
        var after = MarkdownContent.Substring(selectionStart + selectionLength);

        var newContent = before + prefix + selected + suffix + after;
        MarkdownContent = newContent;

        var newCaretIndex = selectionStart + prefix.Length + selectionLength + suffix.Length;
        return (newContent, newCaretIndex);
    }

    /// <summary>
    /// 在行首添加前缀
    /// </summary>
    public (string newContent, int newCaretIndex) AddLinePrefix(string prefix, int caretIndex)
    {
        var lineStart = MarkdownContent.LastIndexOf('\n', Math.Max(0, caretIndex - 1)) + 1;

        var before = MarkdownContent.Substring(0, lineStart);
        var after = MarkdownContent.Substring(lineStart);

        var newContent = before + prefix + after;
        MarkdownContent = newContent;

        return (newContent, caretIndex + prefix.Length);
    }

    /// <summary>
    /// 插入加粗标记
    /// </summary>
    public (string, int) InsertBold(int selectionStart, int selectionLength)
    {
        if (selectionLength > 0)
        {
            return WrapSelection("**", "**", selectionStart, selectionLength);
        }
        else
        {
            var newContent = InsertText("****", selectionStart);
            return (newContent, selectionStart + 2);
        }
    }

    /// <summary>
    /// 插入斜体标记
    /// </summary>
    public (string, int) InsertItalic(int selectionStart, int selectionLength)
    {
        if (selectionLength > 0)
        {
            return WrapSelection("*", "*", selectionStart, selectionLength);
        }
        else
        {
            var newContent = InsertText("**", selectionStart);
            return (newContent, selectionStart + 1);
        }
    }

    /// <summary>
    /// 插入删除线标记
    /// </summary>
    public (string, int) InsertStrikethrough(int selectionStart, int selectionLength)
    {
        if (selectionLength > 0)
        {
            return WrapSelection("~~", "~~", selectionStart, selectionLength);
        }
        else
        {
            var newContent = InsertText("~~~~", selectionStart);
            return (newContent, selectionStart + 2);
        }
    }

    /// <summary>
    /// 插入标题
    /// </summary>
    public (string, int) InsertHeading(int level, int caretIndex)
    {
        var prefix = new string('#', level) + " ";
        return AddLinePrefix(prefix, caretIndex);
    }

    /// <summary>
    /// 插入链接
    /// </summary>
    public (string, int) InsertLink(int selectionStart, int selectionLength)
    {
        if (selectionLength > 0)
        {
            var selected = MarkdownContent.Substring(selectionStart, selectionLength);
            var linkText = $"[{selected}](url)";
            var before = MarkdownContent.Substring(0, selectionStart);
            var after = MarkdownContent.Substring(selectionStart + selectionLength);
            var newContent = before + linkText + after;
            MarkdownContent = newContent;
            return (newContent, selectionStart + selected.Length + 3);
        }
        else
        {
            var linkText = "[链接文本](url)";
            var newContent = InsertText(linkText, selectionStart);
            return (newContent, selectionStart + 1);
        }
    }

    /// <summary>
    /// 插入图片
    /// </summary>
    public (string, int) InsertImage(int caretIndex)
    {
        var imageText = "![图片描述](图片地址)";
        var newContent = InsertText(imageText, caretIndex);
        return (newContent, caretIndex + 2);
    }

    /// <summary>
    /// 插入代码块
    /// </summary>
    public (string, int) InsertCodeBlock(int caretIndex)
    {
        var codeBlock = "\n```\n\n```\n";
        var newContent = InsertText(codeBlock, caretIndex);
        return (newContent, caretIndex + 5);
    }

    /// <summary>
    /// 插入行内代码
    /// </summary>
    public (string, int) InsertInlineCode(int selectionStart, int selectionLength)
    {
        if (selectionLength > 0)
        {
            return WrapSelection("`", "`", selectionStart, selectionLength);
        }
        else
        {
            var newContent = InsertText("``", selectionStart);
            return (newContent, selectionStart + 1);
        }
    }

    /// <summary>
    /// 插入无序列表
    /// </summary>
    public (string, int) InsertBulletList(int caretIndex)
    {
        return AddLinePrefix("- ", caretIndex);
    }

    /// <summary>
    /// 插入有序列表
    /// </summary>
    public (string, int) InsertNumberedList(int caretIndex)
    {
        return AddLinePrefix("1. ", caretIndex);
    }

    /// <summary>
    /// 插入任务列表
    /// </summary>
    public (string, int) InsertTaskList(int caretIndex)
    {
        return AddLinePrefix("- [ ] ", caretIndex);
    }

    /// <summary>
    /// 插入引用块
    /// </summary>
    public (string, int) InsertQuote(int caretIndex)
    {
        return AddLinePrefix("> ", caretIndex);
    }

    /// <summary>
    /// 插入分隔线
    /// </summary>
    public (string, int) InsertHorizontalRule(int caretIndex)
    {
        var rule = "\n---\n";
        var newContent = InsertText(rule, caretIndex);
        return (newContent, caretIndex + rule.Length);
    }

    /// <summary>
    /// 插入图片 Markdown 语法
    /// </summary>
    public (string, int) InsertImageMarkdown(string imagePath, int caretIndex, string? altText = null)
    {
        var alt = altText ?? "图片";
        var imageText = $"![{alt}]({imagePath})";
        var newContent = InsertText(imageText, caretIndex);
        return (newContent, caretIndex + imageText.Length);
    }

    #endregion

    #region Private Methods

    private void UpdatePreview()
    {
        HtmlContent = _markdownService.ToHtml(MarkdownContent);
    }

    #endregion

    public void Dispose()
    {
        _autoSaveSubscription.Dispose();
    }
}
