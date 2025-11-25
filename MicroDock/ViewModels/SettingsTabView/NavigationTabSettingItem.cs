using DynamicData.Binding;
using MicroDock.Database;
using MicroDock.Model;
using MicroDock.Service;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

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
    private readonly CompositeDisposable _cleanUp = new();
#pragma warning disable CS8618
    public NavigationTabSettingItem(NavigationTabDto? navigationTabDto)
#pragma warning restore CS8618 
    {
        if (navigationTabDto == null)
        {
            UniqueId = "";
            return;
        }
        this._tabDto = navigationTabDto;
        UniqueId = _tabDto.Id;
        OrderIndex = _tabDto.OrderIndex;
        IsVisible = _tabDto.IsVisible;
        _tabDto.WhenValueChanged(a => a.IsVisible)
            .Subscribe(_ => IsVisible = _tabDto.IsVisible)
            .DisposeWith(_cleanUp);
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
                    var platformService = ServiceLocator.GetService<MicroDock.Service.Platform.IPlatformService>();
                    if (string.IsNullOrEmpty(value))
                    {
                        platformService?.UnregisterHotKey(UniqueId);
                    }
                    else
                    {
                        platformService?.RegisterHotKey(UniqueId, value, () =>
                        {
                            ServiceLocator.Get<EventService>().Publish(new NavigateToTabMessage(uniqueId: UniqueId));
                            ServiceLocator.Get<EventService>().Publish(new MainWindowShowMessage());
                        });
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}
