using Avalonia.WebView.Desktop;
using AvaloniaWebView.Shared;
using DryIoc.Shared.Extensions;
using MicroDock.Plugin;
using MicroNotePlugin.Core.Interfaces;
using MicroNotePlugin.Infrastructure.Database;
using MicroNotePlugin.Infrastructure.Repositories;
using MicroNotePlugin.Infrastructure.Services;
using MicroNotePlugin.Services;
using MicroNotePlugin.Views;
using Microsoft.Extensions.DependencyInjection;
using WebViewCore;
using WebViewCore.Ioc;

namespace MicroNotePlugin;

/// <summary>
/// 随手记插件 - 提供 Markdown 笔记管理功能
/// </summary>
public class MicroNotePlugin : BaseMicroDockPlugin
{
    private MicroNoteTab? _tab;
    private ServiceProvider? _serviceProvider;
    private NoteDbContext? _dbContext;

    public static MicroNotePlugin? Instance { get; private set; }

    /// <summary>
    /// 插件标签页
    /// </summary>
    public override IMicroTab[] Tabs => _tab != null
        ? new IMicroTab[] { _tab }
        : Array.Empty<IMicroTab>();

    /// <summary>
    /// 服务提供者
    /// </summary>
    public IServiceProvider? Services => _serviceProvider;

    /// <summary>
    /// 插件初始化
    /// </summary>
    public override void OnInit()
    {
        Instance = this;
        base.OnInit();
    }

    public override async Task OnInitAsync()
    {
        await base.OnInitAsync();
        if (Context == null)
        {
            LogError("插件上下文未初始化");
            return;
        }
        // 初始化服务容器
        await InitializeServices(Context.DataPath);

        // 创建主页面
        _tab = new MicroNoteTab(this);

        LogInfo("随手记插件已初始化");
    }

    /// <summary>
    /// 初始化依赖注入服务
    /// </summary>
    private async Task InitializeServices(string dataPath)
    {
        // 创建数据库上下文
        _dbContext = new NoteDbContext(dataPath);

        // 初始化数据库
        await _dbContext.InitializeAsync();

        // 配置服务
        var services = new ServiceCollection();

        // 注册数据库上下文（单例）
        services.AddSingleton(_dbContext);

        // 注册仓储（单例，因为它们持有数据库上下文）
        services.AddSingleton<INoteRepository, SqliteNoteRepository>();
        services.AddSingleton<IFolderRepository, SqliteFolderRepository>();
        services.AddSingleton<ITagRepository, SqliteTagRepository>();
        services.AddSingleton<IVersionService, SqliteVersionRepository>();
        services.AddSingleton<IImageService>(sp =>
            new SqliteImageRepository(sp.GetRequiredService<NoteDbContext>(), dataPath));

        // 注册服务
        services.AddSingleton<ISearchService, FullTextSearchService>();
        services.AddSingleton<MarkdownService>();

        // 构建服务提供者
        _serviceProvider = services.BuildServiceProvider();

        LogInfo("服务容器已初始化");
    }

    /// <summary>
    /// 插件启用
    /// </summary>
    public override void OnEnable()
    {
        base.OnEnable();
        LogInfo("随手记插件已启用");
    }

    /// <summary>
    /// 插件禁用
    /// </summary>
    public override void OnDisable()
    {
        base.OnDisable();
        LogInfo("随手记插件已禁用");
    }

    /// <summary>
    /// 插件销毁
    /// </summary>
    public override void OnDestroy()
    {
        base.OnDestroy();

        // 释放服务
        _serviceProvider?.Dispose();
        _dbContext?.Dispose();

        LogInfo("随手记插件已销毁");
    }
}
