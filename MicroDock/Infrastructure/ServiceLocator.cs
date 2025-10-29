using System;
using System.Collections.Generic;
using MicroDock.Services;

namespace MicroDock.Infrastructure;

/// <summary>
/// 简化版服务定位器 - 用于全局服务访问
/// </summary>
public class ServiceLocator
{
    private static readonly Lazy<ServiceLocator> _instance = new(() => new ServiceLocator());
    
    public static ServiceLocator Instance => _instance.Value;
    
    private readonly Dictionary<Type, object> _services = new();
    private readonly object _lock = new();
    
    private ServiceLocator()
    {
    }
    
    /// <summary>
    /// 注册服务
    /// </summary>
    public void Register<T>(T service) where T : class
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));
        
        lock (_lock)
        {
            _services[typeof(T)] = service;
        }
    }
    
    /// <summary>
    /// 获取服务
    /// </summary>
    public T? GetService<T>() where T : class
    {
        lock (_lock)
        {
            if (_services.TryGetValue(typeof(T), out object? service))
            {
                return service as T;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    public bool IsRegistered<T>() where T : class
    {
        lock (_lock)
        {
            return _services.ContainsKey(typeof(T));
        }
    }
    
    /// <summary>
    /// 清空所有服务
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _services.Clear();
        }
    }
}

