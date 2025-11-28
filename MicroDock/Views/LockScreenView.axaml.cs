using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MicroDock.ViewModels;
using System;

namespace MicroDock.Views;

public partial class LockScreenView : UserControl
{
    public LockScreenView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 处理密码框按键事件（Enter 键解锁）
    /// </summary>
    private void PasswordBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is LockScreenViewModel viewModel)
            {
                viewModel.UnlockCommand.Execute().Subscribe(_ => { });
            }
        }
    }
}

