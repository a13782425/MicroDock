using MicroDock.Service;
using ReactiveUI;
using System;
using System.Reactive;

namespace MicroDock.ViewModels;

/// <summary>
/// 锁屏界面 ViewModel
/// </summary>
public class LockScreenViewModel : ViewModelBase
{
    private string _tabId;
    private string _tabTitle;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError = false;

    /// <summary>
    /// 解锁成功事件
    /// </summary>
    public event EventHandler<string>? UnlockSucceeded;

    /// <summary>
    /// 解锁命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> UnlockCommand { get; }

    public LockScreenViewModel(string tabId, string tabTitle)
    {
        _tabId = tabId;
        _tabTitle = tabTitle;

        UnlockCommand = ReactiveCommand.Create(TryUnlock);
    }

    /// <summary>
    /// 页签标题
    /// </summary>
    public string TabTitle
    {
        get => _tabTitle;
        set => this.RaiseAndSetIfChanged(ref _tabTitle, value);
    }

    /// <summary>
    /// 密码输入
    /// </summary>
    public string Password
    {
        get => _password;
        set
        {
            this.RaiseAndSetIfChanged(ref _password, value);
            // 清除错误提示
            if (HasError)
            {
                HasError = false;
                ErrorMessage = string.Empty;
            }
        }
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool HasError
    {
        get => _hasError;
        set => this.RaiseAndSetIfChanged(ref _hasError, value);
    }

    /// <summary>
    /// 尝试解锁
    /// </summary>
    private void TryUnlock()
    {
        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "请输入密码";
            HasError = true;
            return;
        }

        var tabLockService = ServiceLocator.Get<TabLockService>();
        if (tabLockService.TryUnlock(_tabId, Password))
        {
            // 解锁成功
            UnlockSucceeded?.Invoke(this, _tabId);
        }
        else
        {
            // 密码错误
            ErrorMessage = "密码错误，请重试";
            HasError = true;
            Password = string.Empty;
        }
    }
}

