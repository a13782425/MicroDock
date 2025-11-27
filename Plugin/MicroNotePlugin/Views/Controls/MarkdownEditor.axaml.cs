using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using MicroNotePlugin.Services;
using MicroNotePlugin.ViewModels;

namespace MicroNotePlugin.Views.Controls;

public partial class MarkdownEditor : UserControl
{
    private MarkdownEditorViewModel? _viewModel;
    private TextEditor? _editor;
    private EditorToolbar? _toolbar;
    private Control? _previewScroller;
    private bool _isUpdatingContent;
    private ImageService? _imageService;

    public MarkdownEditor()
    {
        AvaloniaXamlLoader.Load(this);
        this.Loaded += OnLoaded;
    }

    /// <summary>
    /// 设置图片服务
    /// </summary>
    public void SetImageService(ImageService imageService)
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
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _editor = this.FindControl<TextEditor>("Editor");
        _toolbar = this.FindControl<EditorToolbar>("Toolbar");
        _previewScroller = this.FindControl<Control>("PreviewScroller");

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

        if (_toolbar != null)
        {
            BindToolbarEvents();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MarkdownEditorViewModel.MarkdownContent))
        {
            UpdateEditorContent();
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
                    _viewModel.Save();
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
            
            var imagePath = await _imageService.SaveImageFromClipboardAsync(clipboard);
            if (imagePath != null)
            {
                // 插入图片 Markdown 语法
                var markdown = ImageService.GetMarkdownImageSyntax(imagePath);
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
