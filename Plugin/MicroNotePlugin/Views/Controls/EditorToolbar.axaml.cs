using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MicroNotePlugin.Views.Controls;

public partial class EditorToolbar : UserControl
{
    public EditorToolbar()
    {
        AvaloniaXamlLoader.Load(this);
        this.Loaded += OnLoaded;
    }

    #region 事件

    /// <summary>加粗</summary>
    public event EventHandler? BoldClicked;
    /// <summary>斜体</summary>
    public event EventHandler? ItalicClicked;
    /// <summary>删除线</summary>
    public event EventHandler? StrikethroughClicked;
    /// <summary>H1标题</summary>
    public event EventHandler? H1Clicked;
    /// <summary>H2标题</summary>
    public event EventHandler? H2Clicked;
    /// <summary>H3标题</summary>
    public event EventHandler? H3Clicked;
    /// <summary>链接</summary>
    public event EventHandler? LinkClicked;
    /// <summary>图片</summary>
    public event EventHandler? ImageClicked;
    /// <summary>代码块</summary>
    public event EventHandler? CodeBlockClicked;
    /// <summary>行内代码</summary>
    public event EventHandler? InlineCodeClicked;
    /// <summary>无序列表</summary>
    public event EventHandler? BulletListClicked;
    /// <summary>有序列表</summary>
    public event EventHandler? NumberedListClicked;
    /// <summary>任务列表</summary>
    public event EventHandler? TaskListClicked;
    /// <summary>引用块</summary>
    public event EventHandler? QuoteClicked;
    /// <summary>分隔线</summary>
    public event EventHandler? HorizontalRuleClicked;
    /// <summary>切换到编辑模式</summary>
    public event EventHandler? EditModeClicked;
    /// <summary>切换到分屏模式</summary>
    public event EventHandler? SplitModeClicked;
    /// <summary>切换到预览模式</summary>
    public event EventHandler? PreviewModeClicked;

    #endregion

    private ViewModeState _viewMode = ViewModeState.Edit;

    public enum ViewModeState { Edit, Split, Preview }

    private bool _isEditMode = true;

    /// <summary>
    /// 当前是否为编辑模式
    /// </summary>
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            _isEditMode = value;
            UpdateModeButtons();
        }
    }

    /// <summary>
    /// 当前视图模式
    /// </summary>
    public ViewModeState ViewMode
    {
        get => _viewMode;
        set
        {
            _viewMode = value;
            _isEditMode = value != ViewModeState.Preview;
            UpdateModeButtons();
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        BindButtonEvents();
        UpdateModeButtons();
    }

    private void BindButtonEvents()
    {
        BindButton("BoldButton", () => BoldClicked?.Invoke(this, EventArgs.Empty));
        BindButton("ItalicButton", () => ItalicClicked?.Invoke(this, EventArgs.Empty));
        BindButton("StrikethroughButton", () => StrikethroughClicked?.Invoke(this, EventArgs.Empty));
        BindButton("H1Button", () => H1Clicked?.Invoke(this, EventArgs.Empty));
        BindButton("H2Button", () => H2Clicked?.Invoke(this, EventArgs.Empty));
        BindButton("H3Button", () => H3Clicked?.Invoke(this, EventArgs.Empty));
        BindButton("LinkButton", () => LinkClicked?.Invoke(this, EventArgs.Empty));
        BindButton("ImageButton", () => ImageClicked?.Invoke(this, EventArgs.Empty));
        BindButton("CodeBlockButton", () => CodeBlockClicked?.Invoke(this, EventArgs.Empty));
        BindButton("InlineCodeButton", () => InlineCodeClicked?.Invoke(this, EventArgs.Empty));
        BindButton("BulletListButton", () => BulletListClicked?.Invoke(this, EventArgs.Empty));
        BindButton("NumberedListButton", () => NumberedListClicked?.Invoke(this, EventArgs.Empty));
        BindButton("TaskListButton", () => TaskListClicked?.Invoke(this, EventArgs.Empty));
        BindButton("QuoteButton", () => QuoteClicked?.Invoke(this, EventArgs.Empty));
        BindButton("HorizontalRuleButton", () => HorizontalRuleClicked?.Invoke(this, EventArgs.Empty));
        
        BindButton("EditModeButton", () =>
        {
            ViewMode = ViewModeState.Edit;
            EditModeClicked?.Invoke(this, EventArgs.Empty);
        });

        BindButton("SplitModeButton", () =>
        {
            ViewMode = ViewModeState.Split;
            SplitModeClicked?.Invoke(this, EventArgs.Empty);
        });
        
        BindButton("PreviewModeButton", () =>
        {
            ViewMode = ViewModeState.Preview;
            PreviewModeClicked?.Invoke(this, EventArgs.Empty);
        });
    }

    private void BindButton(string name, Action action)
    {
        var button = this.FindControl<Button>(name);
        if (button != null)
        {
            button.Click += (_, _) => action();
        }
    }

    private void UpdateModeButtons()
    {
        var editButton = this.FindControl<Button>("EditModeButton");
        var splitButton = this.FindControl<Button>("SplitModeButton");
        var previewButton = this.FindControl<Button>("PreviewModeButton");

        editButton?.Classes.Remove("mode-toggle");
        splitButton?.Classes.Remove("mode-toggle");
        previewButton?.Classes.Remove("mode-toggle");

        switch (_viewMode)
        {
            case ViewModeState.Edit:
                editButton?.Classes.Add("mode-toggle");
                break;
            case ViewModeState.Split:
                splitButton?.Classes.Add("mode-toggle");
                break;
            case ViewModeState.Preview:
                previewButton?.Classes.Add("mode-toggle");
                break;
        }
    }

    /// <summary>
    /// 设置格式按钮的可用状态（预览模式时禁用）
    /// </summary>
    public void SetFormattingEnabled(bool enabled)
    {
        var buttonNames = new[]
        {
            "BoldButton", "ItalicButton", "StrikethroughButton",
            "H1Button", "H2Button", "H3Button",
            "LinkButton", "ImageButton",
            "CodeBlockButton", "InlineCodeButton",
            "BulletListButton", "NumberedListButton", "TaskListButton",
            "QuoteButton", "HorizontalRuleButton"
        };

        foreach (var name in buttonNames)
        {
            var button = this.FindControl<Button>(name);
            if (button != null)
            {
                button.IsEnabled = enabled;
            }
        }
    }
}

