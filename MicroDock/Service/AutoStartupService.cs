using System;
using System.Reflection;
using MicroDock.Service.Platform;
using Serilog;

namespace MicroDock.Service;

/// <summary>
/// 开机自启动服务
/// 使用平台抽象层，支持跨平台
/// </summary>
public class AutoStartupService : IWindowService
{
    private const string APP_NAME = "MicroDock";
    private readonly IPlatformStartupService? _platformService;
    
    public AutoStartupService()
    {
        _platformService = PlatformServiceFactory.CreateStartupService();
    }
    
    /// <summary>
    /// 启用开机自启动
    /// </summary>
    public void Enable()
    {
        if (_platformService == null || !_platformService.IsSupported)
        {
            Log.Warning("当前平台不支持开机自启动功能");
            return;
        }

        try
        {
            string exePath = GetExecutablePath();
            _platformService.Enable(APP_NAME, exePath);
            Log.Information("开机自启动已启用: {ExecutablePath}", exePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启用开机自启动失败");
        }
    }

    /// <summary>
    /// 禁用开机自启动
    /// </summary>
    public void Disable()
    {
        if (_platformService == null || !_platformService.IsSupported)
        {
            Log.Warning("当前平台不支持开机自启动功能");
            return;
        }

        try
        {
            _platformService.Disable(APP_NAME);
            Log.Information("开机自启动已禁用");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "禁用开机自启动失败");
        }
    }

    /// <summary>
    /// 获取服务是否已启用
    /// </summary>
    public bool IsEnabled
    {
        get
        {
            if (_platformService == null || !_platformService.IsSupported)
            {
                return false;
            }

            try
            {
                return _platformService.IsEnabled(APP_NAME);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "检查开机自启动状态失败");
                return false;
            }
        }
    }

    /// <summary>
    /// 获取可执行文件路径
    /// </summary>
    private string GetExecutablePath()
    {
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
        {
            return processPath;
        }
        
        // 备用方案
        return Assembly.GetEntryAssembly()?.Location ?? string.Empty;
    }
}

