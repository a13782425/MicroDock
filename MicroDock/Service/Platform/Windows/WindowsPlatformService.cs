using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Win32.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace MicroDock.Service;

/// <summary>
/// Windows 平台服务实现
/// </summary>
public class WindowsPlatformService : IPlatformService
{
    private const int WM_HOTKEY = 0x0312;
    private IntPtr _handle;
    private Window? _window;
    private int _currentId = 1000; // ID计数器
    private readonly Dictionary<string, int> _idMapping = new(); // uniqueId -> win32Id
    private readonly Dictionary<int, Action> _callbacks = new(); // win32Id -> callback
    private bool _isInitialized;

    public void Initialize(Window window)
    {
        if (_isInitialized) return;
        _window = window;

        var platformHandle = window.TryGetPlatformHandle();
        if (platformHandle != null)
        {
            _handle = platformHandle.Handle;
            // 挂载消息钩子
            // 注意：Avalonia的窗口句柄获取方式可能需要根据具体版本调整，这里假设是标准的Win32句柄
            // Avalonia目前没有直接暴露WndProc Hook，需要变通方法或者使用特定API
            // 对于简单的HotKey，我们可以尝试使用 Win32 API SetWindowLongPtr 来替换 WndProc
            // 或者使用 ApplicationLifetime 中的事件? 不，通常需要原生窗口过程。
            // Avalonia 11 推荐方式:
            // 但 MicroDock 似乎没有使用特定的 Win32 库。
            // 这里使用简单的消息循环监听可能比较困难。
            // 替代方案：启动一个不可见的 WinForms/WPF 窗口或者使用 HwndSource (WPF)
            // 但我们不想引入 WPF/WinForms。

            // 实际上，我们可以通过 P/Invoke SetWindowLongPtr 来子类化窗口过程。
            // 但这比较危险。

            // 另一个方案：轮询 GetMessage? 不行。

            // 幸运的是，我们只需要在注册热键时工作。
            // 我们先尝试子类化窗口过程。

            _newWndProcDelegate = new WndProcDelegate(CustomWndProc);
            _oldWndProc = SetWindowLongPtr(_handle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProcDelegate));

            _isInitialized = true;
            Log.Information("WindowsPlatformService initialized with handle: {Handle}", _handle);
        }
    }

    public bool TryStartProcess(string path)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            Process? process = Process.Start(psi);
            bool success = process != null;

            if (success)
            {
                Log.Information("成功启动进程: {Path}", path);
            }
            else
            {
                Log.Warning("启动进程失败(返回null): {Path}", path);
            }

            return success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动进程失败: {Path}", path);
            return false;
        }
    }
    public bool OpenExplorer(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                // 打开文件所在文件夹并选中该文件
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            else if (Directory.Exists(filePath))
            {
                // 打开文件夹
                Process.Start("explorer.exe", $"\"{filePath}\"");
            }
            return true;
        }
        catch
        {
            // 静默失败
            return false;
        }
    }
    public bool RegisterHotKey(string uniqueId, string keyCombination, Action callback)
    {
        if (!_isInitialized || _handle == IntPtr.Zero)
        {
            Log.Warning("Cannot register hotkey: Platform service not initialized or invalid handle.");
            return false;
        }

        // 解析快捷键
        if (!ParseKeyCombination(keyCombination, out uint modifiers, out uint vkey))
        {
            Log.Warning("Invalid key combination: {KeyCombination}", keyCombination);
            return false;
        }

        // 如果已存在，先注销
        UnregisterHotKey(uniqueId);

        int id = _currentId++;
        if (RegisterHotKey(_handle, id, modifiers, vkey))
        {
            _idMapping[uniqueId] = id;
            _callbacks[id] = callback;
            Log.Information("Registered hotkey: {KeyCombination} (ID: {Id})", keyCombination, id);
            return true;
        }
        else
        {
            Log.Error("Failed to register hotkey: {KeyCombination}. Error code: {Error}", keyCombination, Marshal.GetLastWin32Error());
            return false;
        }
    }

    public void UnregisterHotKey(string uniqueId)
    {
        if (_idMapping.TryGetValue(uniqueId, out int id))
        {
            UnregisterHotKey(_handle, id);
            _idMapping.Remove(uniqueId);
            _callbacks.Remove(id);
            Log.Information("Unregistered hotkey: {UniqueId} (ID: {Id})", uniqueId, id);
        }
    }

    public void Dispose()
    {
        // 恢复窗口过程
        if (_handle != IntPtr.Zero && _oldWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(_handle, GWL_WNDPROC, _oldWndProc);
        }

        // 注销所有热键
        foreach (var id in _idMapping.Values)
        {
            UnregisterHotKey(_handle, id);
        }
        _idMapping.Clear();
        _callbacks.Clear();
    }

    // P/Invoke definitions
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Modifiers
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    // Subclassing
    private const int GWL_WNDPROC = -4;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProcDelegate? _newWndProcDelegate; // Keep reference to prevent GC
    private IntPtr _oldWndProc;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // 兼容 32位/64位
    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);


    private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_callbacks.TryGetValue(id, out var callback))
            {
                // 在UI线程执行
                Avalonia.Threading.Dispatcher.UIThread.Post(() => callback?.Invoke());
            }
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private bool ParseKeyCombination(string combo, out uint modifiers, out uint vkey)
    {
        modifiers = 0;
        vkey = 0;

        if (string.IsNullOrWhiteSpace(combo)) return false;

        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToUpper())
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= MOD_CONTROL;
                    break;
                case "ALT":
                    modifiers |= MOD_ALT;
                    break;
                case "SHIFT":
                    modifiers |= MOD_SHIFT;
                    break;
                case "WIN":
                case "WINDOWS":
                case "META":
                    modifiers |= MOD_WIN;
                    break;
                default:
                    // Parse key
                    if (Enum.TryParse<Key>(part, true, out var key))
                    {
                        vkey = (uint)KeyInterop.VirtualKeyFromKey(key);
                    }
                    else
                    {
                        // Try single character
                        if (part.Length == 1)
                        {
                            // Very basic mapping for letters/numbers
                            // This might need improvement
                            // Avalonia Key enum is preferred
                            return false;
                        }
                    }
                    break;
            }
        }

        return vkey != 0;
    }
}

