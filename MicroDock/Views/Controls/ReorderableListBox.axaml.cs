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
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Avalonia.Styling;

namespace MicroDock.Views.Controls;

/// <summary>
/// 一个支持拖拽排序的 ListBox 控件。
/// 实现了类似 Unity ReorderableList 的交互体验：
/// 1. 实时排序：拖拽过程中列表会实时重排。
/// 2. 替身机制：拖拽时使用悬浮的替身（Ghost），解决传统拖拽的中断问题。
/// 3. 平滑过渡：通过 RenderTransform 和 FLIP 动画提供丝滑的视觉反馈。
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
public partial class ReorderableListBox : UserControl, IDisposable
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

    /// <summary>
    /// 列表的最大高度 (double)
    /// </summary>
    public static readonly StyledProperty<double> MaxListBoxHeightProperty =
        AvaloniaProperty.Register<ReorderableListBox, double>(nameof(MaxListBoxHeight), defaultValue: double.PositiveInfinity);

    /// <summary>
    /// 获取或设置列表的最大高度。
    /// 当内容超过此高度时会自动显示垂直滚动条。
    /// 默认值为 double.PositiveInfinity (无限制)。
    /// </summary>
    public double MaxListBoxHeight
    {
        get => GetValue(MaxListBoxHeightProperty);
        set => SetValue(MaxListBoxHeightProperty, value);
    }

    /// <summary>
    /// 自动滚动边缘触发区域大小(像素) (double)
    /// </summary>
    public static readonly StyledProperty<double> AutoScrollEdgeSizeProperty =
        AvaloniaProperty.Register<ReorderableListBox, double>(nameof(AutoScrollEdgeSize), defaultValue: 50.0);

    /// <summary>
    /// 获取或设置拖拽时触发自动滚动的边缘区域大小(单位：像素)。
    /// 当鼠标进入距离 ScrollViewer 上下边缘此距离内时,会触发自动滚动。
    /// 默认值为 50 像素。
    /// </summary>
    public double AutoScrollEdgeSize
    {
        get => GetValue(AutoScrollEdgeSizeProperty);
        set => SetValue(AutoScrollEdgeSizeProperty, value);
    }

    /// <summary>
    /// 自动滚动速度(像素/帧) (double)
    /// </summary>
    public static readonly StyledProperty<double> AutoScrollSpeedProperty =
        AvaloniaProperty.Register<ReorderableListBox, double>(nameof(AutoScrollSpeed), defaultValue: 5.0);

    /// <summary>
    /// 获取或设置自动滚动的基础速度(单位：像素/帧)。
    /// 实际滚动速度会根据鼠标距离边缘的远近动态调整(0 到此值之间)。
    /// 默认值为 5.0 像素/帧。
    /// </summary>
    public double AutoScrollSpeed
    {
        get => GetValue(AutoScrollSpeedProperty);
        set => SetValue(AutoScrollSpeedProperty, value);
    }


    // Constants
    private const double GHOST_OPACITY = 0.8;
    private const double GHOST_SHADOW_BLUR = 10.0;
    private const int SWAP_CHECK_THROTTLE_MS = 16; // ~60fps
    private const double ANIMATION_DURATION_SECONDS = 0.3;
    private const double PLACEHOLDER_OPACITY = 0.0;
    private const double VISIBLE_OPACITY = 1.0;
    private const int MIN_BITMAP_SIZE = 1;
    
    // Drag State
    private bool _isDragging;
    private Border? _originalContainer;
    private Border? _ghostContainer;
    private ContentPresenter? _cachedGhostContentPresenter; // Cached for reuse
    private Border? _cachedGhostInnerBorder; // Cached for reuse
    private object? _draggedItem;
    private Point _dragOffset; // Offset from pointer to top-left of ghost
    private IPointer? _capturedPointer;
    private HashSet<Control> _animatingControls = new();
    private Dictionary<Control, System.Threading.CancellationTokenSource> _animationCancellations = new();
    
    // Container Cache
    private Dictionary<object, Border> _containerCache = new();
    private bool _cacheNeedsUpdate = true;
    
    // Throttle for CheckSwap
    private DateTime _lastSwapCheck = DateTime.MinValue;
    
    // Auto-scroll State
    private DispatcherTimer? _autoScrollTimer;
    private double _autoScrollVelocity = 0; // Positive = scroll down, Negative = scroll up
    
    // Layout Update Handler
    private EventHandler? _pendingLayoutHandler;

    public ReorderableListBox()
    {
        InitializeComponent();
        
        // Global pointer events for drag handling
        // We attach these to the UserControl itself to ensure we catch moves even if we drift off the item
        AddHandler(PointerMovedEvent, OnRootPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnRootPointerReleased, RoutingStrategies.Tunnel);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            _cacheNeedsUpdate = true;
            
            // Subscribe to collection changes if INotifyCollectionChanged
            if (change.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= OnCollectionChanged;
            }
            if (change.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += OnCollectionChanged;
            }
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _cacheNeedsUpdate = true;
    }

    private void RebuildContainerCache()
    {
        _containerCache.Clear();
        var panel = PART_ItemsControl.ItemsPanelRoot;
        if (panel == null) return;

        foreach (var child in panel.Children)
        {
            if (child is Control c && c.DataContext != null)
            {
                Border? border = null;
                if (child is ContentPresenter cp)
                {
                    border = cp.GetVisualDescendants().OfType<Border>().FirstOrDefault();
                }
                else if (child is Border b)
                {
                    border = b;
                }

                if (border != null)
                {
                    _containerCache[c.DataContext] = border;
                }
            }
        }
        _cacheNeedsUpdate = false;
    }

    private Border? GetContainerForItem(object item)
    {
        if (_cacheNeedsUpdate)
        {
            RebuildContainerCache();
        }
        return _containerCache.TryGetValue(item, out var container) ? container : null;
    }

    /// <summary>
    /// 检查指定的 DataContext 是否为当前正在拖拽的项
    /// </summary>
    private bool IsDraggedItem(object? dataContext)
    {
        return _draggedItem != null && ReferenceEquals(dataContext, _draggedItem);
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
            _capturedPointer = e.Pointer;
            _capturedPointer.Capture(this);
            
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
        _originalContainer.Opacity = PLACEHOLDER_OPACITY;
    }

    private void CreateGhost(Border original)
    {
        // Validate bounds to prevent crashes with invalid dimensions
        var bounds = original.Bounds;
        if (bounds.Width < MIN_BITMAP_SIZE || bounds.Height < MIN_BITMAP_SIZE)
        {
            return; // Cannot create ghost for items with invalid size
        }
        
        // Reuse cached visual elements for better performance
        // Only create new elements if they don't exist yet
        if (_cachedGhostContentPresenter == null)
        {
            _cachedGhostContentPresenter = new ContentPresenter
            {
                ContentTemplate = ItemTemplate
            };
        }
        
        if (_cachedGhostInnerBorder == null)
        {
            _cachedGhostInnerBorder = new Border
            {
                Background = Brushes.Transparent,
                Child = _cachedGhostContentPresenter
            };
        }
        
        // Update the content and dimensions for current drag
        _cachedGhostContentPresenter.Content = _draggedItem;
        _cachedGhostContentPresenter.Width = bounds.Width;
        _cachedGhostContentPresenter.Height = bounds.Height;
        
        _cachedGhostInnerBorder.CornerRadius = original.CornerRadius;
        _cachedGhostInnerBorder.Width = bounds.Width;
        _cachedGhostInnerBorder.Height = bounds.Height;

        // Create or reuse the outer ghost container
        if (_ghostContainer == null)
        {
            _ghostContainer = new Border
            {
                Child = _cachedGhostInnerBorder,
                Opacity = GHOST_OPACITY,
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    Blur = GHOST_SHADOW_BLUR, 
                    Color = Color.Parse("#40000000"), 
                    OffsetY = 4
                }),
                IsHitTestVisible = false // Important: Ghost must pass clicks through
            };
        }
        else
        {
            // If ghost container exists, just ensure it has the right child
            _ghostContainer.Child = _cachedGhostInnerBorder;
        }

        // Add to Canvas only if not already present
        if (!PART_DragLayer.Children.Contains(_ghostContainer))
        {
            PART_DragLayer.Children.Add(_ghostContainer);
        }
        
        // Position it initially matching the original
        var pointerPos = GetPointerPosOnRoot();
        if (pointerPos.HasValue)
        {
            UpdateGhostPosition(pointerPos.Value);
        }
    }


    
    private Point? GetPointerPosOnRoot()
    {
        if (_originalContainer == null)
        {
            return null;
        }
        return _originalContainer.TranslatePoint(new Point(0,0), this);
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
        
        // Check and update auto-scroll based on pointer position
        UpdateAutoScroll(pointerPos);
        
        // Throttle: Check swap at most every 16ms (~60fps)
        if ((DateTime.Now - _lastSwapCheck).TotalMilliseconds >= SWAP_CHECK_THROTTLE_MS)
        {
            CheckSwap(pointerPos.Y);
            _lastSwapCheck = DateTime.Now;
        }
    }

    /// <summary>
    /// 检测指针是否接近 ScrollViewer 边缘，并启动/停止自动滚动
    /// </summary>
    private void UpdateAutoScroll(Point pointerPos)
    {
        // Find the ScrollViewer
        var scrollViewer = this.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer == null || double.IsInfinity(MaxListBoxHeight))
        {
            // No ScrollViewer or no height limit, stop auto-scroll
            StopAutoScroll();
            return;
        }

        // Get ScrollViewer bounds relative to this control
        var scrollViewerPos = scrollViewer.TranslatePoint(new Point(0, 0), this);
        if (!scrollViewerPos.HasValue) return;

        double scrollViewerTop = scrollViewerPos.Value.Y;
        double scrollViewerBottom = scrollViewerTop + scrollViewer.Bounds.Height;

        // Check if pointer is near top edge
        if (pointerPos.Y < scrollViewerTop + AutoScrollEdgeSize && scrollViewer.Offset.Y > 0)
        {
            // Scroll up
            double distanceFromEdge = pointerPos.Y - scrollViewerTop;
            double intensity = 1.0 - (distanceFromEdge / AutoScrollEdgeSize); // 0-1
            _autoScrollVelocity = -AutoScrollSpeed * intensity;
            StartAutoScroll();
        }
        // Check if pointer is near bottom edge
        else if (pointerPos.Y > scrollViewerBottom - AutoScrollEdgeSize)
        {
            // Check if we can scroll down
            var maxOffset = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
            if (scrollViewer.Offset.Y < maxOffset)
            {
                // Scroll down
                double distanceFromEdge = scrollViewerBottom - pointerPos.Y;
                double intensity = 1.0 - (distanceFromEdge / AutoScrollEdgeSize); // 0-1
                _autoScrollVelocity = AutoScrollSpeed * intensity;
                StartAutoScroll();
            }
            else
            {
                StopAutoScroll();
            }
        }
        else
        {
            // Pointer is not in edge zone
            StopAutoScroll();
        }
    }

    private void StartAutoScroll()
    {
        if (_autoScrollTimer == null)
        {
            _autoScrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps
            };
            _autoScrollTimer.Tick += (s, e) => PerformAutoScroll();
            _autoScrollTimer.Start();
        }
    }

    private void StopAutoScroll()
    {
        if (_autoScrollTimer != null)
        {
            _autoScrollTimer.Stop();
            _autoScrollTimer = null;
        }
        _autoScrollVelocity = 0;
    }

    private void PerformAutoScroll()
    {
        if (Math.Abs(_autoScrollVelocity) < 0.01) return;

        var scrollViewer = this.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer == null) return;

        double newOffset = scrollViewer.Offset.Y + _autoScrollVelocity;
        scrollViewer.Offset = scrollViewer.Offset.WithY(newOffset);
    }

    private void OnRootPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        StopDragging();
    }

    /// <summary>
    /// 根据指针位置检测是否需要与相邻项交换位置。
    /// 采用中心点检测策略：当指针跨越相邻项的中心点时触发交换。
    /// </summary>
    /// <param name="pointerY">指针在控件内的 Y 坐标</param>
    private void CheckSwap(double pointerY)
    {
        if (ItemsSource == null || _draggedItem == null) return;

        // 需要 IList 才能支持索引操作
        var list = ItemsSource as IList;
        if (list == null) return; // Can't reorder non-IList
        
        int currentIndex = list.IndexOf(_draggedItem);
        if (currentIndex < 0) return;

        // 获取项容器面板
        var panel = PART_ItemsControl.ItemsPanelRoot;
        if (panel == null) return;
        
        // 检查是否应该向上移动（与前一项交换）
        if (currentIndex > 0)
        {
            if (currentIndex - 1 >= 0 && currentIndex - 1 < panel.Children.Count)
            {
                var prevContainer = panel.Children[currentIndex - 1];
                var prevPos = prevContainer.TranslatePoint(new Point(0,0), this);
                if (prevPos.HasValue)
                {
                    // 计算前一项的垂直中心点
                    double prevCenterY = prevPos.Value.Y + prevContainer.Bounds.Height / 2;
                    
                    // 如果指针移到前一项中心点之上，触发向上交换
                    if (pointerY < prevCenterY)
                    {
                        MoveItem(currentIndex, currentIndex - 1);
                        return;
                    }
                }
            }
        }
        
        // 检查是否应该向下移动（与后一项交换）
        if (currentIndex < list.Count - 1)
        {
            if (currentIndex + 1 >= 0 && currentIndex + 1 < panel.Children.Count)
            {
                var nextContainer = panel.Children[currentIndex + 1];
                var nextPos = nextContainer.TranslatePoint(new Point(0,0), this);
                if (nextPos.HasValue)
                {
                    // 计算后一项的垂直中心点
                    double nextCenterY = nextPos.Value.Y + nextContainer.Bounds.Height / 2;
                    
                    // 如果指针移到后一项中心点之下，触发向下交换
                    if (pointerY > nextCenterY)
                    {
                        MoveItem(currentIndex, currentIndex + 1);
                        return;
                    }
                }
            }
        }
    }

    // Snapshot helper - only records positions in affected range
    private Dictionary<object, double> GetItemPositionsInRange(int startIndex, int endIndex)
    {
        var positions = new Dictionary<object, double>();
        var panel = PART_ItemsControl.ItemsPanelRoot;
        if (panel == null) return positions;

        // Expand range to include neighboring items
        int start = Math.Max(0, Math.Min(startIndex, endIndex) - 1);
        int end = Math.Min(panel.Children.Count - 1, Math.Max(startIndex, endIndex) + 1);

        for (int i = start; i <= end; i++)
        {
            if (panel.Children[i] is Control c && c.DataContext != null)
            {
                var pos = c.TranslatePoint(new Point(0,0), this);
                if (pos.HasValue)
                {
                    positions[c.DataContext] = pos.Value.Y;
                }
            }
        }
        return positions;
    }

    private void MoveItem(int oldIndex, int newIndex)
    {
        if (ItemsSource == null) return;
        
        // 1. FLIP First: Record positions in affected range
        var oldPositions = GetItemPositionsInRange(oldIndex, newIndex);

        // 2. Move Data with exception handling
        try
        {
            var listType = ItemsSource.GetType();
            var moveMethod = listType.GetMethod("Move", new[] { typeof(int), typeof(int) });
            if (moveMethod != null)
            {
                moveMethod.Invoke(ItemsSource, new object[] { oldIndex, newIndex });
            }
            else if (ItemsSource is IList list)
            {
                // Check if collection is read-only
                if (list.IsReadOnly)
                {
                    return; // Cannot modify read-only collection
                }
                
                var item = list[oldIndex];
                list.RemoveAt(oldIndex);
                list.Insert(newIndex, item);
            }
            else
            {
                return; // Collection type doesn't support reordering
            }
        }
        catch (Exception)
        {
            // Silently fail - don't crash on collection operation errors
            return;
        }

        // Trigger ItemMoved Command (Real-time feedback)
        if (ItemMovedCommand?.CanExecute(null) == true)
        {
            ItemMovedCommand.Execute(null);
        }

        // 3. Wait for Layout, then FLIP Invert & Play
        // Remove previous handler if exists to prevent accumulation
        if (_pendingLayoutHandler != null)
        {
            PART_ItemsControl.LayoutUpdated -= _pendingLayoutHandler;
            _pendingLayoutHandler = null;
        }

        // We use LayoutUpdated to ensure the visual tree has been arranged
        // This eliminates the "flash" where the item appears at new pos before animating
        _pendingLayoutHandler = (s, e) =>
        {
            PART_ItemsControl.LayoutUpdated -= _pendingLayoutHandler;
            _pendingLayoutHandler = null;
            
            UpdatePlaceholder(newIndex);
            ApplyLayoutAnimation(oldPositions);
        };
        
        PART_ItemsControl.LayoutUpdated += _pendingLayoutHandler;
        
        // Force layout invalidation to ensure event fires
        PART_ItemsControl.InvalidateMeasure();
    }
    
    private void ApplyLayoutAnimation(Dictionary<object, double> oldPositions)
    {
        var panel = PART_ItemsControl.ItemsPanelRoot;
        if (panel == null) return;

        foreach (var child in panel.Children)
        {
            if (child is Control c && c.DataContext != null)
            {
                // Skip the dragged item itself, it's hidden and represented by ghost
                if (IsDraggedItem(c.DataContext)) continue;

                if (oldPositions.TryGetValue(c.DataContext, out double oldY))
                {
                    var newPos = c.TranslatePoint(new Point(0,0), this);
                    if (newPos.HasValue)
                    {
                        double newY = newPos.Value.Y;
                        double delta = oldY - newY;
                        
                        if (Math.Abs(delta) > 0.1)
                        {
                            // FLIP Invert: Snap to old position
                            var transform = new TranslateTransform(0, delta);
                            c.RenderTransform = transform;

                            // Mark as animating
                            _animatingControls.Add(c);

                            // FLIP Play: Animate to 0
                            // We use a simple animation since Transitions on RenderTransform 
                            // might conflict if not managed carefully, but here we want explicit control.
                            // Cancel previous animation if exists
                            if (_animationCancellations.TryGetValue(c, out var oldCts))
                            {
                                oldCts.Cancel();
                                oldCts.Dispose();
                            }
                            
                            var cts = new System.Threading.CancellationTokenSource();
                            _animationCancellations[c] = cts;
                            
                            var animation = new Animation
                            {
                                Duration = TimeSpan.FromSeconds(ANIMATION_DURATION_SECONDS),
                                Easing = new CubicEaseOut(),
                                FillMode = FillMode.Forward, 
                                Children = 
                                {
                                    new KeyFrame
                                    {
                                        Cue = new Cue(0.0),
                                        Setters = { new Setter(TranslateTransform.YProperty, delta) }
                                    },
                                    new KeyFrame
                                    {
                                        Cue = new Cue(1.0),
                                        Setters = { new Setter(TranslateTransform.YProperty, 0.0) }
                                    }
                                }
                            };
                            
                            var task = animation.RunAsync(c, cts.Token);
                            task.ContinueWith(_ => 
                            {
                                Dispatcher.UIThread.Post(() =>
                                {
                                    // Capture the cancelled state before accessing cts
                                    // to avoid ObjectDisposedException if cts was disposed
                                    bool wasCancelled = _.IsCanceled || _.IsFaulted;
                                    
                                    if (!wasCancelled)
                                    {
                                        c.RenderTransform = null;
                                    }
                                    _animatingControls.Remove(c);
                                    
                                    if (_animationCancellations.TryGetValue(c, out var currentCts) && currentCts == cts)
                                    {
                                        _animationCancellations.Remove(c);
                                        cts.Dispose();
                                    }
                                });
                            });                
                        }
                    }
                }
            }
        }
    }

    private void UpdatePlaceholder(int index)
    {
        if (_draggedItem == null) return;
        
        var border = GetContainerForItem(_draggedItem);
        if (border != null)
        {
            border.Opacity = PLACEHOLDER_OPACITY;
            _originalContainer = border;
        }
    }

    private void StopDragging()
    {
        if (!_isDragging) return;
        _isDragging = false;

        // Stop auto-scroll if active
        StopAutoScroll();

        // Release pointer capture
        if (_capturedPointer != null)
        {
            _capturedPointer.Capture(null);
            _capturedPointer = null;
        }

        // Remove Ghost from Canvas (but keep reference for caching)
        if (_ghostContainer != null)
        {
            PART_DragLayer.Children.Remove(_ghostContainer);
            // Don't set to null - we reuse it for better performance
        }

        // Use cache to restore all items' Opacity
        foreach (var kvp in _containerCache)
        {
            kvp.Value.Opacity = VISIBLE_OPACITY;
        }
        
        // Fallback: Ensure original container is restored even if cache is stale
        if (_originalContainer != null)
        {
            _originalContainer.Opacity = VISIBLE_OPACITY;
        }

        
        // Clear leftover transforms (skip animating controls)
        var panel = PART_ItemsControl.ItemsPanelRoot;
        if (panel != null)
        {
             foreach(var child in panel.Children)
             {
                 if (child is Control c && !_animatingControls.Contains(c))
                 {
                     c.RenderTransform = null;
                 }
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

    /// <summary>
    /// 释放控件使用的资源
    /// </summary>
    public void Dispose()
    {
        // Unsubscribe from collection changes
        if (ItemsSource is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged -= OnCollectionChanged;
        }
        
        // Remove pointer event handlers added in constructor
        RemoveHandler(PointerMovedEvent, OnRootPointerMoved);
        RemoveHandler(PointerReleasedEvent, OnRootPointerReleased);

        // Clear caches
        _containerCache.Clear();
        _animatingControls.Clear();
        
        // Cancel all pending animations
        foreach (var cts in _animationCancellations.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _animationCancellations.Clear();

        // Release pointer capture
        if (_capturedPointer != null)
        {
            _capturedPointer.Capture(null);
            _capturedPointer = null;
        }

        // Remove layout update handler
        if (_pendingLayoutHandler != null)
        {
            PART_ItemsControl.LayoutUpdated -= _pendingLayoutHandler;
            _pendingLayoutHandler = null;
        }

        // Clean up ghost
        if (_ghostContainer != null)
        {
            PART_DragLayer.Children.Remove(_ghostContainer);
            _ghostContainer = null;
        }
    }
}
