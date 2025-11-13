using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroDock.Infrastructure;

/// <summary>
/// 事件聚合器 - 用于组件间的松耦合通信
/// 实现单例模式，使用弱引用避免内存泄漏
/// </summary>
public class EventAggregator
{
    private static readonly Lazy<EventAggregator> _instance = new(() => new EventAggregator());

    public static EventAggregator Instance => _instance.Value;

    private readonly Dictionary<Type, List<object>> _subscribers = new();
    private readonly object _lock = new();

    private EventAggregator()
    {
    }

    /// <summary>
    /// 订阅指定类型的消息
    /// </summary>
    public IDisposable Subscribe<T>(Action<T> handler) where T : IEventMessage
    {
        Type messageType = typeof(T);

        lock (_lock)
        {
            if (!_subscribers.ContainsKey(messageType))
            {
                _subscribers[messageType] = new List<object>();
            }

            //WeakReference weakRef = new WeakReference(handler);
            _subscribers[messageType].Add(handler);

            return new Subscription<T>(this, handler);
        }
    }

    /// <summary>
    /// 发布指定类型的消息
    /// </summary>
    public void Publish<T>(T message) where T : IEventMessage
    {
        Type messageType = typeof(T);
        List<Action<T>> validHandlers = new();

        lock (_lock)
        {
            if (!_subscribers.ContainsKey(messageType))
            {
                return;
            }

            List<object> deadReferences = new();

            foreach (object weakRef in _subscribers[messageType])
            {
                if (weakRef is Action<T> handler)
                {
                    validHandlers.Add(handler);
                }
                else
                {
                    deadReferences.Add(weakRef);
                }
            }

            // 清理已失效的引用
            foreach (WeakReference deadRef in deadReferences)
            {
                _subscribers[messageType].Remove(deadRef);
            }
        }

        // 在锁外执行处理器，避免死锁
        foreach (Action<T> handler in validHandlers)
        {
            try
            {
                handler(message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EventAggregator] 处理消息时发生错误: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    private void Unsubscribe<T>(Action<T> handler) where T : IEventMessage
    {
        Type messageType = typeof(T);

        lock (_lock)
        {
            if (!_subscribers.ContainsKey(messageType))
            {
                return;
            }

            object? refToRemove = _subscribers[messageType]
                .FirstOrDefault(wr => ReferenceEquals(wr, handler));

            if (refToRemove != null)
            {
                _subscribers[messageType].Remove(refToRemove);
            }
        }
    }

    /// <summary>
    /// 订阅令牌 - 用于取消订阅
    /// </summary>
    private class Subscription<T> : IDisposable where T : IEventMessage
    {
        private readonly EventAggregator _aggregator;
        private readonly Action<T> _handler;
        private bool _disposed;

        public Subscription(EventAggregator aggregator, Action<T> handler)
        {
            _aggregator = aggregator;
            _handler = handler;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _aggregator.Unsubscribe(_handler);
                _disposed = true;
            }
        }
    }
}

