using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using MicroNotePlugin.Core.Interfaces;
using MicroNotePlugin.ViewModels;
using AvaloniaWebView;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MicroNotePlugin.Views.Controls;

public partial class MarkdownEditor : UserControl
{
    private MarkdownEditorViewModel? _viewModel;
    private TextEditor? _editor;
    private EditorToolbar? _toolbar;
    private WebView? _webView;
    private bool _isUpdatingContent;
    private bool _webViewLoaded;
    private IImageService? _imageService;

    public MarkdownEditor()
    {
        AvaloniaXamlLoader.Load(this);
        this.Loaded += OnLoaded;
    }

    /// <summary>
    /// 设置图片服务
    /// </summary>
    public void SetImageService(IImageService imageService)
    {
        _imageService = imageService;
    }

    /// <summary>
    /// 设置 ViewModel
    /// </summary>
    public void SetViewModel(MarkdownEditorViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        // 绑定内容变化
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // 如果编辑器已初始化且有内容，同步到编辑器
            if (_editor != null && !string.IsNullOrEmpty(_viewModel.MarkdownContent))
            {
                _isUpdatingContent = true;
                try
                {
                    _editor.Text = _viewModel.MarkdownContent;
                }
                finally
                {
                    _isUpdatingContent = false;
                }
            }

            // 初始化 WebView 内容
            if (_webView != null && !string.IsNullOrEmpty(_viewModel.HtmlContent))
            {
                UpdateWebViewContent();
            }
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _editor = this.FindControl<TextEditor>("Editor");
        _toolbar = this.FindControl<EditorToolbar>("Toolbar");
        _webView = this.FindControl<WebView>("PreviewWebView");

        if (_editor != null)
        {
            // 确保编辑器可编辑
            _editor.IsReadOnly = false;
            _editor.ShowLineNumbers = true;
            _editor.WordWrap = true;
            _editor.TextArea.RightClickMovesCaret = true;

            // 绑定文本变化事件
            _editor.TextChanged += OnEditorTextChanged;

            // 绑定快捷键
            _editor.KeyDown += OnEditorKeyDown;

            // 如果已有内容，同步到编辑器
            if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.MarkdownContent))
            {
                _editor.Text = _viewModel.MarkdownContent;
            }
        }

        if (_webView != null)
        {
            // 修正路径：应该是 config/web，不是 web
            var configPath = MicroNotePlugin.Instance?.Context?.ConfigPath ?? "";
            var webPath = Path.Combine(configPath, "web", "index.html");

            // 如果 config/web 不存在，尝试直接在插件目录下找
            if (!File.Exists(webPath))
            {
                // 尝试备选路径
                var pluginDir = Path.GetDirectoryName(typeof(MicroNotePlugin).Assembly.Location) ?? "";
                webPath = Path.Combine(pluginDir, "config", "web", "index.html");
            }

            System.Diagnostics.Debug.WriteLine($"[MicroNote] WebView path: {webPath}, Exists: {File.Exists(webPath)}");

            if (File.Exists(webPath))
            {
                _webView.Url = new Uri(webPath);
                _webViewLoaded = false;
                _webView.Loaded += _webView_Loaded;
                // 延迟发送内容，等待 WebView 加载完成
                // 增加延迟时间以确保 JS 已加载

            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MicroNote] WebView HTML file not found: {webPath}");
            }
        }

        if (_toolbar != null)
        {
            BindToolbarEvents();
        }
    }

    private void _webView_Loaded(object? sender, RoutedEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            _webViewLoaded = true;

            // WebView 加载完成后，如果已经有内容则立即更新
            // 这解决了笔记加载时 WebView 还未就绪导致预览不显示的问题
            if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.HtmlContent))
            {
                UpdateWebViewContent();
            }
        });
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MarkdownEditorViewModel.MarkdownContent))
        {
            UpdateEditorContent();
        }
        else if (e.PropertyName == nameof(MarkdownEditorViewModel.HtmlContent))
        {
            UpdateWebViewContent();
        }
        else if (e.PropertyName == nameof(MarkdownEditorViewModel.IsEditMode))
        {
            _toolbar?.SetFormattingEnabled(_viewModel?.IsEditMode ?? true);
            if (_toolbar != null)
            {
                _toolbar.IsEditMode = _viewModel?.IsEditMode ?? true;
            }
        }
    }

    private void UpdateWebViewContent()
    {
        if (_webView == null || _viewModel == null)
        {
            System.Diagnostics.Debug.WriteLine($"[MicroNote] UpdateWebViewContent: webView={_webView != null}, viewModel={_viewModel != null}");
            return;
        }

        if (!_webViewLoaded)
        {
            MicroNotePlugin.Instance?.Context?.LogWarning("[MicroNote] UpdateWebViewContent: WebView not loaded yet");
            return;
        }

        var html = _viewModel.HtmlContent;

        System.Diagnostics.Debug.WriteLine($"[MicroNote] UpdateWebViewContent: html length={html?.Length ?? 0}");

        // 替换图片路径
        if (_imageService != null && !string.IsNullOrEmpty(html))
        {
            var imagesRoot = _imageService.GetImagesRootPath();
            var rootUrl = new Uri(imagesRoot).AbsoluteUri;
            if (!rootUrl.EndsWith("/")) rootUrl += "/";

            html = Regex.Replace(html, "src=[\"']images/([^\"']+)[\"']", m =>
            {
                var relative = m.Groups[1].Value;
                return $"src=\"{rootUrl}{relative}\"";
            });
        }

        // 发送消息给 JS
        var jsonContent = JsonSerializer.Serialize(html ?? "");
        var script = $"if(typeof updateContent === 'function') {{ updateContent({jsonContent}); }} else {{ console.error('updateContent not defined'); }}";
        System.Diagnostics.Debug.WriteLine($"[MicroNote] Executing script: {script.Substring(0, Math.Min(100, script.Length))}...");
        _webView.ExecuteScriptAsync(script);
    }

    // ... (Rest of the file remains same)

    private void UpdateEditorContent()
    {
        if (_viewModel == null || _isUpdatingContent)
            return;

        _isUpdatingContent = true;
        try
        {
            if (_editor != null && _editor.Text != _viewModel.MarkdownContent)
            {
                _editor.Text = _viewModel.MarkdownContent;
            }
        }
        finally
        {
            _isUpdatingContent = false;
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_viewModel == null || _editor == null || _isUpdatingContent)
            return;

        _isUpdatingContent = true;
        try
        {
            _viewModel.MarkdownContent = _editor.Text;
        }
        finally
        {
            _isUpdatingContent = false;
        }
    }

    private async void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel == null || _editor == null)
            return;

        // 处理快捷键
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.B: // 加粗
                    InsertBold();
                    e.Handled = true;
                    break;
                case Key.I: // 斜体
                    InsertItalic();
                    e.Handled = true;
                    break;
                case Key.K: // 链接
                    InsertLink();
                    e.Handled = true;
                    break;
                case Key.S: // 保存
                    _ = _viewModel.SaveAsync();
                    e.Handled = true;
                    break;
                case Key.V: // 粘贴 - 检查是否有图片
                    var handled = await TryPasteImageAsync();
                    if (handled)
                    {
                        e.Handled = true;
                    }
                    // 如果没有图片，让系统处理默认粘贴
                    break;
            }
        }
    }

    /// <summary>
    /// 尝试粘贴图片
    /// </summary>
    private async Task<bool> TryPasteImageAsync()
    {
        if (_imageService == null || _viewModel == null || _editor == null)
            return false;

        try
        {
            // 获取剪贴板
            var topLevel = TopLevel.GetTopLevel(this);
            var clipboard = topLevel?.Clipboard;

            var image = await _imageService.SaveFromClipboardAsync(clipboard);
            if (image != null)
            {
                // 插入图片 Markdown 语法
                var markdown = _imageService.GetMarkdownSyntax(image);
                var caretOffset = _editor.CaretOffset;

                _viewModel.InsertText(markdown + "\n", caretOffset);
                UpdateEditorContent();
                _editor.CaretOffset = Math.Min(caretOffset + markdown.Length + 1, _editor.Text.Length);

                return true;
            }
        }
        catch
        {
            // 忽略错误，让系统处理默认粘贴
        }

        return false;
    }

    private void BindToolbarEvents()
    {
        if (_toolbar == null) return;

        _toolbar.BoldClicked += (_, _) => InsertBold();
        _toolbar.ItalicClicked += (_, _) => InsertItalic();
        _toolbar.StrikethroughClicked += (_, _) => InsertStrikethrough();
        _toolbar.H1Clicked += (_, _) => InsertHeading(1);
        _toolbar.H2Clicked += (_, _) => InsertHeading(2);
        _toolbar.H3Clicked += (_, _) => InsertHeading(3);
        _toolbar.LinkClicked += (_, _) => InsertLink();
        _toolbar.ImageClicked += (_, _) => InsertImage();
        _toolbar.CodeBlockClicked += (_, _) => InsertCodeBlock();
        _toolbar.InlineCodeClicked += (_, _) => InsertInlineCode();
        _toolbar.BulletListClicked += (_, _) => InsertBulletList();
        _toolbar.NumberedListClicked += (_, _) => InsertNumberedList();
        _toolbar.TaskListClicked += (_, _) => InsertTaskList();
        _toolbar.QuoteClicked += (_, _) => InsertQuote();
        _toolbar.HorizontalRuleClicked += (_, _) => InsertHorizontalRule();

        _toolbar.EditModeClicked += (_, _) => _viewModel?.SwitchToEditMode();
        _toolbar.SplitModeClicked += (_, _) => _viewModel?.SwitchToSplitMode();
        _toolbar.PreviewModeClicked += (_, _) => _viewModel?.SwitchToPreviewMode();
    }

    #region 格式化操作

    private void InsertBold()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertBold(
            _editor.SelectionStart,
            _editor.SelectionLength);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertItalic()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertItalic(
            _editor.SelectionStart,
            _editor.SelectionLength);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertStrikethrough()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertStrikethrough(
            _editor.SelectionStart,
            _editor.SelectionLength);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertHeading(int level)
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertHeading(level, _editor.CaretOffset);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertLink()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertLink(
            _editor.SelectionStart,
            _editor.SelectionLength);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertImage()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertImage(_editor.CaretOffset);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertCodeBlock()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertCodeBlock(_editor.CaretOffset);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertInlineCode()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertInlineCode(
            _editor.SelectionStart,
            _editor.SelectionLength);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertBulletList()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertBulletList(_editor.CaretOffset);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertNumberedList()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertNumberedList(_editor.CaretOffset);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertTaskList()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertTaskList(_editor.CaretOffset);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertQuote()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertQuote(_editor.CaretOffset);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    private void InsertHorizontalRule()
    {
        if (_viewModel == null || _editor == null) return;

        var (_, newCaret) = _viewModel.InsertHorizontalRule(_editor.CaretOffset);

        UpdateEditorContent();
        _editor.CaretOffset = Math.Min(newCaret, _editor.Text.Length);
    }

    #endregion
}
