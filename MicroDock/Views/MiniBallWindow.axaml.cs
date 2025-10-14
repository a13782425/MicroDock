using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MicroDock.Database;
using MicroDock.Services;
using System;
using Avalonia;

namespace MicroDock.Views;

public partial class MiniBallWindow : Window
{
    private readonly DispatcherTimer _longPressTimer;
    private bool _isDragging;
    private bool _isExpanded;
    private PointerPressedEventArgs? _dragStartArgs;
    private readonly IMiniModeService? _miniModeService;

    // Parameterless for designer
    public MiniBallWindow()
    {
        InitializeComponent();
        _longPressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _longPressTimer.Tick += OnLongPress;
        _isExpanded = false;
    }

    public MiniBallWindow(IMiniModeService miniModeService) : this()
    {
        _miniModeService = miniModeService;
        LauncherView.MiniModeService = miniModeService;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDragging = false;
        _dragStartArgs = e;
        _longPressTimer.Start();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _longPressTimer.Stop();
        if (!_isDragging)
        {
            ResetToBall();
        }
        _dragStartArgs = null;
        e.Handled = true;
    }

    private void OnLongPress(object? sender, EventArgs e)
    {
        _longPressTimer.Stop();
        _isDragging = false;
        _isExpanded = true;
        
        var apps = DBContext.GetApplications();
        
        Width = 200;
        Height = 200;
        
        LauncherView.IsVisible = true;
        LauncherView.Applications = apps;
    }
    
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        ResetToBall();
    }
    
    private void ResetToBall()
    {
        LauncherView.IsVisible = false;
        // CancelButton.IsVisible = false;
        Width = 64;
        Height = 64;
        _isExpanded = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isExpanded && _longPressTimer.IsEnabled && _dragStartArgs != null)
        {
            _longPressTimer.Stop();
            _isDragging = true;
            BeginMoveDrag(_dragStartArgs);
        }
    }
}
