using MicroDock.Database;
using MicroDock.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MicroDock.Service;

/// <summary>
/// 延时存储
/// </summary>
public class DelayStorageService : IDisposable
{
    private readonly HashSet<IDatabaseDto> _dirtyQueue = new();

    private readonly List<IDatabaseDto> _saveList = new List<IDatabaseDto>();
    private readonly object _lock = new();
    private readonly Timer _timer;
    private bool _disposed;
    public DelayStorageService()
    {
        // 延迟 1秒 执行，周期 1秒
        _timer = new Timer(Flush, null, 1000, 1000);
    }

    internal void RegisterDirtyObject(IDatabaseDto databaseDto)
    {
        lock (_lock)
        {
            _dirtyQueue.Add(databaseDto);
        }
    }
    /// <summary>
    /// 强制立即保存所有脏数据
    /// </summary>
    public void ForceSave()
    {
        Flush(null);
    }

    private void Flush(object? state)
    {
        if (_disposed) return;

        _saveList.Clear();
        lock (_lock)
        {
            if (_dirtyQueue.Count == 0) return;
            _saveList.AddRange(_dirtyQueue);
            _dirtyQueue.Clear();
        }

        try
        {
            if (_saveList.Count > 50)
            {
                DBContext.RunInTransaction(() =>
                {
                    foreach (var dto in _saveList)
                    {
                        dto.Save();
                    }
                });
            }
            else
            {
                foreach (var dto in _saveList)
                {
                    dto.Save();
                }
            }
            _saveList.Clear();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BatchSaver] Batch save failed!");
            // 简单的重试策略：出错放回队列（需谨慎防止死循环）
            lock (_lock)
            {
                foreach (var item in _saveList) _dirtyQueue.Add(item);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Dispose();
        // 退出前最后一次保存
        Flush(null);
    }
}
