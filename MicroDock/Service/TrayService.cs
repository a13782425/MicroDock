using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Serilog;

namespace MicroDock.Service;

/// <summary>
/// 托盘服务实现
/// </summary>
public class TrayService
{
    private TrayIcon? _trayIcon;
    private NativeMenu? _trayMenu;
    private readonly Dictionary<string, NativeMenuItem> _menuItems = new();
    private readonly Dictionary<string, NativeMenuItemSeparator> _separators = new();
    
    // 核心菜单项（显示/隐藏/退出）
    private NativeMenuItem? _showMenuItem;
    private NativeMenuItem? _hideMenuItem;
    private NativeMenuItemSeparator? _coreSeparator;
    private NativeMenuItem? _exitMenuItem;

    /// <summary>
    /// 初始化托盘服务
    /// </summary>
    public void Initialize()
    {
        if (_trayIcon != null)
        {
            Log.Warning("TrayService 已经初始化");
            return;
        }

        _trayIcon = new TrayIcon();
        _trayMenu = new NativeMenu();
        
        // 设置托盘图标
        try
        {
            _trayIcon.Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://MicroDock/Assets/logo.ico")));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载托盘图标失败");
        }
        
        // 设置工具提示
        _trayIcon.ToolTipText = "MicroDock - 双击显示/隐藏";
        
        // 双击托盘图标显示/隐藏窗口
        _trayIcon.Clicked += (sender, args) =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
                && desktop.MainWindow != null)
            {
                if (desktop.MainWindow.IsVisible)
                {
                    desktop.MainWindow.Hide();
                }
                else
                {
                    desktop.MainWindow.Show();
                    // 如果窗口处于最小化状态，需要还原
                    if (desktop.MainWindow.WindowState == WindowState.Minimized)
                    {
                        desktop.MainWindow.WindowState = WindowState.Normal;
                    }

                    desktop.MainWindow.Activate();
                    // 2. 智能置顶逻辑
                    // 只有当窗口当前【不是】置顶状态时，才执行"临时置顶"策略
                    // 这样既能把窗口拉到最前，又不会误关掉用户开启的"始终置顶"
                    if (!desktop.MainWindow.Topmost)
                    {
                        desktop.MainWindow.Topmost = true;

                        // 使用 Post 确保在下一帧或当前UI操作完成后执行，
                        // 这样能让操作系统有足够时间响应 Topmost=true 的状态，把窗口层级拉上来
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            // 再次检查确认用户没有在这一瞬间开启了全局置顶（极端情况防御），然后还原
                            if (desktop.MainWindow != null && !ServiceLocator.Get<TopMostService>().IsEnabled)
                            {
                                // 注意：这里最好结合你的 SettingDB 或 TopMostService 状态来判断
                                // 如果不想依赖 Service，最简单的做法是直接还原:
                                desktop.MainWindow.Topmost = false;
                            }
                            else
                            {
                                // 如果稍微复杂点，不依赖Service的简单写法：
                                desktop.MainWindow.Topmost = false;
                            }
                        });
                    }
                }
            }
        };
        
        // 创建核心菜单项
        InitializeCoreMenuItems();
        
        // 设置菜单
        _trayIcon.Menu = _trayMenu;
        
        // 显示托盘图标
        _trayIcon.IsVisible = true;
        
        Log.Information("TrayService 初始化完成");
    }

    /// <summary>
    /// 初始化核心菜单项
    /// </summary>
    private void InitializeCoreMenuItems()
    {
        if (_trayMenu == null) return;
        
        _showMenuItem = new NativeMenuItem("显示");
        _showMenuItem.Click += (sender, args) =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
                && desktop.MainWindow != null)
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();
            }
        };
        
        _hideMenuItem = new NativeMenuItem("隐藏");
        _hideMenuItem.Click += (sender, args) =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop 
                && desktop.MainWindow != null)
            {
                desktop.MainWindow.Hide();
            }
        };
        
        _exitMenuItem = new NativeMenuItem("退出");
        _exitMenuItem.Click += (sender, args) =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                AppConfig.RealExit = true;
                desktop.Shutdown();
            }
        };
        
        _coreSeparator = new NativeMenuItemSeparator();
        
        // 添加核心菜单项到菜单末尾
        _trayMenu.Items.Add(_showMenuItem);
        _trayMenu.Items.Add(_hideMenuItem);
        _trayMenu.Items.Add(_coreSeparator);
        _trayMenu.Items.Add(_exitMenuItem);
    }

    /// <summary>
    /// 重建菜单（插件菜单项 + 核心菜单项）
    /// </summary>
    private void RebuildMenu()
    {
        if (_trayMenu == null) return;
        
        _trayMenu.Items.Clear();
        
        // 添加插件菜单项
        foreach (var item in _menuItems.Values)
        {
            _trayMenu.Items.Add(item);
        }
        
        // 添加分隔符
        foreach (var separator in _separators.Values)
        {
            _trayMenu.Items.Add(separator);
        }
        
        // 添加核心菜单项（始终在底部）
        if (_showMenuItem != null) _trayMenu.Items.Add(_showMenuItem);
        if (_hideMenuItem != null) _trayMenu.Items.Add(_hideMenuItem);
        if (_coreSeparator != null) _trayMenu.Items.Add(_coreSeparator);
        if (_exitMenuItem != null) _trayMenu.Items.Add(_exitMenuItem);
    }

    /// <summary>
    /// 添加菜单项
    /// </summary>
    public void AddMenuItem(string id, string text, Action onClick)
    {
        if (string.IsNullOrEmpty(id))
        {
            Log.Warning("AddMenuItem: id 不能为空");
            return;
        }
        
        if (_menuItems.ContainsKey(id))
        {
            Log.Warning("AddMenuItem: id '{Id}' 已存在", id);
            return;
        }
        
        var menuItem = new NativeMenuItem(text);
        menuItem.Click += (sender, args) => onClick?.Invoke();
        
        _menuItems[id] = menuItem;
        RebuildMenu();
        
        Log.Debug("TrayService: 添加菜单项 '{Id}' - '{Text}'", id, text);
    }

    /// <summary>
    /// 移除菜单项
    /// </summary>
    public void RemoveMenuItem(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Log.Warning("RemoveMenuItem: id 不能为空");
            return;
        }
        
        if (_menuItems.Remove(id))
        {
            RebuildMenu();
            Log.Debug("TrayService: 移除菜单项 '{Id}'", id);
        }
        else
        {
            Log.Warning("RemoveMenuItem: 菜单项 '{Id}' 不存在", id);
        }
    }

    /// <summary>
    /// 添加分隔符
    /// </summary>
    public void AddSeparator(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Log.Warning("AddSeparator: id 不能为空");
            return;
        }
        
        if (_separators.ContainsKey(id))
        {
            Log.Warning("AddSeparator: id '{Id}' 已存在", id);
            return;
        }
        
        var separator = new NativeMenuItemSeparator();
        _separators[id] = separator;
        RebuildMenu();
        
        Log.Debug("TrayService: 添加分隔符 '{Id}'", id);
    }

    /// <summary>
    /// 设置托盘图标
    /// </summary>
    public void SetIcon(Stream iconStream)
    {
        if (_trayIcon == null)
        {
            Log.Warning("SetIcon: TrayIcon 未初始化");
            return;
        }
        
        try
        {
            _trayIcon.Icon = new WindowIcon(iconStream);
            Log.Debug("TrayService: 设置托盘图标成功");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "TrayService: 设置托盘图标失败");
        }
    }

    /// <summary>
    /// 设置工具提示文本
    /// </summary>
    public void SetToolTip(string text)
    {
        if (_trayIcon == null)
        {
            Log.Warning("SetToolTip: TrayIcon 未初始化");
            return;
        }
        
        _trayIcon.ToolTipText = text;
        Log.Debug("TrayService: 设置工具提示 '{Text}'", text);
    }
}

