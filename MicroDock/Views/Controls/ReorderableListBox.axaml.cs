using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace MicroDock.Views.Controls;

/// <summary>
/// 一个支持拖拽排序的 ListBox 控件。
/// 实现了类似 Unity ReorderableList 的交互体验：
/// 1. 实时排序：拖拽过程中列表会实时重排。
/// 2. 替身机制：拖拽时使用悬浮的替身（Ghost），解决传统拖拽的中断问题。
/// 3. 平滑过渡：通过 RenderTransform 和 Transitions 提供丝滑的视觉反馈。
/// </summary>
/// <example>
/// <code>
/// <controls:ReorderableListBox ItemsSource="{Binding Items}"
///                              OrderChangedCommand="{Binding SaveOrderCommand}"
///                              ItemMovedCommand="{Binding PlaySoundCommand}">
///     <controls:ReorderableListBox.ItemTemplate>
///         <DataTemplate>
///             <TextBlock Text="{Binding Name}" />
///         </DataTemplate>
///     </controls:ReorderableListBox.ItemTemplate>
/// </controls:ReorderableListBox>
/// </code>
/// </example>
public partial class ReorderableListBox : UserControl
{
    /// <summary>
    /// 数据源集合 (System.Collections.IEnumerable)
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ReorderableListBox, IEnumerable?>(nameof(ItemsSource));

    /// <summary>
    /// 获取或设置列表的数据源
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// 列表项模板 (Avalonia.Controls.Templates.IDataTemplate)
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<ReorderableListBox, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>
    /// 获取或设置列表项的显示模板
    /// </summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// 排序完成命令 (System.Windows.Input.ICommand)
    /// </summary>
    public static readonly StyledProperty<ICommand?> OrderChangedCommandProperty =
        AvaloniaProperty.Register<ReorderableListBox, ICommand?>(nameof(OrderChangedCommand));

    /// <summary>
    /// 获取或设置在拖拽排序结束时（松手时）触发的命令，通常用于持久化保存新的顺序。
    /// </summary>
    public ICommand? OrderChangedCommand
    {
        get => GetValue(OrderChangedCommandProperty);
        set => SetValue(OrderChangedCommandProperty, value);
    }

    /// <summary>
    /// 列表项移动命令 (System.Windows.Input.ICommand)
    /// </summary>
    public static readonly StyledProperty<ICommand?> ItemMovedCommandProperty =
        AvaloniaProperty.Register<ReorderableListBox, ICommand?>(nameof(ItemMovedCommand));

    /// <summary>
    /// 获取或设置在每次列表项发生交换时触发的命令。
    /// 可用于播放音效或触觉反馈。
    /// </summary>
    public ICommand? ItemMovedCommand
    {
        get => GetValue(ItemMovedCommandProperty);
        set => SetValue(ItemMovedCommandProperty, value);
    }
    
    /// <summary>
    /// 拖拽手柄是否显示在起始位置 (bool)
    /// </summary>
    public static readonly StyledProperty<bool> IsDragHandleAtStartProperty =
        AvaloniaProperty.Register<ReorderableListBox, bool>(nameof(IsDragHandleAtStart), defaultValue: false);

    /// <summary>
    /// 获取或设置拖拽手柄的位置。
    /// True: 手柄在左侧（行首）；False: 手柄在右侧（行尾，默认）
    /// </summary>
    public bool IsDragHandleAtStart
    {
        get => GetValue(IsDragHandleAtStartProperty);
        set => SetValue(IsDragHandleAtStartProperty, value);
    }

    // Drag State
    private bool _isDragging;
    private Border? _originalContainer;
    private Border? _ghostContainer;
    private object? _draggedItem;
    private Point _dragOffset; // Offset from pointer to top-left of ghost
    private IDisposable? _captureSubscription;

    public ReorderableListBox()
    {
        InitializeComponent();
        
        // Global pointer events for drag handling
        // We attach these to the UserControl itself to ensure we catch moves even if we drift off the item
        AddHandler(PointerMovedEvent, OnRootPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnRootPointerReleased, RoutingStrategies.Tunnel);
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext == null) return;

        // Check inputs
        var source = e.Source as Control;
        if (source is TextBox || source is CheckBox || source is Button) return;

