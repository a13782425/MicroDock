namespace MicroDock.Plugin;

/// <summary>
/// 插件上下文接口，提供日志、图片管理、工具调用等功能
/// </summary>
public interface IPluginContext
{
    #region 日志 API

    /// <summary>
    /// 输出调试日志
    /// </summary>
    /// <param name="message">日志内容</param>
    /// <param name="tag">日志标签（可选，默认使用插件名称）</param>
    void LogDebug(string message, string? tag = null);

    /// <summary>
    /// 输出信息日志
    /// </summary>
    /// <param name="message">日志内容</param>
    /// <param name="tag">日志标签（可选，默认使用插件名称）</param>
    void LogInfo(string message, string? tag = null);

    /// <summary>
    /// 输出警告日志
    /// </summary>
    /// <param name="message">日志内容</param>
    /// <param name="tag">日志标签（可选，默认使用插件名称）</param>
    void LogWarning(string message, string? tag = null);
    /// <summary>
    /// 输出错误日志
    /// </summary>
    /// <param name="message">日志内容</param>
    void LogError(string message);
    /// <summary>
    /// 输出错误日志
    /// </summary>
    /// <param name="message">日志内容</param>
    /// <param name="exception">异常信息（可选）</param>
    void LogError(string message, Exception? exception = null);
    /// <summary>
    /// 输出错误日志
    /// </summary>
    /// <param name="message">日志内容</param>
    /// <param name="tag">日志标签（可选，默认使用插件名称）</param>
    /// <param name="exception">异常信息（可选）</param>
    void LogError(string message, string? tag = null, Exception? exception = null);

    #endregion

    #region 图片管理 API

    /// <summary>
    /// 保存图片
    /// </summary>
    void SaveImage(string key, byte[] imageData);

    /// <summary>
    /// 加载图片
    /// </summary>
    byte[]? LoadImage(string key);

    /// <summary>
    /// 删除图片
    /// </summary>
    void DeleteImage(string key);

    #endregion

    #region 路径 API

    /// <summary>
    /// 资源路径
    /// </summary>
    string AssetsPath { get; }

    /// <summary>
    /// 获取插件配置目录（跟随插件更新而覆盖）
    /// </summary>
    string ConfigPath { get; }

    /// <summary>
    /// 获取插件数据目录（会进行备份，主要存储玩家数据）
    /// </summary>
    string DataPath { get; }
    /// <summary>
    /// 获取插件临时数据目录（会被删除）
    /// </summary>
    string TempDataPath { get; }

    /// <summary>
    /// 依赖程序集路径
    /// </summary>
    string DllPath { get; }

    #endregion

    #region 插件查询 API

    /// <summary>
    /// 判断指定名称的插件是否已加载
    /// </summary>
    /// <param name="pluginName">插件名称</param>
    /// <returns>如果插件已加载则返回 true，否则返回 false</returns>
    bool IsPluginLoaded(string pluginName);

    /// <summary>
    /// 判断指定的多个插件是否全部已加载
    /// </summary>
    /// <param name="pluginNames">插件名称列表</param>
    /// <returns>如果所有插件都已加载则返回 true，否则返回 false</returns>
    bool IsAllPluginsLoaded(params string[] pluginNames);

    /// <summary>
    /// 判断指定的多个插件是否有任意一个已加载
    /// </summary>
    /// <param name="pluginNames">插件名称列表</param>
    /// <returns>如果任意一个插件已加载则返回 true，否则返回 false</returns>
    bool IsAnyPluginLoaded(params string[] pluginNames);


    /// <summary>
    /// 获取所有已加载的插件名称列表(含自身)
    /// </summary>
    /// <returns>已加载插件的名称列表</returns>
    List<string> GetLoadedPluginNames();

    #endregion

    #region 工具调用 API

    /// <summary>
    /// 调用工具（异步）
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <param name="parameters">参数字典（键为参数名，值为参数值）</param>
    /// <param name="pluginName">可选的插件名称，如果指定则只调用该插件的工具，否则调用全局第一个匹配的工具</param>
    /// <returns>工具执行结果（JSON 字符串）</returns>
    Task<string> CallToolAsync(
        string toolName,
        Dictionary<string, string> parameters,
        string? pluginName = null);

    /// <summary>
    /// 获取所有可用工具（含自身的）
    /// </summary>
    List<string> GetAvailableTools();

    /// <summary>
    /// 获取指定插件的工具列表
    /// </summary>
    List<string> GetPluginTools(string pluginName);

    /// <summary>
    /// 判断指定名称的工具是否存在
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <param name="pluginName">可选的插件名称，如果指定则只在该插件中查找</param>
    /// <returns>如果工具存在则返回 true，否则返回 false</returns>
    bool IsToolAvailable(string toolName, string? pluginName = null);
    /// <summary>
    /// 判断多个工具是否全部存在
    /// </summary>
    /// <param name="toolNames">工具名称列表</param>
    /// <returns>如果所有工具都存在则返回 true，否则返回 false</returns>
    bool IsAllToolsAvailable(params string[] toolNames);
    /// <summary>
    /// 判断多个工具是否有任意一个存在
    /// </summary>
    /// <param name="toolNames">工具名称列表</param>
    /// <returns>如果任意一个工具存在则返回 true，否则返回 false</returns>
    bool IsAnyToolAvailable(params string[] toolNames);

    #endregion

    #region 托盘 API

    /// <summary>
    /// 添加托盘菜单项
    /// </summary>
    /// <param name="id">唯一标识符（建议使用插件名前缀）</param>
    /// <param name="text">显示文本</param>
    /// <param name="onClick">点击事件处理</param>
    void AddTrayMenuItem(string id, string text, System.Action onClick);

    /// <summary>
    /// 移除托盘菜单项
    /// </summary>
    /// <param name="id">唯一标识符</param>
    void RemoveTrayMenuItem(string id);

    /// <summary>
    /// 添加托盘菜单分隔符
    /// </summary>
    /// <param name="id">唯一标识符</param>
    void AddTrayMenuSeparator(string id);

    #endregion

    #region 通知 API

    /// <summary>
    /// 显示应用内通知（窗口内Toast通知）
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    /// <param name="type">通知类型</param>
    void ShowInAppNotification(string title, string message, NotificationType type = NotificationType.Information);

    /// <summary>
    /// 显示系统托盘通知
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知内容</param>
    /// <param name="buttons">按钮字典（键为按钮文本，值为按钮ID）</param>
    void ShowSystemNotification(string title, string message, Dictionary<string, string>? buttons = null);

    #endregion

    #region Loading API

    /// <summary>
    /// 显示全屏Loading
    /// </summary>
    /// <param name="message">加载消息（可选）</param>
    void ShowLoading(string? message = null);

    /// <summary>
    /// 隐藏全屏Loading
    /// </summary>
    void HideLoading();

    #endregion
}