using MicroDock.Service.Platform;
using MicroDock.Service.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MicroDock.Service;

/// <summary>
/// 静态服务定位器 - 用于全局服务访问和管理
/// </summary>
public static class ServiceLocator
{
    /// <summary>
    /// 一对一
    /// </summary>
    private static readonly HashSet<IMicroService> _services = new();
    private static readonly object _lock = new();

    /// <summary>
    /// 多对一
    /// </summary>
    private static readonly Dictionary<Type, object> _servicesMappingDict = new();

    /// <summary>
    /// 初始化所有应用级服务（在 App 启动时调用一次）
    /// </summary>
    public static void InitializeServices()
    {
        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetCustomAttribute<AutoRegisterAttribute>() != null
                && typeof(IMicroService).IsAssignableFrom(t)  // 方向修正
                && t.IsClass
                && !t.IsAbstract)
            .OrderBy(t => t.GetCustomAttribute<AutoRegisterAttribute>()!.Priority);
        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type) as IMicroService;
            if (instance != null)
                Register(instance);
        }

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
            _servicesMappingDict[typeof(T)] = service;
        }
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="service"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void Register(IMicroService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));
        lock (_lock)
        {
            if (_services.Contains(service))
                return;

            var concreteType = service.GetType();

            // 1. 注册具体类型
            _services.Add(service);
            _servicesMappingDict[concreteType] = service;
            // 2. 注册所有实现的接口（继承自 IMicroService 的）
            foreach (var interfaceType in concreteType.GetInterfaces())
            {
                if (interfaceType != typeof(IMicroService)
                    && interfaceType != typeof(IDisposable))
                {
                    _servicesMappingDict[interfaceType] = service;
                }
            }

            // 3. 注册所有父类（继承自 IMicroService 的）
            var baseType = concreteType.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                _servicesMappingDict[baseType] = service;
                baseType = baseType.BaseType;
            }

        }
    }

    /// <summary>
    /// 获取服务（不可为空，如果未注册则抛出异常）
    /// </summary>
    public static T? Get<T>() where T : class
    {
        lock (_lock)
        {
            if (_servicesMappingDict.TryGetValue(typeof(T), out object? service))
            {
                return service as T;
            }
        }
        throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");
    }

    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    public static bool IsRegistered<T>() where T : class
    {
        lock (_lock)
        {
            return _servicesMappingDict.ContainsKey(typeof(T));
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
            _servicesMappingDict.Clear();
        }
    }

    /// <summary>
    /// 服务注册完成后的回调（在所有服务注册后调用一次）
    /// </summary>
    /// <returns></returns>
    internal static async Task OnRegistered()
    {
        foreach (var item in _services)
        {
            try
            {
                await item.OnRegistered();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"服务OnRegistered失败,服务: {item.GetType().FullName}, ex: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 应用程序构建器完成后的回调（在应用构建器配置完成后调用一次）
    /// </summary>
    /// <returns></returns>
    internal static async Task OnAfterAppBuilder()
    {
        foreach (var item in _services)
        {
            try
            {
                await item.OnAfterAppBuilder();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"服务OnBeforeSplashScreen失败,服务: {item.GetType().FullName}, ex: {ex.Message}");
            }
        }
    }


    /// <summary>
    /// 应用程序显示启动画面之前的回调（在显示启动画面之前调用一次）
    /// </summary>
    /// <returns></returns>
    internal static async Task OnBeforeSplashScreen()
    {
        foreach (var item in _services)
        {
            try
            {
                await item.OnBeforeSplashScreen();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"服务OnBeforeSplashScreen失败,服务: {item.GetType().FullName}, ex: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 应用程序显示启动画面后的回调（在显示启动画面后调用一次）
    /// </summary>
    /// <returns></returns>
    internal static async Task OnAfterSplashScreen()
    {
        foreach (var item in _services)
        {
            try
            {
                await item.OnAfterSplashScreen();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"服务OnAfterSplashScreen失败,服务: {item.GetType().FullName}, ex: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 应用程序显示后的回调（在应用显示后调用一次）
    /// </summary>
    /// <returns></returns>
    internal static async Task OnApplicationStarted()
    {
        foreach (var item in _services)
        {
            try
            {
                await item.OnApplicationStarted();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"服务OnApplicationStarted失败,服务: {item.GetType().FullName}, ex: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 应用程序停止时的回调（在应用退出前调用一次）
    /// </summary>
    /// <returns></returns>
    internal static async Task OnApplicationStopping()
    {
        foreach (var item in _services)
        {
            try
            {
                await item.OnApplicationStopping();
            }
            catch (Exception)
            {
            }
        }
    }
}