        var pointer = e.GetCurrentPoint(this);
        if (pointer.Properties.IsLeftButtonPressed)
        {
            _originalContainer = border;
            _draggedItem = border.DataContext;
            
            // Calculate offset: Vector from Pointer to Item TopLeft
            var pointerPos = e.GetPosition(this);
            var itemPos = border.TranslatePoint(new Point(0, 0), this) ?? new Point(0,0);
            _dragOffset = pointerPos - itemPos;

            // Start Drag
            StartDragging();
            
            // Capture pointer on the Root Control to handle events even if ItemsControl rebuilds
            e.Pointer.Capture(this);
            
            e.Handled = true;
        }
    }

    private void StartDragging()
    {
        if (_originalContainer == null || _draggedItem == null) return;
        _isDragging = true;

        // Create Ghost
        CreateGhost(_originalContainer);

        // Hide original (Placeholder effect)
        _originalContainer.Opacity = 0.0;
    }

    private void CreateGhost(Border original)
    {
        // Create a visual clone for the ghost
        // Since we can't easily clone the visual tree deep copy, we can:
        // 1. Use a VisualBrush (Efficient, but interaction issues? Ghost doesn't need interaction)
        // 2. Re-instantiate the ItemTemplate (Cleanest)
        
        // Let's try recreating the template
        var ghostContent = new ContentPresenter
        {
            Content = _draggedItem,
            ContentTemplate = ItemTemplate
        };

        _ghostContainer = new Border
        {
            Background = original.Background,
            BorderBrush = original.BorderBrush,
            BorderThickness = original.BorderThickness,
            CornerRadius = original.CornerRadius,
            Padding = original.Padding,
            Width = original.Bounds.Width,
            Height = original.Bounds.Height,
            // Ghost styling
            Opacity = 0.8,
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 10, Color = Color.Parse("#40000000"), OffsetY = 4
            }),
            IsHitTestVisible = false // Important: Ghost must pass clicks through
        };
        
        // Reconstruct the layout inside ghost to look like the item
        // Since our ItemTemplate is simple, we can just mimic the Grid structure if needed.
        // But simpler: just use the content. 
        // Note: The Handle is part of our Control's template, NOT the user's ItemTemplate.
        // So we need to recreate that structure too if we want the ghost to look identical.
        
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        
        // Handle Start
        if (IsDragHandleAtStart)
        {
             grid.Children.Add(CreateHandleBorder(0));
        }
        
        // Content
        Grid.SetColumn(ghostContent, 1);
        ghostContent.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        grid.Children.Add(ghostContent);
        
        // Handle End
        if (!IsDragHandleAtStart)
        {
             grid.Children.Add(CreateHandleBorder(2));
        }
        
        _ghostContainer.Child = grid;

        // Add to Canvas
        PART_DragLayer.Children.Add(_ghostContainer);
        
        // Position it initially matching the original
        UpdateGhostPosition(GetPointerPosOnRoot());
    }

    private Border CreateHandleBorder(int col)
    {
        var b = new Border
        {
            Background = Brushes.Transparent,
            Padding = new Thickness(8, 0)
        };
        var t = new TextBlock
        {
            Text = "≡",
            FontSize = 16,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Foreground = Application.Current?.FindResource("TextFillColorSecondaryBrush") as IBrush 
                         ?? Brushes.Gray
        };
        b.Child = t;
        Grid.SetColumn(b, col);
        return b;
    }
    
    private Point GetPointerPosOnRoot()
    {
        // We need current pointer position. Since we are in a UserControl,
        // we can track it via OnRootPointerMoved, or just rely on the event args passed there.
        // For CreateGhost, we assume we are called from Pressed, so we have position.
        // But wait, StartDragging is called from Pressed which calculated _dragOffset.
        // We need to know WHERE to put the ghost.
        // We'll use the position of the OriginalContainer relative to Root.
        
        return _originalContainer!.TranslatePoint(new Point(0,0), this) ?? new Point(0,0);
    }
    
    // Update ghost position based on pointer
    private void UpdateGhostPosition(Point pointerPos)
    {
        if (_ghostContainer == null) return;

        double left = pointerPos.X - _dragOffset.X;
        double top = pointerPos.Y - _dragOffset.Y;

        Canvas.SetLeft(_ghostContainer, left);
        Canvas.SetTop(_ghostContainer, top);
    }

    private void OnRootPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _ghostContainer == null) return;

        var pointerPos = e.GetPosition(this);
        UpdateGhostPosition(pointerPos);
        
        CheckSwap(pointerPos.Y);
    }

    private void OnRootPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        StopDragging();
    }

    private void CheckSwap(double pointerY)
    {
        if (ItemsSource == null || _draggedItem == null) return;

        // Find index of dragged item in current list
        // Using Reflection because ItemsSource is IEnumerable
        // For ObservableCollection, we cast to IList usually works
        var list = ItemsSource as IList;
        if (list == null) return; // Can't reorder non-IList
        
        int currentIndex = list.IndexOf(_draggedItem);
        if (currentIndex < 0) return;

        // Find target index
        var panel = PART_ItemsControl.ItemsPanelRoot;
        if (panel == null) return;
        
        // Simple hit testing on the Y axis
        // We check immediate neighbors for stability
        
        // Check Prev
        if (currentIndex > 0)
        {
            // Note: Panel.Children might not map 1:1 to Indices if virtualization is on.
            // But assuming StackPanel without virtualization for settings tab.
            if (currentIndex - 1 < panel.Children.Count)
            {
                var prevContainer = panel.Children[currentIndex - 1];
                // Get center Y of prev container relative to Root
                var prevPos = prevContainer.TranslatePoint(new Point(0,0), this);
                if (prevPos.HasValue)
                {
                    double prevCenterY = prevPos.Value.Y + prevContainer.Bounds.Height / 2;
                    if (pointerY < prevCenterY)
                    {
                        MoveItem(currentIndex, currentIndex - 1);
                        return;
                    }
                }
            }
        }
        
        // Check Next
        if (currentIndex < list.Count - 1)
        {
            if (currentIndex + 1 < panel.Children.Count)
            {
                var nextContainer = panel.Children[currentIndex + 1];
                var nextPos = nextContainer.TranslatePoint(new Point(0,0), this);
                if (nextPos.HasValue)
                {
                    double nextCenterY = nextPos.Value.Y + nextContainer.Bounds.Height / 2;
                    if (pointerY > nextCenterY)
                    {
                        MoveItem(currentIndex, currentIndex + 1);
                        return;
                    }
                }
            }
        }
    }

    private void MoveItem(int oldIndex, int newIndex)
    {
        if (ItemsSource == null) return;
        
        // Perform Move
        // NOTE: When we move, ItemsControl will refresh.
        // _originalContainer might be detached! 
        // But _ghostContainer is in Overlay, so it persists.
        // We need to find the NEW container for the dragged item to set it as hidden placeholder.
        
        // 1. Move Data
        var listType = ItemsSource.GetType();
        var moveMethod = listType.GetMethod("Move", new[] { typeof(int), typeof(int) });
        if (moveMethod != null)
        {
            moveMethod.Invoke(ItemsSource, new object[] { oldIndex, newIndex });
        }
        else if (ItemsSource is IList list)
        {
             var item = list[oldIndex];
             list.RemoveAt(oldIndex);
             list.Insert(newIndex, item);
        }

        // Trigger ItemMoved Command (Real-time feedback)
        if (ItemMovedCommand?.CanExecute(null) == true)
        {
            ItemMovedCommand.Execute(null);
        }

        // 2. Update Placeholder Reference
        // We need to wait for layout update to find the new container
        Dispatcher.UIThread.Post(() =>
        {
            UpdatePlaceholder(newIndex);
        }, DispatcherPriority.Render);
    }

    private void UpdatePlaceholder(int index)
    {
        var panel = PART_ItemsControl.ItemsPanelRoot;
        if (panel == null || index >= panel.Children.Count) return;

        // Find the new container at the index
        // Since we just moved data, the container at 'index' should be our item
        // (assuming no virtualization recycling messing things up instantly)
        
        // Reset opacity of OLD container (if it still exists / is reused)
        // Actually, safer to iterate and ensure only OUR item is hidden
        
        // But since we don't track "our item" perfectly across rebuilds without binding...
        // Wait, the DataContext of the container at 'index' should be _draggedItem!
        
        for(int i=0; i<panel.Children.Count; i++)
        {
            var child = panel.Children[i] as Control;
            if(child == null) continue;
            
            // Find the wrapper Border inside the ItemTemplate if ItemsControl generated ContentPresenters
            // ItemsControl creates ContentPresenter usually.
            // Our ItemTemplate has a Border.
            // So child is ContentPresenter -> Child is Border.
            
            Border? border = null;
            if (child is ContentPresenter cp)
            {
                 // Check DataContext
                 if (ReferenceEquals(cp.DataContext, _draggedItem))
                 {
                      // This is our guy
                      // Dig down to find our Border
                      border = cp.GetVisualDescendants().OfType<Border>().FirstOrDefault(); // The root border
                 }
            }
            else if (ReferenceEquals(child.DataContext, _draggedItem))
            {
                 // In case panel children are direct
                 border = child as Border;
            }
            
            if (border != null)
            {
                if (ReferenceEquals(child.DataContext, _draggedItem))
                {
                    border.Opacity = 0.0;
                    _originalContainer = border; // Update reference
                }
                else
                {
                    border.Opacity = 1.0; // Ensure others are visible
                }
            }
        }
    }

    private void StopDragging()
    {
        if (!_isDragging) return;
        _isDragging = false;

        // Release Capture
        // e.Pointer.Capture(null) is implicit if we don't hold it? 
        // We should explicitly release if we captured on Root.
        // But we don't have the pointer instance here easily without args.
        // Usually release happens on Up automatically.

        // Destroy Ghost
        if (_ghostContainer != null)
        {
            PART_DragLayer.Children.Remove(_ghostContainer);
            _ghostContainer = null;
        }

        // Show Original
        if (_originalContainer != null)
        {
            _originalContainer.Opacity = 1.0;
        }
        
        // Iterate all to be safe (in case UpdatePlaceholder missed one)
        var panel = PART_ItemsControl.ItemsPanelRoot;
        if (panel != null)
        {
             foreach(var child in panel.Children)
             {
                 var border = child.GetVisualDescendants().OfType<Border>().FirstOrDefault();
                 if(border != null) border.Opacity = 1.0;
             }
        }

        _originalContainer = null;
        _draggedItem = null;
        
        // Commit
        if (OrderChangedCommand?.CanExecute(null) == true)
        {
            OrderChangedCommand.Execute(null);
        }
    }
}
