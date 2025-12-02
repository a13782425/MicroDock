using DynamicData.Binding;
using MicroDock.Database;
using MicroDock.Model;
using MicroDock.Service;
using MicroDock.Utils;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace MicroDock.ViewModels;

/// <summary>
/// 导航页签设置项
/// </summary>
public class NavigationTabSettingItem : ReactiveObject, IDisposable
{
    private string _name = string.Empty;
    private bool _isVisible;
    private string _shortcutKey = string.Empty;
    private int _orderIndex;
    private NavigationTabDto _tabDto;
    private bool _isEnable = true;
    private bool _isLocked = false;
    private readonly CompositeDisposable _cleanUp = new();

    /// <summary>
    /// 设置/修改密码命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> SetPasswordCommand { get; }

    /// <summary>
    /// 移除密码命令
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemovePasswordCommand { get; }

#pragma warning disable CS8618
    public NavigationTabSettingItem(NavigationTabDto? navigationTabDto)
#pragma warning restore CS8618 
    {
        SetPasswordCommand = ReactiveCommand.CreateFromTask(SetPasswordAsync);
        RemovePasswordCommand = ReactiveCommand.CreateFromTask(RemovePasswordAsync);

        if (navigationTabDto == null)
        {
            UniqueId = "";
            return;
        }
        this._tabDto = navigationTabDto;
        UniqueId = _tabDto.Id;
        OrderIndex = _tabDto.OrderIndex;
        IsVisible = _tabDto.IsVisible;
        _isLocked = _tabDto.IsLocked;

        _tabDto.WhenValueChanged(a => a.IsVisible)
            .Subscribe(_ => IsVisible = _tabDto.IsVisible)
            .DisposeWith(_cleanUp);
        _tabDto.WhenValueChanged(a => a.IsLocked)
            .Subscribe(_ => IsLocked = _tabDto.IsLocked)
            .DisposeWith(_cleanUp);

        // 订阅锁定状态变更消息
        ServiceLocator.Get<EventService>()?.Subscribe<TabLockStateChangedMessage>(OnTabLockStateChanged);
    }

    private void OnTabLockStateChanged(TabLockStateChangedMessage message)
    {
        if (message.TabId == UniqueId)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsLocked = message.IsLocked;
            });
        }
    }

    public string UniqueId { get; init; }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public int OrderIndex
    {
        get => _orderIndex;
        set
        {
            if (_orderIndex != value)
            {
                this.RaiseAndSetIfChanged(ref _orderIndex, value);
                _tabDto.OrderIndex = value;
            }
        }
    }

    public bool IsVisible
    {
        get => _isEnable ? _isVisible : false;
        set
        {
            if (_isVisible != value)
            {
                this.RaiseAndSetIfChanged(ref _isVisible, value);
                _tabDto.IsVisible = value;
            }
        }
    }

    public bool IsEnable
    {
        get => _isEnable;
        set
        {
            if (_isEnable != value)
            {
                this.RaiseAndSetIfChanged(ref _isEnable, value);
                this.RaisePropertyChanged(nameof(IsVisible));
            }
        }
    }

    public string ShortcutKey
    {
        get => _shortcutKey;
        set
        {
            if (_shortcutKey != value)
            {
                this.RaiseAndSetIfChanged(ref _shortcutKey, value);
                // Update DB and Register
                var tab = DBContext.GetNavigationTab(UniqueId);
                if (tab != null)
                {
                    tab.ShortcutKey = value;
                    DBContext.UpdateNavigationTab(tab);

                    // Re-register hotkey
                    var platformService = ServiceLocator.Get<MicroDock.Service.IPlatformService>();
                    if (string.IsNullOrEmpty(value))
                    {
                        platformService?.UnregisterHotKey(UniqueId);
                    }
                    else
                    {
                        platformService?.RegisterHotKey(UniqueId, value, () =>
                        {
                            ServiceLocator.Get<EventService>()?.Publish(new NavigateToTabMessage(uniqueId: UniqueId));
                            ServiceLocator.Get<EventService>()?.Publish(new MainWindowShowMessage());
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// 是否已设置密码锁定
    /// </summary>
    public bool IsLocked
    {
        get => _isLocked;
        set => this.RaiseAndSetIfChanged(ref _isLocked, value);
    }

    /// <summary>
    /// 密码设置按钮文本
    /// </summary>
    public string PasswordButtonText => IsLocked ? "修改密码" : "设置密码";

    /// <summary>
    /// 设置或修改密码
    /// </summary>
    private async Task SetPasswordAsync()
    {
        var tabLockService = ServiceLocator.Get<TabLockService>();

        if (IsLocked)
        {
            // 修改密码 - 需要先验证旧密码
            var oldPassword = await ShowPasswordInputDialogAsync("修改密码", "请输入当前密码：");
            if (string.IsNullOrEmpty(oldPassword))
                return;

            if (!tabLockService.VerifyPassword(UniqueId, oldPassword))
            {
                ShowNotification("密码错误", "当前密码不正确", AppNotificationType.Error);
                return;
            }

            var newPassword = await ShowPasswordInputDialogAsync("修改密码", "请输入新密码：");
            if (string.IsNullOrEmpty(newPassword))
                return;

            var confirmPassword = await ShowPasswordInputDialogAsync("修改密码", "请再次输入新密码：");
            if (newPassword != confirmPassword)
            {
                ShowNotification("密码不匹配", "两次输入的密码不一致", AppNotificationType.Error);
                return;
            }

            if (tabLockService.ChangePassword(UniqueId, oldPassword, newPassword))
            {
                ShowNotification("修改成功", "密码已修改", AppNotificationType.Success);
            }
            else
            {
                ShowNotification("修改失败", "密码修改失败", AppNotificationType.Error);
            }
        }
        else
        {
            // 设置新密码
            var newPassword = await ShowPasswordInputDialogAsync("设置密码", "请输入密码：");
            if (string.IsNullOrEmpty(newPassword))
                return;

            var confirmPassword = await ShowPasswordInputDialogAsync("设置密码", "请再次输入密码：");
            if (newPassword != confirmPassword)
            {
                ShowNotification("密码不匹配", "两次输入的密码不一致", AppNotificationType.Error);
                return;
            }

            if (tabLockService.SetPassword(UniqueId, newPassword))
            {
                IsLocked = true;
                this.RaisePropertyChanged(nameof(PasswordButtonText));
                ShowNotification("设置成功", "密码已设置，页签已加锁", AppNotificationType.Success);
            }
            else
            {
                ShowNotification("设置失败", "密码设置失败", AppNotificationType.Error);
            }
        }
    }

    /// <summary>
    /// 移除密码
    /// </summary>
    private async Task RemovePasswordAsync()
    {
        if (!IsLocked)
            return;

        var password = await ShowPasswordInputDialogAsync("移除密码", "请输入当前密码以移除锁定：");
        if (string.IsNullOrEmpty(password))
            return;

        var tabLockService = ServiceLocator.Get<TabLockService>();
        if (tabLockService.RemovePassword(UniqueId, password))
        {
            IsLocked = false;
            this.RaisePropertyChanged(nameof(PasswordButtonText));
            ShowNotification("移除成功", "密码已移除，页签已解锁", AppNotificationType.Success);
        }
        else
        {
            ShowNotification("密码错误", "密码不正确，无法移除", AppNotificationType.Error);
        }
    }

    /// <summary>
    /// 显示密码输入对话框
    /// </summary>
    private async Task<string?> ShowPasswordInputDialogAsync(string title, string prompt)
    {
        return await UniversalUtils.ShowPasswordInputDialogAsync(title, prompt);
    }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}
