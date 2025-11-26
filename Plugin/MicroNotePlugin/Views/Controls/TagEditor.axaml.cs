using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroNotePlugin.Models;
using MicroNotePlugin.Services;
using System.Collections.ObjectModel;

namespace MicroNotePlugin.Views.Controls;

public partial class TagEditor : UserControl
{
    private MetadataService? _metadataService;
    private string? _currentNotePath;
    private AutoCompleteBox? _tagInput;

    public TagEditor()
    {
        AvaloniaXamlLoader.Load(this);
        this.Loaded += OnLoaded;
    }

    /// <summary>
    /// 当前笔记的标签列表
    /// </summary>
    public ObservableCollection<TagDisplayItem> Tags { get; } = new();

    /// <summary>
    /// 标签变化事件
    /// </summary>
    public event EventHandler? TagsChanged;

    /// <summary>
    /// 设置服务和当前笔记
    /// </summary>
    public void Initialize(MetadataService metadataService, string? notePath)
    {
        _metadataService = metadataService;
        _currentNotePath = notePath;
        RefreshTags();
        UpdateAutoCompleteSource();
    }

    /// <summary>
    /// 更新当前笔记路径
    /// </summary>
    public void SetCurrentNote(string? notePath)
    {
        _currentNotePath = notePath;
        RefreshTags();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _tagInput = this.FindControl<AutoCompleteBox>("TagInput");
        var addButton = this.FindControl<Button>("AddTagButton");

        if (addButton != null)
        {
            addButton.Click += OnAddTagClick;
        }

        if (_tagInput != null)
        {
            _tagInput.KeyDown += OnTagInputKeyDown;
        }

        DataContext = this;
    }

    private void RefreshTags()
    {
        Tags.Clear();

        if (_metadataService == null || string.IsNullOrEmpty(_currentNotePath))
            return;

        var tagNames = _metadataService.GetNoteTags(_currentNotePath);
        foreach (var tagName in tagNames)
        {
            var definition = _metadataService.GetTagDefinition(tagName);
            Tags.Add(new TagDisplayItem
            {
                Name = tagName,
                Color = definition?.Color ?? "#808080"
            });
        }
    }

    private void UpdateAutoCompleteSource()
    {
        if (_tagInput == null || _metadataService == null)
            return;

        var allTags = _metadataService.GetAllTags();
        _tagInput.ItemsSource = allTags.Select(t => t.Name).ToList();
    }

    private void OnAddTagClick(object? sender, RoutedEventArgs e)
    {
        AddCurrentTag();
    }

    private void OnTagInputKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            AddCurrentTag();
            e.Handled = true;
        }
    }

    private void AddCurrentTag()
    {
        if (_tagInput == null || _metadataService == null || string.IsNullOrEmpty(_currentNotePath))
            return;

        var tagName = _tagInput.Text?.Trim();
        if (string.IsNullOrEmpty(tagName))
            return;

        // 添加标签
        _metadataService.AddTagToNote(_currentNotePath, tagName);
        
        // 刷新显示
        RefreshTags();
        UpdateAutoCompleteSource();
        
        // 清空输入
        _tagInput.Text = string.Empty;

        TagsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 移除标签命令
    /// </summary>
    public void RemoveTag(string tagName)
    {
        if (_metadataService == null || string.IsNullOrEmpty(_currentNotePath))
            return;

        _metadataService.RemoveTagFromNote(_currentNotePath, tagName);
        RefreshTags();
        TagsChanged?.Invoke(this, EventArgs.Empty);
    }

    // 用于绑定的命令
    public System.Windows.Input.ICommand RemoveTagCommand => new RelayCommand<string>(RemoveTag);
}

/// <summary>
/// 标签显示项
/// </summary>
public class TagDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#808080";
}

/// <summary>
/// 简单的命令实现
/// </summary>
public class RelayCommand<T> : System.Windows.Input.ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

