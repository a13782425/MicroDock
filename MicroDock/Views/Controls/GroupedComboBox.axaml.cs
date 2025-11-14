using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using ReactiveUI;
using Serilog;
using System;
using System.Collections;
using System.Linq;
using System.Reactive;

namespace MicroDock.Views.Controls;

/// <summary>
/// 支持分组的组合框控件
/// 模仿标准 ComboBox 的外观和交互，但支持分组显示
/// </summary>
public partial class GroupedComboBox : UserControl
{
    /// <summary>
    /// 分组数据源（IEnumerable&lt;IGrouping&lt;string, T&gt;&gt;）
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> GroupedItemsSourceProperty =
        AvaloniaProperty.Register<GroupedComboBox, IEnumerable?>(
            nameof(GroupedItemsSource));

    public IEnumerable? GroupedItemsSource
    {
        get => GetValue(GroupedItemsSourceProperty);
        set => SetValue(GroupedItemsSourceProperty, value);
    }

    /// <summary>
    /// 选中的项
    /// </summary>
    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<GroupedComboBox, object?>(
            nameof(SelectedItem));

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// 显示文本（从选中项提取）
    /// </summary>
    public static readonly StyledProperty<string> DisplayTextProperty =
        AvaloniaProperty.Register<GroupedComboBox, string>(
            nameof(DisplayText),
            defaultValue: "请选择...");

    public string DisplayText
    {
        get => GetValue(DisplayTextProperty);
        private set => SetValue(DisplayTextProperty, value);
    }

    /// <summary>
    /// 显示成员路径（用于提取显示文本）
    /// </summary>
    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<GroupedComboBox, string?>(
            nameof(DisplayMemberPath));

    public string? DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    /// <summary>
    /// 占位符文本
    /// </summary>
    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<GroupedComboBox, string>(
            nameof(PlaceholderText),
            defaultValue: "请选择...");

    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// 弹出框最大高度
    /// </summary>
    public static readonly StyledProperty<double> MaxDropDownHeightProperty =
        AvaloniaProperty.Register<GroupedComboBox, double>(
            nameof(MaxDropDownHeight),
            defaultValue: 300.0);

    public double MaxDropDownHeight
    {
        get => GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    /// <summary>
    /// 项目数据模板
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<GroupedComboBox, IDataTemplate?>(
            nameof(ItemTemplate));

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// 是否支持分组折叠（默认 false）
    /// </summary>
    public static readonly StyledProperty<bool> IsGroupCollapsibleProperty =
        AvaloniaProperty.Register<GroupedComboBox, bool>(
            nameof(IsGroupCollapsible),
            defaultValue: false);

    public bool IsGroupCollapsible
    {
        get => GetValue(IsGroupCollapsibleProperty);
        set => SetValue(IsGroupCollapsibleProperty, value);
    }

    /// <summary>
    /// 默认分组是否展开（默认 true）
    /// </summary>
    public static readonly StyledProperty<bool> DefaultGroupExpandedProperty =
        AvaloniaProperty.Register<GroupedComboBox, bool>(
            nameof(DefaultGroupExpanded),
            defaultValue: true);

    public bool DefaultGroupExpanded
    {
        get => GetValue(DefaultGroupExpandedProperty);
        set => SetValue(DefaultGroupExpandedProperty, value);
    }

    public ReactiveCommand<object?, Unit> ItemClickCommand { get; }

    private Popup? _popup;
    private ScrollViewer? _scrollViewer;

    public GroupedComboBox()
    {
        InitializeComponent();
        
        ItemClickCommand = ReactiveCommand.Create<object?>(OnItemClick);
        
        // 监听属性变化
        SelectedItemProperty.Changed.AddClassHandler<GroupedComboBox>(
            (sender, e) => sender.OnSelectedItemChanged(e));
        
        DisplayMemberPathProperty.Changed.AddClassHandler<GroupedComboBox>(
            (sender, e) => sender.UpdateDisplayText());
        
        PlaceholderTextProperty.Changed.AddClassHandler<GroupedComboBox>(
            (sender, e) => sender.UpdateDisplayText());
        
        // 在控件加载后获取 Popup 引用
        this.AttachedToVisualTree += (s, e) =>
        {
            _popup = this.FindControl<Popup>("PART_Popup");
            _scrollViewer = this.FindControl<ScrollViewer>("PART_ScrollViewer");
            
            // 阻止滚动事件冒泡
            if (_scrollViewer != null)
            {
                _scrollViewer.PointerWheelChanged += OnScrollViewerPointerWheelChanged;
            }
            
            // 调试日志
            int itemCount = 0;
            if (GroupedItemsSource != null)
            {
                try
                {
                    itemCount = GroupedItemsSource.Cast<object>().Count();
                }
                catch { }
            }
            Log.Information("GroupedComboBox 已加载，ItemsSource 项数: {Count}", itemCount);
        };

        // 在控件从视觉树移除时清理事件订阅
        this.DetachedFromVisualTree += (s, e) =>
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.PointerWheelChanged -= OnScrollViewerPointerWheelChanged;
            }
        };
        
        // 初始化显示文本
        UpdateDisplayText();
    }

    private void OnDropDownButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_popup != null)
        {
            _popup.IsOpen = !_popup.IsOpen;
        }
    }

    private void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        UpdateDisplayText();
    }

    private void UpdateDisplayText()
    {
        if (SelectedItem == null)
        {
            DisplayText = PlaceholderText;
            return;
        }

        // 根据 DisplayMemberPath 提取显示文本
        if (!string.IsNullOrEmpty(DisplayMemberPath))
        {
            Type itemType = SelectedItem.GetType();
            System.Reflection.PropertyInfo? prop = itemType.GetProperty(DisplayMemberPath);
            if (prop != null)
            {
                object? value = prop.GetValue(SelectedItem);
                DisplayText = value?.ToString() ?? PlaceholderText;
                return;
            }
        }

        // 如果没有指定 DisplayMemberPath 或找不到属性，直接使用 ToString
        DisplayText = SelectedItem.ToString() ?? PlaceholderText;
    }

    /// <summary>
    /// 项目被点击时调用
    /// </summary>
    /// <param name="item">被点击的项目</param>
    private void OnItemClick(object? item)
    {
        if (item == null) return;
        
        SelectedItem = item;
        
        // 关闭弹出框
        if (_popup != null)
        {
            _popup.IsOpen = false;
        }
    }

    /// <summary>
    /// 处理 ScrollViewer 的滚轮事件，阻止事件冒泡
    /// </summary>
    private void OnScrollViewerPointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        if (_scrollViewer == null) return;

        // 获取当前滚动位置
        double currentOffset = _scrollViewer.Offset.Y;
        double maxOffset = _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height;

        // 判断是否可以继续滚动
        bool canScrollUp = currentOffset > 0;
        bool canScrollDown = currentOffset < maxOffset;

        // 判断滚动方向
        bool isScrollingUp = e.Delta.Y > 0;
        bool isScrollingDown = e.Delta.Y < 0;

        // 如果可以在当前方向滚动，则阻止事件冒泡
        if ((isScrollingUp && canScrollUp) || (isScrollingDown && canScrollDown))
        {
            e.Handled = true;
        }
    }
}
