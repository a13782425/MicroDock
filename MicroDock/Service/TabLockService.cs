using Avalonia.Threading;
using MicroDock.Database;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MicroDock.Service;

/// <summary>
/// 页签锁定服务 - 管理页签的密码保护和解锁状态
/// </summary>
public class TabLockService : IDisposable
{
    private const string LOG_TAG = "TabLockService";

    /// <summary>
    /// 解锁超时时间（分钟）
    /// </summary>
    private const int UNLOCK_TIMEOUT_MINUTES = 10;

    /// <summary>
    /// 已解锁页签及其解锁时间
    /// </summary>
    private readonly Dictionary<string, DateTime> _unlockedTabs = new();

    /// <summary>
    /// 超时检查定时器
    /// </summary>
    private readonly DispatcherTimer _timeoutTimer;

    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _disposed = false;

    public TabLockService()
    {
        // 创建定时器，每分钟检查一次超时
        _timeoutTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _timeoutTimer.Tick += OnTimeoutTimerTick;
        _timeoutTimer.Start();

        Log.Debug("[{Tag}] 页签锁定服务已初始化", LOG_TAG);
    }

    #region 密码管理

    /// <summary>
    /// 设置页签密码
    /// </summary>
    /// <param name="tabId">页签唯一ID</param>
    /// <param name="password">密码明文</param>
    /// <returns>是否成功</returns>
    public bool SetPassword(string tabId, string password)
    {
        if (string.IsNullOrEmpty(tabId) || string.IsNullOrEmpty(password))
            return false;

        try
        {
            string passwordHash = ComputePasswordHash(password);
            DBContext.SetTabLock(tabId, passwordHash);

            // 设置密码后，移除解锁状态
            lock (_unlockedTabs)
            {
                _unlockedTabs.Remove(tabId);
            }

            // 发布锁定状态变更消息
            ServiceLocator.Get<EventService>().Publish(new TabLockStateChangedMessage(tabId, true));

            Log.Information("[{Tag}] 页签 {TabId} 已设置密码锁定", LOG_TAG, tabId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 设置页签密码失败: {TabId}", LOG_TAG, tabId);
            return false;
        }
    }

    /// <summary>
    /// 修改页签密码
    /// </summary>
    /// <param name="tabId">页签唯一ID</param>
    /// <param name="oldPassword">旧密码</param>
    /// <param name="newPassword">新密码</param>
    /// <returns>是否成功</returns>
    public bool ChangePassword(string tabId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(tabId) || string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            return false;

        // 验证旧密码
        if (!VerifyPassword(tabId, oldPassword))
        {
            Log.Warning("[{Tag}] 修改密码失败：旧密码不正确 - {TabId}", LOG_TAG, tabId);
            return false;
        }

        return SetPassword(tabId, newPassword);
    }

    /// <summary>
    /// 移除页签密码
    /// </summary>
    /// <param name="tabId">页签唯一ID</param>
    /// <param name="password">当前密码（用于验证）</param>
    /// <returns>是否成功</returns>
    public bool RemovePassword(string tabId, string password)
    {
        if (string.IsNullOrEmpty(tabId))
            return false;

        // 验证密码
        if (!VerifyPassword(tabId, password))
        {
            Log.Warning("[{Tag}] 移除密码失败：密码不正确 - {TabId}", LOG_TAG, tabId);
            return false;
        }

        try
        {
            DBContext.RemoveTabLock(tabId);

            // 移除解锁状态
            lock (_unlockedTabs)
            {
                _unlockedTabs.Remove(tabId);
            }

            // 发布锁定状态变更消息
            ServiceLocator.Get<EventService>().Publish(new TabLockStateChangedMessage(tabId, false));

            Log.Information("[{Tag}] 页签 {TabId} 已移除密码锁定", LOG_TAG, tabId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{Tag}] 移除页签密码失败: {TabId}", LOG_TAG, tabId);
            return false;
        }
    }

    /// <summary>
    /// 验证密码是否正确
    /// </summary>
    /// <param name="tabId">页签唯一ID</param>
    /// <param name="password">密码明文</param>
    /// <returns>是否正确</returns>
    public bool VerifyPassword(string tabId, string password)
    {
        if (string.IsNullOrEmpty(tabId) || string.IsNullOrEmpty(password))
            return false;

        string? storedHash = DBContext.GetTabPasswordHash(tabId);
        if (string.IsNullOrEmpty(storedHash))
            return false;

        string inputHash = ComputePasswordHash(password);
        return string.Equals(storedHash, inputHash, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 解锁状态管理

    /// <summary>
    /// 尝试解锁页签
    /// </summary>
    /// <param name="tabId">页签唯一ID</param>
    /// <param name="password">密码明文</param>
    /// <returns>是否解锁成功</returns>
    public bool TryUnlock(string tabId, string password)
    {
        if (!VerifyPassword(tabId, password))
        {
            Log.Warning("[{Tag}] 解锁失败：密码不正确 - {TabId}", LOG_TAG, tabId);
            return false;
        }

        lock (_unlockedTabs)
        {
            _unlockedTabs[tabId] = DateTime.Now;
        }

        // 发布解锁消息
        ServiceLocator.Get<EventService>().Publish(new TabUnlockedMessage(tabId));

        Log.Information("[{Tag}] 页签 {TabId} 已解锁", LOG_TAG, tabId);
        return true;
    }

    /// <summary>
    /// 检查页签是否已解锁（且未超时）
    /// </summary>
    /// <param name="tabId">页签唯一ID</param>
    /// <returns>是否已解锁</returns>
    public bool IsUnlocked(string tabId)
    {
        if (string.IsNullOrEmpty(tabId))
            return false;

        // 如果页签没有设置锁定，则默认为已解锁
        if (!DBContext.IsTabLocked(tabId))
            return true;

        lock (_unlockedTabs)
        {
            if (_unlockedTabs.TryGetValue(tabId, out DateTime unlockTime))
            {
                // 检查是否超时
                if ((DateTime.Now - unlockTime).TotalMinutes < UNLOCK_TIMEOUT_MINUTES)
                {
                    return true;
                }
                else
                {
                    // 已超时，移除解锁状态
                    _unlockedTabs.Remove(tabId);
                    return false;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 检查页签是否设置了密码锁定
    /// </summary>
    /// <param name="tabId">页签唯一ID</param>
    /// <returns>是否已锁定</returns>
    public bool IsLocked(string tabId)
    {
        return DBContext.IsTabLocked(tabId);
    }

    /// <summary>
    /// 刷新页签的解锁时间（用户操作时调用）
    /// </summary>
    /// <param name="tabId">页签唯一ID</param>
    public void RefreshUnlockTime(string tabId)
    {
        if (string.IsNullOrEmpty(tabId))
            return;

        lock (_unlockedTabs)
        {
            if (_unlockedTabs.ContainsKey(tabId))
            {
                _unlockedTabs[tabId] = DateTime.Now;
            }
        }
    }

    /// <summary>
    /// 锁定指定页签（重新加锁）
    /// </summary>
    /// <param name="tabId">页签唯一ID</param>
    public void LockTab(string tabId)
    {
        if (string.IsNullOrEmpty(tabId))
            return;

        bool wasUnlocked = false;
        lock (_unlockedTabs)
        {
            wasUnlocked = _unlockedTabs.Remove(tabId);
        }

        if (wasUnlocked)
        {
            // 发布加锁消息
            ServiceLocator.Get<EventService>().Publish(new TabLockedMessage(tabId));
            Log.Information("[{Tag}] 页签 {TabId} 已重新加锁", LOG_TAG, tabId);
        }
    }

    /// <summary>
    /// 锁定所有已解锁的页签（窗口最小化时调用）
    /// </summary>
    public void LockAll()
    {
        List<string> tabsToLock;

        lock (_unlockedTabs)
        {
            tabsToLock = _unlockedTabs.Keys.ToList();
            _unlockedTabs.Clear();
        }

        foreach (string tabId in tabsToLock)
        {
            ServiceLocator.Get<EventService>().Publish(new TabLockedMessage(tabId));
        }

        if (tabsToLock.Count > 0)
        {
            Log.Information("[{Tag}] 已锁定所有页签，共 {Count} 个", LOG_TAG, tabsToLock.Count);
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 计算密码的 SHA256 哈希值
    /// </summary>
    private static string ComputePasswordHash(string password)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(password);
        byte[] hashBytes = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 超时检查定时器回调
    /// </summary>
    private void OnTimeoutTimerTick(object? sender, EventArgs e)
    {
        CheckTimeout();
    }

    /// <summary>
    /// 检查并处理超时的解锁页签
    /// </summary>
    private void CheckTimeout()
    {
        List<string> expiredTabs = new();

        lock (_unlockedTabs)
        {
            DateTime now = DateTime.Now;
            foreach (var kvp in _unlockedTabs)
            {
                if ((now - kvp.Value).TotalMinutes >= UNLOCK_TIMEOUT_MINUTES)
                {
                    expiredTabs.Add(kvp.Key);
                }
            }

            foreach (string tabId in expiredTabs)
            {
                _unlockedTabs.Remove(tabId);
            }
        }

        // 发布超时加锁消息
        foreach (string tabId in expiredTabs)
        {
            ServiceLocator.Get<EventService>().Publish(new TabLockedMessage(tabId));
            Log.Information("[{Tag}] 页签 {TabId} 因超时已自动加锁", LOG_TAG, tabId);
        }
    }

    #endregion

    public void Dispose()
    {
        if (_disposed)
            return;

        _timeoutTimer.Stop();
        _timeoutTimer.Tick -= OnTimeoutTimerTick;
        _disposed = true;

        Log.Debug("[{Tag}] 页签锁定服务已释放", LOG_TAG);
    }
}

