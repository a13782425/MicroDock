using System;
using System.Collections.Generic;
using MicroDock.Services;

namespace MicroDock.Infrastructure;

/// <summary>
/// 静态服务定位器 - 用于全局服务访问和管理
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();
    private static readonly object _lock = new();
    
    /// <summary>
    /// 初始化所有应用级服务（在 App 启动时调用一次）
    /// </summary>
    public static void InitializeServices()
    {
        // 1. 注册不依赖窗口的服务
        Register(new AutoStartupService());
        Register(new TrayService());
        
        // 2. 注册需要窗口但延迟初始化的服务
        Register(new AutoHideService());
        Register(new TopMostService());
        
        // 3. 注册原本使用 Instance 单例的服务
        // 注意：IconService 是静态类，不需要注册
        // 注意：LogService 在 Program.InitializeLogger() 中已提前注册
        Register(new PluginLoader());
        Register(new ToolRegistry());
    }
    
    /// <summary>
    /// 注册服务
    /// </summary>
    public static void Register<T>(T service) where T : class
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));
        
        lock (_lock)
        {
            _services[typeof(T)] = service;
        }
    }
    
    /// <summary>
    /// 获取服务（可能为空）
    /// </summary>
    public static T? GetService<T>() where T : class
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
    /// 获取服务（不可为空，如果未注册则抛出异常）
    /// </summary>
    public static T Get<T>() where T : class
    {
        return GetService<T>() ?? throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");
    }
    
    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    public static bool IsRegistered<T>() where T : class
    {
        lock (_lock)
        {
            return _services.ContainsKey(typeof(T));
        }
    }
    
    /// <summary>
    /// 清空所有服务（应用退出时调用）
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _services.Clear();
        }
    }
}
