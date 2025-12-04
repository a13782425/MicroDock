using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MicroDock.Database;
using MicroDock.Service;
using MicroDock.ViewModels;

namespace MicroDock.Views.Controls;

public partial class AppItemControl : UserControl
{
    private bool _isEditing;
    private string? _originalName;

    public AppItemControl()
    {
        InitializeComponent();
        // 绑定菜单项点击事件
        OpenMenuItem.Click += OnOpenClick;
        OpenLocationMenuItem.Click += OnOpenLocationClick;
        RenameMenuItem.Click += OnRenameClick;
        DeleteMenuItem.Click += OnDeleteClick;

        // 绑定 TextBox 事件
        NameTextBox.KeyDown += OnNameTextBoxKeyDown;
        NameTextBox.LostFocus += OnNameTextBoxLostFocus;
    }

    private ApplicationTabViewModel? GetViewModel()
    {
        // 通过可视树查找父级的 DataContext
        Visual? parent = this.GetVisualParent();
        while (parent != null)
        {
            if (parent is Control control && control.DataContext is ApplicationTabViewModel viewModel)
            {
                return viewModel;
            }
            parent = parent.GetVisualParent();
        }
        return null;
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_isEditing) return;
        
        if (DataContext is ApplicationDB app)
        {
            ServiceLocator.Get<IPlatformService>()?.TryStartProcess(app.FilePath);
        }
    }

    private void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ApplicationDB app)
        {
            ServiceLocator.Get<IPlatformService>()?.TryStartProcess(app.FilePath);
        }
    }

    private void OnOpenLocationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ApplicationDB app)
        {
            ServiceLocator.Get<IPlatformService>()?.OpenExplorer(app.FilePath);
        }
    }

    private void OnRenameClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ApplicationDB app)
        {
            StartEditing(app.Name);
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ApplicationDB app)
        {
            ApplicationTabViewModel? viewModel = GetViewModel();
            viewModel?.RemoveApplication(app);
        }
    }

    private void StartEditing(string currentName)
    {
        _isEditing = true;
        _originalName = currentName;
        
        NameTextBlock.IsVisible = false;
        NameTextBox.IsVisible = true;
        NameTextBox.Text = currentName;
        NameTextBox.Focus();
        NameTextBox.SelectAll();
    }

    private void StopEditing(bool save)
    {
        if (!_isEditing) return;
        
        _isEditing = false;
        
        if (save && DataContext is ApplicationDB app)
        {
            string newName = NameTextBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(newName) && newName != _originalName)
            {
                ApplicationTabViewModel? viewModel = GetViewModel();
                viewModel?.RenameApplication(app, newName);
                // 手动更新 TextBlock 显示（因为 ApplicationDB 没有实现 INotifyPropertyChanged）
                NameTextBlock.Text = newName;
            }
        }
        
        NameTextBox.IsVisible = false;
        NameTextBlock.IsVisible = true;
        _originalName = null;
    }

    private void OnNameTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            StopEditing(save: true);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            StopEditing(save: false);
            e.Handled = true;
        }
    }

    private void OnNameTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        StopEditing(save: true);
    }
}

